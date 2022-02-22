﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Gurobi;

namespace SA_ILP
{


    class RouteStore
    {
        public List<int> Route { get; private set; }
        public double Value { get; private set; }


        public RouteStore(List<int> route, double value)
        {
            this.Route = route;
            Value = value;
        }

        public override bool Equals(object? obj)
        {
            if (typeof(RouteStore) != obj.GetType())
                return false;
            var y = (RouteStore)obj;
            if (this.Route.Count != y.Route.Count)
                return false;
            for (int i = 0; i < Route.Count; i++)
            {
                if (Route[i] != y.Route[i])
                    return false;
            }
            return true;
        }

        public override string ToString()
        {
            return String.Join(' ', Route);
        }

        //Programmed to efficiently support up to 1000 customers. If more customers are in the problem this might integer overflows and more hash matches.
        public override int GetHashCode()
        {
            //unchecked
            //{
            //    int hash = 19;
            //    foreach (var foo in Route)
            //    {
            //        hash = hash * 31 + foo.GetHashCode();
            //    }
            //    return hash;
            //}


            //return Route.Sum();
            int total = 0;
            for (int i = 0; i < Route.Count; i++)
            {
                total += Route[i] * (i + 1001);
            }
            return total;
            //return Route.Sum(x=>);//String.Join(";", Route).GetHashCode();
        }
    }


    internal class Solver
    {

        public static readonly double BaseRemovedCustomerPenalty = 150;
        public static readonly double BaseRemovedCustomerPenaltyPow = 1.5;

        Random random;
        private double CalcTotalDistance(List<Route> routes, List<Customer> removed, double temp)
        {
            return routes.Sum(x => x.CalcObjective()) + Math.Pow(removed.Count, Solver.BaseRemovedCustomerPenaltyPow) * (Solver.BaseRemovedCustomerPenalty / temp);
        }


        //Calculates the Solomon distance matrix
        private double[,,] CalculateDistanceMatrix(List<Customer> customers)
        {
            double[,,] matrix = new double[customers.Count, customers.Count, 1];
            for (int i = 0; i < customers.Count; i++)
                for (int j = 0; j < customers.Count; j++)
                    matrix[i, j, 0] = CalculateDistanceObjective(customers[i], customers[j]);//Math.Sqrt(Math.Pow(customers[i].X - customers[j].X,2) + Math.Pow(customers[i].Y - customers[j].Y,2));
            return matrix;
        }

        private double[,,] CalculateLoadDependentTimeMatrix(List<Customer> customers, double[,] distanceMatrix, double minWeight, double maxWeight, int numLoadLevels, double powerInput)
        {
            double[,,] matrix = new double[customers.Count, customers.Count, numLoadLevels];
            for (int i = 0; i < customers.Count; i++)
                for (int j = 0; j < customers.Count; j++)
                    for (int l = 0; l < numLoadLevels; l++)
                    {
                        double loadLevelWeight = minWeight + ((maxWeight - minWeight) / numLoadLevels) * l + ((maxWeight - minWeight) / numLoadLevels) / 2;

                        double dist;
                        if (i < j)
                            dist = distanceMatrix[i, j];
                        else
                            dist = distanceMatrix[j, i];

                        matrix[i, j, l] = VRPLTT.CalculateTravelTime(customers[i].Elevation - customers[j].Elevation, dist, loadLevelWeight, powerInput);
                    }
            return matrix;
        }

        public static double CalculateDistanceObjective(Customer cust1, Customer cust2)
        {
            return Math.Sqrt(Math.Pow(cust1.X - cust2.X, 2) + Math.Pow(cust1.Y - cust2.Y, 2));
        }

        public Solver()
        {
            random = new Random();
        }

        private void StupidShuffle(List<Customer> customers, Random random, double timesShuffle = 1)
        {
            for (int i = 0; i < timesShuffle * customers.Count; i++)
            {
                int index1 = random.Next(1, customers.Count);
                int index2 = random.Next(1, customers.Count);

                var temp = customers[index1];
                customers[index1] = customers[index2];
                customers[index2] = temp;
            }


        }

