using System;
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
        public static double CalcTotalDistance(List<Route> routes, List<Customer> removed, double temp)
        {
            //This expclitly does not use the Score property of a route to force recalculation. This way bugs might be caught
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
            Parallel.For(0, customers.Count, i =>
            {
                for (int j = 0; j < customers.Count; j++)
                    for (int l = 0; l < numLoadLevels; l++)
                    {
                        double loadLevelWeight = minWeight + ((maxWeight - minWeight) / numLoadLevels) * l + ((maxWeight - minWeight) / numLoadLevels) / 2;

                        double dist;
                        if (i < j)
                            dist = distanceMatrix[i, j];
                        else
                            dist = distanceMatrix[j, i];

                        matrix[i, j, l] = VRPLTT.CalculateTravelTime(customers[j].Elevation  - customers[i].Elevation, dist, loadLevelWeight, powerInput);
                    }

            }); // (int i = 0; i < customers.Count; i++)

            //for (int i = 0; i < customers.Count; i++)
            //    for (int j = 0; j < customers.Count; j++)
            //        for (int l = 0; l < numLoadLevels; l++)
            //        {
            //            double loadLevelWeight = minWeight + ((maxWeight - minWeight) / numLoadLevels) * l + ((maxWeight - minWeight) / numLoadLevels) / 2;

            //            double dist;
            //            if (i < j)
            //                dist = distanceMatrix[i, j];
            //            else
            //                dist = distanceMatrix[j, i];

            //            matrix[i, j, l] = VRPLTT.CalculateTravelTime(customers[i].Elevation - customers[j].Elevation, dist, loadLevelWeight, powerInput);
            //        }

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





        public void SolveSolomonInstance(string fileName, int numIterations = 3000000,int timeLimit = 30000)
        {
            (string name, int numV, double capV, List<Customer> customers) = SolomonParser.ParseInstance(fileName);
            List<Task<(HashSet<RouteStore>, List<Route>, double)>> tasks = new List<Task<(HashSet<RouteStore>, List<Route>, double)>>();
            var distanceMatrix = CalculateDistanceMatrix(customers);
            var ls = new LocalSearch(LocalSearchConfigs.VRPTW, random.Next());
            (var colums, var sol, var value) = ls.LocalSearchInstance(-1, name, numV, capV, customers.ConvertAll(i => new Customer(i)), distanceMatrix, numInterations: numIterations,timeLimit:timeLimit);
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

        public async Task<double> SolveVRPLTTInstanceAsync(string fileName, int numIterations = 3000000, double bikeMinMass = 140, double bikeMaxMass = 290, int numLoadLevels = 10, double inputPower = 350, int timelimit = 30000, int numThreads = 4, int numStarts=4)
        {
            (double[,] distances, List<Customer> customers) = VRPLTT.ParseVRPLTTInstance(fileName);
            Stopwatch w = new Stopwatch();
            w.Start();
            Console.WriteLine("Calculating travel time matrix");
            double[,,] matrix = CalculateLoadDependentTimeMatrix(customers, distances, bikeMinMass, bikeMaxMass, numLoadLevels, inputPower);
            Console.WriteLine($"Created distance matrix in {((double)w.ElapsedMilliseconds/1000).ToString("0.00")}s");
            //(var colums, var sol, _, var value) = await LocalSearchInstancAsync("", customers.Count, bikeMaxMass - bikeMinMass, customers, matrix, 1, numIterations, timelimit);//LocalSearchInstance(-1, "", customers.Count, bikeMaxMass-bikeMinMass, customers.ConvertAll(i => new Customer(i)), matrix,random.Next(), numInterations: numIterations,checkInitialSolution:true,timeLimit:timelimit,printExtendedInfo:true);

            (List<RouteStore> ilpSol, double ilpVal, double ilpTime, double lsTime, double lsVal) = await SolveInstanceAsync("", customers.Count, bikeMaxMass - bikeMinMass, customers, matrix, numThreads,numStarts, numIterations, LocalSearchConfigs.VRPLTT, timeLimit: timelimit);

            double totalWaitingTime = 0;
            int numViolations = 0;
            double totalStartTime = 0;
            double checkValue = 0;
            double numCustomers = 0;
            double totalRouteLength = 0;
            foreach (RouteStore rs in ilpSol)
            {
                var ls = new LocalSearch(LocalSearchConfigs.VRPLTT, random.Next());
                Route r = new Route(customers, rs, customers[0], matrix, bikeMaxMass - bikeMinMass, ls);
                checkValue += r.CalcObjective();
                totalStartTime += r.arrival_times[0];
                totalRouteLength += r.arrival_times[r.arrival_times.Count - 1] - r.route.Sum(x => x.ServiceTime);
                numCustomers += r.route.Count - 2;
                var load = r.used_capacity;
                Console.WriteLine(r);
                r.CheckRouteValidity();
                for (int i = 0; i < r.route.Count - 1; i++)
                {
                    var dist = r.CustomerDist(r.route[i], r.route[i + 1], load);
                    load -= r.route[i + 1].Demand;
                    if (r.arrival_times[i] + dist + r.route[i].ServiceTime < r.route[i + 1].TWStart)
                    {
                        Console.WriteLine($"Waiting for customer {r.route[i + 1].Id}");
                        totalWaitingTime += r.route[i + 1].TWStart - (r.arrival_times[i] + dist + r.route[i].ServiceTime);
                        numViolations += 1;
                    }
                }
            }
            Console.WriteLine($"TotalRouteLength {totalRouteLength}");
            Console.WriteLine($"Num Customers {numCustomers}");
            Console.WriteLine($"Recalculated score: {checkValue}");
            Console.WriteLine($"Total start time {totalStartTime}");
            Console.WriteLine($"Total waiting time: {totalWaitingTime} over {numViolations} customers");
            //(List<RouteStore> ilpSol, double ilpVal, double ilpTime, double lsTime, double lsVal) = await SolveInstanceAsync(name, numV, capV, customers, distanceMatrix, numThreads, numIterations);
            return ilpVal;
        }

        public double SolveVRPLTTInstance(string fileName, int numIterations = 3000000, double bikeMinMass = 150, double bikeMaxMass = 350, int numLoadLevels = 10, double inputPower = 400, int timelimit = 30000)
        {
            (double[,] distances, List<Customer> customers) = VRPLTT.ParseVRPLTTInstance(fileName);
            double[,,] matrix = CalculateLoadDependentTimeMatrix(customers, distances, bikeMinMass, bikeMaxMass, numLoadLevels, inputPower);
            var ls = new LocalSearch(LocalSearchConfigs.VRPLTT, random.Next());
            (var colums, var sol, var value) = ls.LocalSearchInstance(-1, "", customers.Count, bikeMaxMass - bikeMinMass, customers.ConvertAll(i => new Customer(i)), matrix, numInterations: numIterations, checkInitialSolution: false, timeLimit: timelimit, printExtendedInfo: true);
            foreach (var route in sol)
                Console.WriteLine(route);

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
                        Console.WriteLine($"Waiting for customer {r.route[i + 1].Id}");

                        totalWaitingTime += r.route[i + 1].TWStart - (r.arrival_times[i] + dist + r.route[i].ServiceTime);
                        numViolations += 1;
                    }
                }
            }
            Console.WriteLine($"Total waiting time: {totalWaitingTime} over {numViolations} customers");
            //(List<RouteStore> ilpSol, double ilpVal, double ilpTime, double lsTime, double lsVal) = await SolveInstanceAsync(name, numV, capV, customers, distanceMatrix, numThreads, numIterations);
            return value;
        }

        public static void ErrorPrint(string toPrint)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(toPrint);
            Console.ForegroundColor = ConsoleColor.White;
        }

        public async Task DoTest(string fileName, int numThreads = 1, int numIterations = 3000000, int timeLimit = 100000)
        {
            (string name, int numV, double capV, List<Customer> customers) = SolomonParser.ParseInstance(fileName);
            var distanceMatrix = CalculateDistanceMatrix(customers);
            await LocalSearchInstancAsync(name, numV, capV, customers, distanceMatrix, 4,4, numIterations, timeLimit,LocalSearchConfigs.VRPTW);
        }

        public async Task<(bool failed, List<RouteStore> ilpSol, double ilpVal, double ilpTime, double lsTime, double lsVal)> SolveSolomonInstanceAsync(string fileName, int numThreads = 1, int numStarts=4, int numIterations = 3000000, int timeLimit = 100000)
        {
            (string name, int numV, double capV, List<Customer> customers) = SolomonParser.ParseInstance(fileName);
            var distanceMatrix = CalculateDistanceMatrix(customers);

            (List<RouteStore> ilpSol, double ilpVal, double ilpTime, double lsTime, double lsVal) = await SolveInstanceAsync(name, numV, capV, customers, distanceMatrix, numThreads,numStarts, numIterations, LocalSearchConfigs.VRPTW, timeLimit: timeLimit);
            bool failed = SolomonParser.CheckSolomonSolution(fileName, ilpSol, ilpVal);
            ilpSol.ForEach(x=>Console.WriteLine(x));    
            return (failed, ilpSol, ilpVal, ilpTime, lsTime, lsVal);
        }

        public async Task<(HashSet<RouteStore> columns, List<Route> LSSolution, double LSTime, double LSVal)> LocalSearchInstancAsync(string name, int numV, double capV, List<Customer> customers, double[,,] distanceMatrix, int numThreads,int numStarts, int numIterations, int timeLimit, LocalSearchConfiguration config)
        {
            List<Task<(HashSet<RouteStore>, List<Route>, double)>> tasks = new List<Task<(HashSet<RouteStore>, List<Route>, double)>>();

            Stopwatch watch = new Stopwatch();
            watch.Start();

            HashSet<RouteStore> allColumns = new HashSet<RouteStore>();
            List<Route> bestSolution = new List<Route>();
            int cnt = 0;
            double bestLSVal = double.MaxValue;

            for (int j = 0; j < numStarts; j += numThreads)
            {
                for (int i = 0; i < numThreads && i + j < numStarts; i++)
                {
                    var id = i;
                    var ls = new LocalSearch(config, random.Next());
                    tasks.Add(Task.Run(() => { return ls.LocalSearchInstance(id, name, numV, capV, customers.ConvertAll(i => new Customer(i)), distanceMatrix.Clone() as double[,,], numInterations: numIterations, timeLimit: timeLimit, printExtendedInfo: false); }));
                    

                }
                
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
            }



            watch.Stop();
            Console.WriteLine();
            Console.WriteLine($" Sum of unique columns found per start: {cnt}");
            Console.WriteLine($" Total amount of unique column: {allColumns.Count}");

            return (allColumns, bestSolution, (double)watch.ElapsedMilliseconds / 1000, bestLSVal);
        }

        public async Task<(List<RouteStore> ilpSol, double ilpVal, double ilpTime, double lsTime, double lsVal)> SolveInstanceAsync(string name, int numV, double capV, List<Customer> customers, double[,,] distanceMatrix, int numThreads,int numStarts, int numIterations, LocalSearchConfiguration configuration, int timeLimit = 30000)
        {
            (var allColumns, var bestSolution, var LSTime, var LSSCore) = await LocalSearchInstancAsync(name, numV, capV, customers, distanceMatrix, numThreads, numStarts, numIterations, timeLimit, configuration);
            (var ilpSol, double ilpVal, double time) = SolveILP(allColumns, customers, numV, bestSolution);


            return (ilpSol, ilpVal, time, LSTime, LSSCore);

        }

        private (List<RouteStore>, double, double) SolveILP(HashSet<RouteStore> columns, List<Customer> customers, int numVehicles, List<Route> bestSolutionLS)
        {
            var columList = columns.ToArray();
            var bestSolStore = bestSolutionLS.ConvertAll(x => new RouteStore(x.CreateIdList(), x.Score));
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

            model.Parameters.TimeLimit = 3600;

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