        private void CreateSmartInitialSolution(List<Route> routes, List<Customer> customers, Random random,List<Customer> removed)
        {
            //Creating a copy of the array such that customer can be removed
            customers = customers.ConvertAll(x => x);
            Customer depot = customers[0];
            customers.RemoveAt(0);

            //var c = new TWCOmparer();
            //customers.Sort(1, customers.Count - 1, c);
            Customer? seed = customers.MinBy(x => x.TWEnd);//customers[0];


            List<Customer> inserted = new List<Customer>();

            foreach (Route route in routes)
            {
                if (customers.Count == 0)
                    break;
                double arrivalTime = route.CustomerDist(depot, seed, route.max_capacity);

                if (inserted.Contains(seed))
                    Console.WriteLine("Gaat fout buiten while");

                route.InsertCust(seed, route.route.Count - 1);
                inserted.Add(seed);
                customers.Remove(seed);
                while (customers.Count != 0)
                {
                    //arrivalTime += route.CustomerDist(depot, seed, route.max_capacity) + seed.ServiceTime;
                    if (arrivalTime < seed.TWStart)
                        arrivalTime = seed.TWStart;
                    arrivalTime += +seed.ServiceTime;

                    //customers.Sort((x, y) => x.TWEnd.CompareTo(y.TWEnd));

                    //The customers with the lowest remaining time will be used as seed




                    Customer? next = customers.MinBy(x =>
                    {
                        double dist = route.CustomerDist(seed, x, route.max_capacity);

                        if (arrivalTime + dist < x.TWEnd)
                            if (arrivalTime + dist < x.TWStart)
                                return dist + route.CalculateEarlyPenaltyTerm(arrivalTime + dist, x.TWStart);
                            else
                                return dist;
                        else return double.MaxValue;

                    });

                    if (arrivalTime + route.CustomerDist(seed, next, route.max_capacity) > next.TWEnd || route.used_capacity + next.Demand > route.max_capacity)
                    {
                        seed = customers.MinBy(x => x.TWEnd);
                        break;
                    }
                    arrivalTime += route.CustomerDist(seed, next, route.max_capacity);// + next.ServiceTime;

                    if (inserted.Contains(next))
                        Console.WriteLine("Gaat fout binnen while");

                    seed = next;



                    route.InsertCust(seed, route.route.Count - 1);
                    customers.Remove(seed);
                    inserted.Add(seed);
                    if (customers.Count == 0)
                        seed = null;

                }
                //route.InsertCust(next, route.route.Count-1);

            }

            if (customers.Count != 0)
            {
                Console.WriteLine("Bad initial solution");
                customers.ForEach(x => removed.Add(x));

            }


        }

        private (HashSet<RouteStore>, List<Route>, double) LocalSearchInstance(int id, string name, int numVehicles, double vehicleCapacity, List<Customer> customers, double[,,] distanceMatrix, int seed, bool printExtendedInfo = false, int numInterations = 3000000, int timeLimit = 30000, bool checkInitialSolution = true)
        {
            Console.WriteLine("Starting local search");
            //customers.Sort(1, customers.Count - 1, delegate (Customer x, Customer y) { x.TWEnd.CompareTo(y.TWEnd); });
            //var c = new TWCOmparer();
            //customers.Sort(1, customers.Count - 1, c);

            Random localRandom = new Random(seed);

            StupidShuffle(customers, localRandom);

            List<Route> routes = new List<Route>();
            List<Customer> removed = new List<Customer>();
            const double initialTemp = 30.0;

            //Generate routes
            for (int i = 0; i < numVehicles; i++)
                routes.Add(new Route(customers[0], distanceMatrix, vehicleCapacity, seed: localRandom.Next(), initialTemp, initialTemp));

            CreateSmartInitialSolution(routes, customers, localRandom,removed);

            //List<Customer> tryAgain = new List<Customer>();

            ////Create initial solution
            //foreach (Customer cust in customers)
            //{
            //    if (cust.Id == 0)
            //        continue;
            //    double bestIncrease = double.MaxValue;
            //    int bestPos = -1;
            //    Route bestRoute = null;
            //    foreach (Route r in routes)
            //    {
            //        (int pos, double increase) = r.BestPossibleInsert(cust);
            //        //Used to force more diverse initial solution. THIS IS AN TEMPORARY MEASURE, PLEASE FIX
            //        //if (r.route.Count == 2)
            //        //    increase -= 1000000;
            //        if (increase < bestIncrease)
            //        {
            //            bestIncrease = increase;
            //            bestPos = pos;
            //            bestRoute = r;
            //        }
            //    }
            //    if (bestPos != -1)
            //        bestRoute.InsertCust(cust, bestPos);
            //    else
            //        tryAgain.Add(cust);
            //    //throw new Exception("Cust past niet!");
            //}


            if (checkInitialSolution)
                //Validate intial solution
                foreach (Route route in routes)
                    if (route.CheckRouteValidity())
                    {
                        Console.WriteLine("Initiele oplossing niet valid");
                        throw new Exception();
                    }

            Console.WriteLine("Finished making initial solution");

            int amtImp = 0, amtWorse = 0, amtNotDone = 0;
            double temp = initialTemp;
            double alpha = 0.98;
            double totalP = 0;
            double countP = 0;
            Stopwatch timer = new Stopwatch();
            timer.Start();
            Stopwatch printTimer = new Stopwatch();
            printTimer.Start();


            List<int> viableRoutes = Enumerable.Range(0, routes.Count).Where(i => routes[i].route.Count > 2).ToList();

            int lastChangeExceptedOnIt = 0;
            HashSet<RouteStore> Columns = new HashSet<RouteStore>();
            List<Route> BestSolution = routes.ConvertAll(i => i.CreateDeepCopy());
            
            double bestSolValue = CalcTotalDistance(BestSolution, removed, temp);
            double currentValue = bestSolValue;
            int bestImprovedIteration = 0;
            int numRestarts = 0;
            int previousUpdateIteration = 0;
            int iteration = 0;
            for (; iteration < numInterations && timer.ElapsedMilliseconds <= timeLimit; iteration++)
            {
                double p = localRandom.NextDouble();
                double imp = 0;
                Action? act = null;
                if (p <= 0.25)
                    (imp, act) = Operators.SwapRandomCustomers(routes, viableRoutes, localRandom); // OK
                else if (p <= 0.5)
                    (imp, act) = Operators.MoveRandomCustomerToRandomCustomer(routes, viableRoutes, localRandom);//
                else if (p <= 0.75)
                    (imp, act) = Operators.ReverseOperator(routes, viableRoutes, localRandom); //OK
                //else if (p <= 0.79)
                //    (imp, act) = Operators.RemoveRandomCustomer(routes, viableRoutes, localRandom, removed, temp);
                //else if (p <= 0.98)
                //    (imp, act) = Operators.AddRandomRemovedCustomer(routes, viableRoutes, localRandom, removed, temp); //OK
                else if (p <= 1)
                    (imp, act) = Operators.GreedilyMoveRandomCustomer(routes, viableRoutes, localRandom);
                if (act != null)
                {
                    if (currentValue > double.MaxValue - 1000000)
                        Console.WriteLine("OVERFLOW");
                    //Accept all improvements
                    if (imp > 0)
                    {
                        //double expectedVal = CalcTotalDistance(routes, removed, temp) - imp;
                        //var beforeCopy = routes.ConvertAll(i => i.CreateDeepCopy());
                        act();
                        //if (Math.Round(CalcTotalDistance(routes, removed, temp), 6) != Math.Round(expectedVal, 6))
                        //    Console.WriteLine($"dit gaat mis expected {expectedVal} not equal to {CalcTotalDistance(routes, removed, temp)}, P: {p}");

                        viableRoutes = Enumerable.Range(0, routes.Count).Where(i => routes[i].route.Count > 2).ToList();
                        //Console.WriteLine(p);
                        //routes.ForEach(route => route.CheckRouteValidity());
                        currentValue -= imp;
                        if (currentValue < bestSolValue && removed.Count == 0 && !routes.Exists(x=>x.ViolatesUpperTimeWindow))
                        {
                            //New best solution found
                            bestSolValue = currentValue;
                            bestImprovedIteration = iteration;
                            BestSolution = routes.ConvertAll(i => i.CreateDeepCopy());


                        }

                        //Add columns
                        foreach (Route route in routes)
                        {
                            if(!route.ViolatesUpperTimeWindow)
                                Columns.Add(new RouteStore(route.CreateIdList(),route.Score));

                        }


                        amtImp += 1;
                        lastChangeExceptedOnIt = iteration;

                    }

                    else
                    {
                        double acceptP = Math.Exp(imp / temp);
                        totalP += acceptP;
                        countP += 1;
                        if (random.NextDouble() <= acceptP)
                        {
                            //Worse solution accepted
                            amtWorse += 1;

                            //double expectedVal = CalcTotalDistance(routes,removed,temp) - imp;
                            //var beforeCopy = routes.ConvertAll(i => i.CreateDeepCopy());
                            act();
                            //Console.WriteLine(p);
                            //routes.ForEach(route => route.CheckRouteValidity());
                            //if (Math.Round(CalcTotalDistance(routes,removed,temp), 6) != Math.Round(expectedVal, 6))
                            //    Console.WriteLine($"dit gaat mis expected {expectedVal} not equal to {CalcTotalDistance(routes,removed,temp)}, P: {p}");


                            viableRoutes = Enumerable.Range(0, routes.Count).Where(i => routes[i].route.Count > 2).ToList();
                            currentValue -= imp;

                            lastChangeExceptedOnIt = iteration;
                        }
                    }
                }
                else
                    amtNotDone += 1;
                if (iteration % 10000 == 0 && iteration != 0)
                {
                    temp *= alpha;
                    routes.ForEach(x => x.Temperature = temp);
                    //TOD: Seperate penalty calculation for optimization
                    currentValue = CalcTotalDistance(routes, removed, temp);
                }
                if (iteration - bestImprovedIteration > 1000000 * (Math.Pow(numRestarts * 3 + 1, 2)) && temp < 0.03)
                {
                    numRestarts += 1;
                    //Restart
                    temp = 30;
                    routes.ForEach(x => x.Temperature = temp);
                    routes = BestSolution.ConvertAll(i => i.CreateDeepCopy());
                    viableRoutes = Enumerable.Range(0, routes.Count).Where(i => routes[i].route.Count > 2).ToList();
                    removed = new List<Customer>();
                    currentValue = bestSolValue;
                    Console.WriteLine($"{id}:Best solution changed to long ago. Restarting from best solution with T: {temp}");
                }
                if (printTimer.ElapsedMilliseconds > 10 * 1000)
                {
                    var elapsed = printTimer.ElapsedMilliseconds;
                    printTimer.Restart();
                    double itsPerSecond = (iteration - previousUpdateIteration) / ((double)elapsed / 1000);
                    previousUpdateIteration = iteration;
                    int cnt = routes.Count(x => x.route.Count > 2);
                    Console.WriteLine($"{id}: T: {temp.ToString("0.000")}, S: {CalcTotalDistance(routes, removed, temp).ToString("0.000")}, TS: {currentValue.ToString("0.000")}, N: {cnt}, IT: {iteration}, LA {iteration - lastChangeExceptedOnIt}, B: {bestSolValue.ToString("0.000")}, BI: {bestImprovedIteration}, IT/s: {itsPerSecond.ToString("0.00")}/s");
                }
            }

            Console.WriteLine($"DONE {id}: {name}, Score: {CalcTotalDistance(BestSolution, new List<Customer>(), temp)}, Columns: {Columns.Count}. Completed {iteration} iterations in {Math.Round((double)timer.ElapsedMilliseconds / 1000, 3)}s");
#if DEBUG
            Console.WriteLine(routes.Sum(x => x.numReference));
#endif
            if (printExtendedInfo)
                Console.WriteLine($"  {id}: Total: {amtNotDone + amtImp + amtWorse}, improvements: {amtImp}, worse: {amtWorse}, not done: {amtNotDone}");
            return (Columns, BestSolution, CalcTotalDistance(BestSolution, removed, temp));
        }

        public void SolveSolomonInstance(string fileName, int numIterations = 3000000)
        {
            (string name, int numV, double capV, List<Customer> customers) = SolomonParser.ParseInstance(fileName);
            List<Task<(HashSet<RouteStore>, List<Route>, double)>> tasks = new List<Task<(HashSet<RouteStore>, List<Route>, double)>>();
            var distanceMatrix = CalculateDistanceMatrix(customers);
            (var colums, var sol, var value) = LocalSearchInstance(-1, name, numV, capV, customers.ConvertAll(i => new Customer(i)), distanceMatrix, random.Next(), numInterations: numIterations);
            foreach (Route route in sol)
                route.CheckRouteValidity();


            //double totalWaitingTime = 0;
            //int numViolations = 0;
            //foreach (Route r in sol)
            //{
            //    var load = r.used_capacity;
            //    bool printRoute = false;
            //    for (int i = 0; i < r.route.Count - 1; i++)
            //    {
            //        var dist = r.CustomerDist(r.route[i], r.route[i + 1], load);
            //        load -= r.route[i + 1].Demand;
            //        if (r.arrival_times[i] + dist + r.route[i].ServiceTime < r.route[i + 1].TWStart)
            //        {
            //            totalWaitingTime += r.route[i + 1].TWStart - (r.arrival_times[i] + dist + r.route[i].ServiceTime);
            //            Console.WriteLine(r.route[i + 1].Id);
            //            numViolations += 1;
            //            printRoute = true;
            //        }
            //    }
            //    if (printRoute)
            //        Console.WriteLine(String.Join(',', r.CreateIdList()));
            //}
            //Console.WriteLine($"Total waiting time: {totalWaitingTime} over {numViolations} customers");
        }

        public async Task<double> SolveVRPLTTInstanceAsync(string fileName, int numIterations = 3000000, double bikeMinMass = 150, double bikeMaxMass = 350, int numLoadLevels = 10, double inputPower = 400, int timelimit = 30000, int numThreads = 4)
        {
            (double[,] distances, List<Customer> customers) = VRPLTT.ParseVRPLTTInstance(fileName);
            double[,,] matrix = CalculateLoadDependentTimeMatrix(customers, distances, bikeMinMass, bikeMaxMass, numLoadLevels, inputPower);
            //(var colums, var sol, _, var value) = await LocalSearchInstancAsync("", customers.Count, bikeMaxMass - bikeMinMass, customers, matrix, 1, numIterations, timelimit);//LocalSearchInstance(-1, "", customers.Count, bikeMaxMass-bikeMinMass, customers.ConvertAll(i => new Customer(i)), matrix,random.Next(), numInterations: numIterations,checkInitialSolution:true,timeLimit:timelimit,printExtendedInfo:true);

            (List<RouteStore> ilpSol, double ilpVal, double ilpTime, double lsTime, double lsVal) = await SolveInstanceAsync("", customers.Count, bikeMaxMass - bikeMinMass, customers, matrix, numThreads, numIterations, timeLimit: timelimit);

            double totalWaitingTime = 0;
            int numViolations = 0;
            foreach (RouteStore rs in ilpSol)
            {
                Route r = new Route(customers,rs,customers[0],matrix, bikeMaxMass - bikeMinMass,1,1);
                var load = r.used_capacity;
                for (int i = 0; i < r.route.Count - 1; i++)
                {
                    var dist = r.CustomerDist(r.route[i], r.route[i + 1], load);
                    load -= r.route[i + 1].Demand;
                    if (r.arrival_times[i] + dist + r.route[i].ServiceTime < r.route[i + 1].TWStart)
                    {
                        totalWaitingTime += r.route[i + 1].TWStart - (r.arrival_times[i] + dist + r.route[i].ServiceTime);
                        numViolations += 1;
                    }
                }
            }
            Console.WriteLine($"Total waiting time: {totalWaitingTime} over {numViolations} customers");
            //(List<RouteStore> ilpSol, double ilpVal, double ilpTime, double lsTime, double lsVal) = await SolveInstanceAsync(name, numV, capV, customers, distanceMatrix, numThreads, numIterations);
            return ilpVal;
        }

        public double SolveVRPLTTInstance(string fileName, int numIterations = 3000000, double bikeMinMass = 150, double bikeMaxMass = 350, int numLoadLevels = 10, double inputPower = 400, int timelimit = 30000)
        {
            (double[,] distances, List<Customer> customers) = VRPLTT.ParseVRPLTTInstance(fileName);
            double[,,] matrix = CalculateLoadDependentTimeMatrix(customers, distances, bikeMinMass, bikeMaxMass, numLoadLevels, inputPower);
            (var colums, var sol, var value) = LocalSearchInstance(-1, "", customers.Count, bikeMaxMass - bikeMinMass, customers.ConvertAll(i => new Customer(i)), matrix, random.Next(), numInterations: numIterations, checkInitialSolution: true, timeLimit: timelimit, printExtendedInfo: true);
            double totalWaitingTime = 0;
            int numViolations = 0;
            foreach (Route r in sol)
            {
                var load = r.used_capacity;
                for (int i = 0; i < r.route.Count - 1; i++)
                {
                    var dist = r.CustomerDist(r.route[i], r.route[i + 1], load);
                    load -= r.route[i + 1].Demand;
                    if (r.arrival_times[i] + dist + r.route[i].ServiceTime < r.route[i + 1].TWStart)
                    {
                        totalWaitingTime += r.route[i + 1].TWStart - (r.arrival_times[i] + dist + r.route[i].ServiceTime);
                        numViolations += 1;
                    }
                }
            }
            Console.WriteLine($"Total waiting time: {totalWaitingTime} over {numViolations} customers");
            //(List<RouteStore> ilpSol, double ilpVal, double ilpTime, double lsTime, double lsVal) = await SolveInstanceAsync(name, numV, capV, customers, distanceMatrix, numThreads, numIterations);
            return value;
        }

        public async Task<(bool failed, List<RouteStore> ilpSol, double ilpVal, double ilpTime, double lsTime, double lsVal)> SolveSolomonInstanceAsync(string fileName, int numThreads = 1, int numIterations = 3000000)
        {
            (string name, int numV, double capV, List<Customer> customers) = SolomonParser.ParseInstance(fileName);
            var distanceMatrix = CalculateDistanceMatrix(customers);

            (List<RouteStore> ilpSol, double ilpVal, double ilpTime, double lsTime, double lsVal) = await SolveInstanceAsync(name, numV, capV, customers, distanceMatrix, numThreads, numIterations,timeLimit:100000);
            bool failed = SolomonParser.CheckSolomonSolution(fileName, ilpSol, ilpVal);
            return (failed, ilpSol, ilpVal, ilpTime, lsTime, lsVal);
        }

        public async Task<(HashSet<RouteStore> columns, List<Route> LSSolution, double LSTime, double LSVal)> LocalSearchInstancAsync(string name, int numV, double capV, List<Customer> customers, double[,,] distanceMatrix, int numThreads, int numIterations, int timeLimit)
        {
            List<Task<(HashSet<RouteStore>, List<Route>, double)>> tasks = new List<Task<(HashSet<RouteStore>, List<Route>, double)>>();

            Stopwatch watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < numThreads; i++)
            {
                var id = i;
                tasks.Add(Task.Run(() => { return LocalSearchInstance(id, name, numV, capV, customers.ConvertAll(i => new Customer(i)), distanceMatrix, random.Next(), numInterations: numIterations, timeLimit: timeLimit); }));


            }

            HashSet<RouteStore> allColumns = new HashSet<RouteStore>();
            List<Route> bestSolution = new List<Route>();
            int cnt = 0;
            double bestLSVal = double.MaxValue;
            foreach (var task in tasks)
            {
                (var columns, var solution, var value) = await task;
                if (value < bestLSVal)
                {
                    bestLSVal = value;
                    bestSolution = solution;

                }
                allColumns.UnionWith(columns);
                cnt += columns.Count;
            }
            watch.Stop();
            Console.WriteLine();
            Console.WriteLine($" Sum of unique columns found per start: {cnt}");
            Console.WriteLine($" Total amount of unique column: {allColumns.Count}");

            return (allColumns, bestSolution, (double)watch.ElapsedMilliseconds / 1000, bestLSVal);
        }

        public async Task<(List<RouteStore> ilpSol, double ilpVal, double ilpTime, double lsTime, double lsVal)> SolveInstanceAsync(string name, int numV, double capV, List<Customer> customers, double[,,] distanceMatrix, int numThreads, int numIterations, int timeLimit = 30000)
        {
            (var allColumns, var bestSolution, var LSTime, var LSSCore) = await LocalSearchInstancAsync(name, numV, capV, customers, distanceMatrix, numThreads, numIterations, timeLimit);
            (var ilpSol, double ilpVal, double time) = SolveILP(allColumns, customers, numV, bestSolution);


            return (ilpSol, ilpVal, time, LSTime, LSSCore);

        }

        private (List<RouteStore>, double, double) SolveILP(HashSet<RouteStore> columns, List<Customer> customers, int numVehicles, List<Route> bestSolutionLS)
        {
            var columList = columns.ToArray();
            var bestSolStore = bestSolutionLS.ConvertAll(x => new RouteStore(x.CreateIdList(),x.Score));
            //double[] costs = new double[columList.Length];
            byte[,] custInRoute = new byte[customers.Count, columList.Length];
            for (int i = 0; i < columList.Length; i++)
            {
               // double cost = 0;
                for (int j = 0; j < columList[i].Route.Count - 1; j++)
                {
                    //cost += CalculateDistanceObjective(customers[columList[i].Route[j]], customers[columList[i].Route[j + 1]]);
                    custInRoute[columList[i].Route[j], i] = 1;
                    custInRoute[columList[i].Route[j + 1], i] = 1;
                }
                //costs[i] = cost;
            }

            //Create model
            GRBEnv env = new GRBEnv();
            GRBModel model = new GRBModel(env);

            model.Parameters.TimeLimit = 1800;

            //Create decision variables
            GRBVar[] columnDecisions = new GRBVar[columList.Length];
            //columnDecisions = model.AddVars(columList.Length, GRB.BINARY,);
            for (int i = 0; i < columList.Length; i++)
            {
                columnDecisions[i] = model.AddVar(0, 1, columList[i].Value, GRB.BINARY, $"X{i}");
            }

            foreach (Customer cust in customers)
            {
                if (cust.Id == 0)
                    continue;
                GRBLinExpr custTotal = 0;
                for (int i = 0; i < columList.Length; i++)
                    custTotal.AddTerm(custInRoute[cust.Id, i], columnDecisions[i]);
                model.AddConstr(custTotal == 1, $"Cust{cust.Id}");
            }

            GRBLinExpr numV = 0;
            for (int i = 0; i < columList.Length; i++)
                numV.AddTerm(1, columnDecisions[i]);
            model.AddConstr(numV <= numVehicles, "Bound vehicles");
            model.ModelSense = GRB.MINIMIZE;

            //Add warm start
            for (int i = 0; i < columList.Length; i++)
            {
                if (bestSolStore.Contains(columList[i]))
                {
                    columnDecisions[i].Start = 1;
                }
            }

            model.Optimize();

            List<RouteStore> solution = new List<RouteStore>();
            for (int i = 0; i < columnDecisions.Length; i++)
            {
                if (columnDecisions[i].X == 1)
                    solution.Add(columList[i]);
            }
            return (solution, model.ObjVal, model.Runtime);
        }

    }

    static class SolomonParser
    {
        public static (string, int, double, List<Customer>) ParseInstance(string fileName)
        {
            using (var reader = new StreamReader(fileName))
            {
                var name = reader.ReadLine().Replace("\n", "");

                reader.ReadLine();
                reader.ReadLine();
                reader.ReadLine();

                var reg = new Regex("  +");
                var line = reader.ReadLine().Replace("\n", "");
                var lineSplit = reg.Replace(line, " ").Split(' ');
                int numV = int.Parse(lineSplit[1]);
                int capV = int.Parse(lineSplit[2]);

                reader.ReadLine();
                reader.ReadLine();
                reader.ReadLine();
                reader.ReadLine();
                ;// reader.ReadLine().Replace("\n", "");

                List<Customer> customers = new List<Customer>();
                while ((line = reader.ReadLine()) != null && line != "")
                {
                    line = line.Replace("\n", "");
                    lineSplit = reg.Replace(line, " ").Split(' ');
                    customers.Add(new Customer(int.Parse(lineSplit[1]), int.Parse(lineSplit[2]), int.Parse(lineSplit[3]), int.Parse(lineSplit[4]), int.Parse(lineSplit[5]), int.Parse(lineSplit[6]), int.Parse(lineSplit[7])));
                }
                return (name, numV, capV, customers);
            }
        }

        public static bool CheckSolomonSolution(string instance, List<RouteStore> solution, double value)
        {
            (string name, int numV, double capV, List<Customer> customers) = SolomonParser.ParseInstance(instance);

            double totalDist = 0;
            HashSet<int> CustomersVisited = new HashSet<int>();
            bool failedTotal = false;
            foreach (var route in solution)
            {
                double arrivalTime = 0;
                bool failed = false;
                double usedCapacity = 0;
                for (int i = 0; i < route.Route.Count; i++)
                {
                    var res = CustomersVisited.Add(route.Route[i]);
                    //If the customer is visited twice and it is not the depot. Fail the solution
                    if (route.Route[i] != 0 && !res)
                    {
                        failed = true;
                        Console.WriteLine($"FAIL. Visited customer {route.Route[i]} more than once");
                    }
                    //Check capacity
                    usedCapacity += customers[route.Route[i]].Demand;
                    if (usedCapacity > capV)
                    {
                        failed = true;
                        Console.WriteLine($"FAIL. Exceeded vehicle capacity");
                    }
                    if (arrivalTime > customers[route.Route[i]].TWStart)
                        arrivalTime = customers[route.Route[i]].TWStart;

                    //Update travel distances
                    if (i != route.Route.Count - 1)
                    {
                        double dist = Solver.CalculateDistanceObjective(customers[route.Route[i]], customers[route.Route[i + 1]]);
                        totalDist += dist;
                        arrivalTime += dist + customers[route.Route[i]].ServiceTime;
                    }


                }
                if (!failed)
                    Console.WriteLine("PASSED");
                failedTotal |= failed;
            }

            //Check if all customers were visited
            if (CustomersVisited.Count != customers.Count)
            {
                failedTotal = true;
                Console.WriteLine("Did not visit all customers");
            }
            //Check if the solution values match
            if (Math.Round(totalDist, 6) != Math.Round(value, 6))
            {
                failedTotal = true;
                Console.WriteLine($"Wrong objective value reported. Epected {value.ToString("0.000")} does not match found {totalDist.ToString("0.000")}");
            }
            return failedTotal;

        }

    }

    public class TWCOmparer : IComparer<Customer>
    {
        public int Compare(Customer x, Customer y)
        {
            if (x == null)
            {
                if (y == null)
                {
                    // If x is null and y is null, they're
                    // equal.
                    return 0;
                }
                else
                {
                    // If x is null and y is not null, y
                    // is greater.
                    return -1;
                }
            }
            else
            {
                // If x is not null...
                //
                if (y == null)
                // ...and y is null, x is greater.
                {
                    return 1;
                }
                else
                {

                    return x.TWEnd.CompareTo(y.TWEnd);//x.Length.CompareTo(y.Length);


                }
            }
        }
    }


}
