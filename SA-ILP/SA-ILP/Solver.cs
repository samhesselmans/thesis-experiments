using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Gurobi;
using MathNet.Numerics.Distributions;

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
        public static double CalcTotalDistance(List<Route> routes, List<Customer> removed, LocalSearch ls)
        {
            //This expclitly does not use the Score property of a route to force recalculation. This way bugs might be caught
            return routes.Sum(x => x.CalcObjective()) + CalcRemovedPenalty(removed.Count,ls);
        }

        public static double CalcRemovedPenalty(int removedCount,LocalSearch ls)
        {
            return Math.Pow(removedCount, ls.Config.BaseRemovedCustomerPenaltyPow) * (ls.Config.BaseRemovedCustomerPenalty / Math.Pow(ls.Temperature,2));
        }


        //Calculates the Solomon distance matrix
        private double[,,] CalculateDistanceMatrix(List<Customer> customers)
        {
            int size = customers.Max(x => x.Id) + 1;
            double[,,] matrix = new double[size , size, 1];
            for (int i = 0; i < customers.Count; i++)
                for (int j = 0; j < customers.Count; j++)
                    matrix[i, j, 0] = CalculateDistanceObjective(customers[i], customers[j]);//Math.Sqrt(Math.Pow(customers[i].X - customers[j].X,2) + Math.Pow(customers[i].Y - customers[j].Y,2));
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
            (var colums, var sol, var value) = ls.LocalSearchInstance(0, name, numV, capV, customers.ConvertAll(i => new Customer(i)), distanceMatrix,new Gamma[distanceMatrix.GetLength(0), distanceMatrix.GetLength(1), distanceMatrix.GetLength(2)], new Gamma[distanceMatrix.GetLength(0), distanceMatrix.GetLength(1), distanceMatrix.GetLength(2)], numInterations: numIterations,timeLimit:timeLimit);
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

        public async Task<(bool failed, List<Route> ilpSol, double ilpVal, double ilpTime, double lsTime, double lsVal)> SolveVRPLTTInstanceAsync(string fileName, int numIterations = 3000000, double bikeMinMass = 140, double bikeMaxMass = 290, int numLoadLevels = 10, double inputPower = 350, int timelimit = 30000, int numThreads = 4, int numStarts=4,LocalSearchConfiguration? config = null)
        {
            if (config == null)
                config = LocalSearchConfigs.VRPLTT;

            (double[,] distances, List<Customer> customers) = VRPLTT.ParseVRPLTTInstance(fileName);
            Stopwatch w = new Stopwatch();
            w.Start();
            Console.WriteLine("Calculating travel time matrix");
            (double[,,] matrix,Gamma[,,] distributionMatrix,IContinuousDistribution[,,] approximationMatrix,_) = VRPLTT.CalculateLoadDependentTimeMatrix(customers, distances, bikeMinMass, bikeMaxMass, numLoadLevels, inputPower,((LocalSearchConfiguration)config).WindSpeed, ((LocalSearchConfiguration)config).WindDirection);
            Console.WriteLine($"Created distance matrix in {((double)w.ElapsedMilliseconds/1000).ToString("0.00")}s");
            //(var colums, var sol, _, var value) = await LocalSearchInstancAsync("", customers.Count, bikeMaxMass - bikeMinMass, customers, matrix, 1, numIterations, timelimit);//LocalSearchInstance(-1, "", customers.Count, bikeMaxMass-bikeMinMass, customers.ConvertAll(i => new Customer(i)), matrix,random.Next(), numInterations: numIterations,checkInitialSolution:true,timeLimit:timelimit,printExtendedInfo:true);
            //Route.objective_matrix = matrix;



            (List<RouteStore> ilpSol, double ilpVal, double ilpTime, double lsTime, double lsVal) = await SolveInstanceAsync("", customers.Count, bikeMaxMass - bikeMinMass, customers, matrix,distributionMatrix,approximationMatrix, numThreads,numStarts, numIterations, (LocalSearchConfiguration)config, timeLimit: timelimit);

            double totalWaitingTime = 0;
            int numViolations = 0;
            double totalStartTime = 0;
            double checkValue = 0;
            double numCustomers = 0;
            double totalRouteLength = 0;
            using(var sw =  new StreamWriter("out.txt"))
            foreach (RouteStore rs in ilpSol)
            {
                var ls = new LocalSearch((LocalSearchConfiguration)config, random.Next());
                Route r = new Route(customers, rs, customers[0], matrix,distributionMatrix, approximationMatrix, bikeMaxMass - bikeMinMass, ls);
                sw.WriteLine($"{r},");
                checkValue += r.CalcObjective();
                totalStartTime += r.arrival_times[0];
                totalRouteLength += r.arrival_times[r.arrival_times.Count - 1] - r.route.Sum(x => x.ServiceTime);
                numCustomers += r.route.Count - 2;
                var load = r.used_capacity;
                if(r.route.Count != 2)
                    Console.WriteLine(r);
                r.CheckRouteValidity();
                for (int i = 0; i < r.route.Count - 1; i++)
                {
                    (var dist, IContinuousDistribution distribution) = r.CustomerDist(r.route[i], r.route[i + 1], load, false);
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
            var parent = new LocalSearch((LocalSearchConfiguration)config, random.Next());
            return (false,ilpSol.ConvertAll(x=>new Route(customers,x,customers[0],matrix,distributionMatrix, approximationMatrix, bikeMaxMass-bikeMinMass, parent)),ilpVal,ilpTime,lsTime,lsVal);
        }

        public double SolveVRPLTTInstance(string fileName, int numIterations = 3000000, double bikeMinMass = 150, double bikeMaxMass = 350, int numLoadLevels = 10, double inputPower = 400, int timelimit = 30000,LocalSearchConfiguration? config = null)
        {

            if (config == null)
                config = LocalSearchConfigs.VRPLTTDebug;

            (double[,] distances, List<Customer> customers) = VRPLTT.ParseVRPLTTInstance(fileName);



            (double[,,] matrix,Gamma[,,] distributionMatrix,IContinuousDistribution[,,] approximationMatrix,_) = VRPLTT.CalculateLoadDependentTimeMatrix(customers, distances, bikeMinMass, bikeMaxMass, numLoadLevels, inputPower, ((LocalSearchConfiguration)config).WindSpeed, ((LocalSearchConfiguration)config).WindDirection);
            var ls = new LocalSearch((LocalSearchConfiguration)config, random.Next());
            //ls.config.ScaleLatenessPenaltyWithTemperature = true;
            (var colums, var sol, var value) = ls.LocalSearchInstance(0, "", customers.Count, bikeMaxMass - bikeMinMass, customers.ConvertAll(i => new Customer(i)), matrix,distributionMatrix,approximationMatrix, numInterations: numIterations, checkInitialSolution: false, timeLimit: timelimit);

            double totalDist = 0;
            double totalWait = 0;
            double totalOntimePercentage = 0;
            int numRoutes = 0;
            using(var sw = new StreamWriter("out.txt"))
            foreach (var route in sol)
            {
                if (route.route.Count != 2)
                {
                        sw.WriteLine($"{route},");
                    Console.WriteLine(route);
                    //Console.WriteLine($"{route}; ST {route.startTime} ; SST {route.route[1].TWStart - route.CustomerDist(route.route[0], route.route[1], route.used_capacity).Item1}");
                    numRoutes++;
                    double avg = 0;
                    int total = 0;
                    double worst = double.MaxValue;
                    Customer? worstCust = null;
                    int worstIndex = -1;

                    for (int i = 0; i < route.route.Count; i++)
                    {

                        if (route.customerDistributions[i] != null)
                        {
                            total += 1;

                            double difUp = route.route[i].TWEnd - route.arrival_times[i];
                            double difDown = route.route[i].TWStart - route.arrival_times[i];

                            if (difUp < 0)
                                difUp = 0;
                            if (difDown < 0)
                                difDown = 0;

                            double pEarly = route.customerDistributions[i].CumulativeDistribution(difDown);

                            double pOnTime = (route.customerDistributions[i].CumulativeDistribution(difUp));

                            if (Double.IsNaN(pOnTime))
                                pOnTime = 1;

                            if (((LocalSearchConfiguration)config).AllowEarlyArrival)
                                pEarly = 0;

                            double p = pOnTime - pEarly;

                            avg += p;
                            if (p < worst)
                            {
                                worstCust = route.route[i];
                                worst = p;
                                worstIndex = i;
                            }
                        }
                    }
                    //Console.WriteLine($"{route}: On time performance: {avg / total} worst: {worst} at {worstCust} at {worstIndex}");

                    //var res = route.Simulate(1000000);
                    //totalDist += res.AverageTravelTime;
                    //totalWait += res.AverageWaitingTime;
                    //totalOntimePercentage += res.OnTimePercentage;

                    //int minIndex = -1;
                    //double min = double.MaxValue;
                    //for(int j =0;j<res.CustomerOnTimePercentage.Length;j++)
                    //{
                    //    if (res.CustomerOnTimePercentage[j] < min)
                    //    {
                    //        min = res.CustomerOnTimePercentage[j];
                    //        minIndex = j;
                    //    }

                    //}

                    //Console.WriteLine($"Simmulated on time perfomance: {res.OnTimePercentage} worst: {min} at {minIndex}\n");
                }

            }

            //Console.WriteLine($"Average solution travel time: {totalDist} with OTP: {totalOntimePercentage/numRoutes}");
            //Console.WriteLine($"Average solution waiting time: {totalWait}");

            //CheckRouteQualityVRPLTT(sol, matrix, bikeMaxMass - bikeMinMass);

            double totalWaitingTime = 0;
            int numViolations = 0;
            foreach (Route r in sol)
            {
                var load = r.used_capacity;
                for (int i = 0; i < r.route.Count - 1; i++)
                {
                    (var dist, IContinuousDistribution distribution) = r.CustomerDist(r.route[i], r.route[i + 1], load, false);
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

        public static void PrintRoutes(List<Route> routes)
        {
            foreach (Route route in routes)
                if (route.route.Count != 2)
                    Console.WriteLine(route);
        }

        public async Task DoTest(string fileName, int numThreads = 1, int numIterations = 3000000, int timeLimit = 100000)
        {
            (string name, int numV, double capV, List<Customer> customers) = SolomonParser.ParseInstance(fileName);
            var distanceMatrix = CalculateDistanceMatrix(customers);
            await LocalSearchInstancAsync(name, numV, capV, customers, distanceMatrix, new Gamma[distanceMatrix.GetLength(0), distanceMatrix.GetLength(1), distanceMatrix.GetLength(2)], new Gamma[distanceMatrix.GetLength(0), distanceMatrix.GetLength(1), distanceMatrix.GetLength(2)], 4,4, numIterations, timeLimit,LocalSearchConfigs.VRPTW);
        }

        public async Task<(bool failed, List<RouteStore> ilpSol, double ilpVal, double ilpTime, double lsTime, double lsVal)> SolveSolomonInstanceAsync(string fileName, int numThreads = 1, int numStarts=4, int numIterations = 3000000, int timeLimit = 100000,LocalSearchConfiguration? config = null)
        {
            (string name, int numV, double capV, List<Customer> customers) = SolomonParser.ParseInstance(fileName);
            var distanceMatrix = CalculateDistanceMatrix(customers);


            if (config == null)
                config = LocalSearchConfigs.VRPTW;

            (List<RouteStore> ilpSol, double ilpVal, double ilpTime, double lsTime, double lsVal) = await SolveInstanceAsync(name, numV, capV, customers, distanceMatrix, new Gamma[distanceMatrix.GetLength(0), distanceMatrix.GetLength(1), distanceMatrix.GetLength(2)], new Gamma[distanceMatrix.GetLength(0), distanceMatrix.GetLength(1), distanceMatrix.GetLength(2)], numThreads,numStarts, numIterations, (LocalSearchConfiguration)config, timeLimit: timeLimit);
            bool failed = SolomonParser.CheckSolomonSolution(fileName, ilpSol, ilpVal);
            ilpSol.ForEach(x=>Console.WriteLine(x));
            return (failed, ilpSol, ilpVal, ilpTime, lsTime, lsVal);
        }

        public async Task<(HashSet<RouteStore> columns, List<Route> LSSolution, double LSTime, double LSVal)> LocalSearchInstancAsync(string name, int numV, double capV, List<Customer> customers, double[,,] distanceMatrix,Gamma[,,] distributionMatrix,IContinuousDistribution[,,] approximationMatrix, int numThreads,int numStarts, int numIterations, int timeLimit, LocalSearchConfiguration config)
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
                    tasks.Add(Task.Run(() => { return ls.LocalSearchInstance(id, name, numV, capV, customers.ConvertAll(i => new Customer(i)), distanceMatrix.Clone() as double[,,], distributionMatrix,approximationMatrix, numInterations: numIterations, timeLimit: timeLimit); }));


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

        public async Task<(List<RouteStore> ilpSol, double ilpVal, double ilpTime, double lsTime, double lsVal)> SolveInstanceAsync(string name, int numV, double capV, List<Customer> customers, double[,,] distanceMatrix,Gamma[,,] distributionMatrix,IContinuousDistribution[,,] approximationMatrix, int numThreads,int numStarts, int numIterations, LocalSearchConfiguration configuration, int timeLimit = 30000)
        {
            (var allColumns, var bestSolution, var LSTime, var LSSCore) = await LocalSearchInstancAsync(name, numV, capV, customers, distanceMatrix, distributionMatrix, approximationMatrix, numThreads, numStarts, numIterations, timeLimit, configuration);
            //bestSolution.ForEach(x => Console.WriteLine(x));
            PrintRoutes(bestSolution);
            (var ilpSol, double ilpVal, double time) = SolveILP(allColumns, customers, numV, bestSolution);


            return (ilpSol, ilpVal, time, LSTime, LSSCore);

        }

        private void CheckRouteQualityVRPLTT(List<Route> routes, double[,,] distanceMatrix,double maxLoad)
        {
            //if (distanceMatrix.GetLength(2) > 1)
            //    throw new Exception("Only works for vrptw instances");


            foreach (var route in routes)
            {
                if (route.route.Count == 2)
                    continue;
                Console.WriteLine($"LS route score: {route.Score}");
                GRBEnv env = new GRBEnv();
                env.LogToConsole = 0;
                GRBModel model = new GRBModel(env);

                int numCust = route.route.Count - 1;

                GRBVar[,] edgeX = new GRBVar[route.route.Count - 1,route.route.Count - 1];
                GRBVar[] arrivalX = new GRBVar[numCust];
                GRBVar[,] vehicleWeight = new GRBVar[route.route.Count - 1, route.route.Count - 1];
                GRBVar[,,] loadLevelEdgeX = new GRBVar[route.route.Count -1, route.route.Count - 1,distanceMatrix.GetLength(2)];
                //GRBVar startTime = model.AddVar(0, double.MaxValue, 0, GRB.CONTINUOUS, "start_time");

                double llWidth = maxLoad / distanceMatrix.GetLength(2);
                double load = route.route.Sum(x => x.Demand);
                //double[] llLowerBound = new double[distanceMatrix.GetLength(2)];
                //double[] llUpperBound = new double[distanceMatrix.GetLength(2)];
                for (int i = 0; i < route.route.Count - 1; i++)
                {
                    arrivalX[i] = model.AddVar(0, 1000, 0, GRB.CONTINUOUS, $"y_{i}");
                    arrivalX[i].Start = route.arrival_times[i];

                    int loadLevel = (int)((Math.Max(0, load - 0.000001) / maxLoad) * distanceMatrix.GetLength(2));
                    load -= route.route[i].Demand;


                    if (loadLevel == distanceMatrix.GetLength(2))
                        loadLevel--;

                    for (int j =0; j < route.route.Count - 1; j++)
                    {
                        edgeX[i,j] = model.AddVar(0,1,0,GRB.BINARY, $"X{i}_{j}"); //(0, 1, GRB.BINARY, $"X{i}_{j}"
                        edgeX[i, j].Start = 0;
                        vehicleWeight[i, j] = model.AddVar(0, maxLoad, 0, GRB.CONTINUOUS, $"F{i}_{j}");
                        for(int l =0 ; l < distanceMatrix.GetLength(2); l++)
                        {
                            loadLevelEdgeX[i, j, l] = model.AddVar(0, 1, distanceMatrix[route.route[i].Id, route.route[j].Id, l], GRB.BINARY, $"z{i}_{j}_{l}");
                            loadLevelEdgeX[i, j, l].Start = 0;

                            //llLowerBound[l] =

                        }
                    }

                    //Warm start
                    if (i != numCust - 1)
                    {
                        edgeX[i, i + 1].Start = 1;
                        loadLevelEdgeX[i, i + 1, loadLevel].Start = 1;
                        vehicleWeight[i, i + 1].Start = load;
                    }
                    else
                    {
                        edgeX[i, 0].Start = 1;
                        loadLevelEdgeX[i,0, loadLevel].Start = 1;
                        vehicleWeight[i, 0].Start = 0;
                    }
                }

                for(int i = 0;i < numCust; i++)
                {

                    //model.AddConstr(route.route[i].TWStart <= arrivalX[i], "lowerTimewindowConstraint");
                    //model.AddConstr(route.route[i].TWEnd >= arrivalX[i], "upperTimewindowConstraint");

                    GRBLinExpr weightMatch = 0;
                    for(int j =0; j< numCust; j++)
                    {

                        weightMatch.AddTerm(1, vehicleWeight[j, i]);
                        weightMatch.AddTerm(-1, vehicleWeight[i, j]);

                        GRBLinExpr totalLLToEdge = 0;

                        GRBLinExpr llLowerBound = 0;
                        GRBLinExpr llUpperBound = 0;

                        GRBLinExpr llTravelTime = 0;
                        //GRB
                        for (int l = 0; l < distanceMatrix.GetLength(2); l++)
                        {
                            totalLLToEdge.AddTerm(1, loadLevelEdgeX[i, j, l]);


                            llLowerBound.AddTerm(l * llWidth,loadLevelEdgeX[i,j,l]);
                            llUpperBound.AddTerm((l+1) * llWidth, loadLevelEdgeX[i, j, l]);

                            llTravelTime.AddTerm(distanceMatrix[route.route[i].Id,route.route[j].Id,l],loadLevelEdgeX[i,j,l]);

                        }


                        double Mij = Math.Max(0,route.route[i].TWEnd + route.route[i].ServiceTime + distanceMatrix[route.route[i].Id, route.route[j].Id, distanceMatrix.GetLength(2)-1] - route.route[j].TWStart);

                        //Constraint 14
                        if (i != j && j != 0)
                        {
                            model.AddConstr(arrivalX[i] - arrivalX[j] + route.route[i].ServiceTime + llTravelTime <= Mij * (1 - edgeX[i, j]), $"Updating traveltimes ({i},{j})");
                            //model.AddConstr(arrivalX[i] - arrivalX[j] + route.route[i].ServiceTime + llTravelTime >= 0, $"Updating traveltimes ({i},{j}) LB");
                        }

                        //Constraint 12
                        model.AddConstr(totalLLToEdge == edgeX[i,j], $"loadLevelXMatchesEdgeX ({i},{j})");

                        //Constraint 13
                        model.AddConstr(llLowerBound <= vehicleWeight[i, j], $"weight constraint l ({i},{j})");
                        model.AddConstr(llUpperBound >= vehicleWeight[i, j], $"weight constraint u ({i},{j})");

                        //Constraint 11
                        model.AddConstr(route.route[j].Demand * edgeX[i, j] <= vehicleWeight[i, j], $"VehiclecapConst ({i},{j})");
                        model.AddConstr(vehicleWeight[i, j] <= (maxLoad-route.route[i].Demand) * edgeX[i,j], $"VehiclecapConstUp ({i},{j})");
                    }
                    //Constraint 10
                    if(i != 0)
                        model.AddConstr(weightMatch == route.route[i].Demand,"Match weight");
                }

                //Outgoing edges
                for (int i = 0; i < numCust; i++)
                {
                    GRBLinExpr custTotalOut = 0;
                    GRBLinExpr custTotalIn = 0;
                    for (int j = 0; j < numCust; j++)
                    {
                        custTotalOut.AddTerm(1, edgeX[i, j]);
                        custTotalIn.AddTerm(1, edgeX[j, i]);
                    }

                    //Constraint 9
                    model.AddConstr(custTotalOut == 1, $"CustIn{route.route[i].Id}");
                    //Constraint 8
                    model.AddConstr(custTotalIn == 1, $"CustOut{route.route[i].Id}");

                }

                ////Incomming edges
                //for (int i = 0; i < numCust; i++)
                //{
                //    GRBLinExpr custTotal = 0;

                //    for (int j = 0; j < numCust; j++)
                //        custTotal.AddTerm(1, edgeX[j, i]);
                //    model.AddConstr(custTotal == 1, $"CustOut{route.route[i].Id}");
                //}

                //timeWindows
                for(int i =1; i< arrivalX.Length; i++)
                {
                    //Constraint 15
                    model.AddConstr(arrivalX[i] <= route.route[i].TWEnd, $"u{route.route[i].Id}");
                    model.AddConstr(route.route[i].TWStart <= arrivalX[i], $"l{route.route[i].Id}");
                }


                model.ModelSense = GRB.MINIMIZE;

                //model.Feasibility();

                model.Optimize();

                List<(int, int, double,int)> r = new List<(int, int, double,int)>();

                for (int i =0;i< numCust; i++)
                {
                    for(int j =0; j< numCust; j++)
                    {
                        if (edgeX[i, j].X == 1)
                        {
                            int loadLevel = -1;
                            for (int l = 0; l < distanceMatrix.GetLength(2); l++)
                                if (loadLevelEdgeX[i, j, l].X == 1)
                                    loadLevel = l;
                            r.Add((route.route[i].Id, route.route[j].Id, vehicleWeight[i, j].X,loadLevel));
                        }

                            //Console.WriteLine($"Taking edge ({route.route[i].Id},{route.route[j].Id}) with weight ${vehicleWeight[i,j].X}");
                    }
                }
                r.Sort((x, y) => y.Item3.CompareTo(x.Item3));

                Console.WriteLine($"ILp route: ({String.Join(',', r.ConvertAll(x => $"{x.Item1}"))})");
                Console.WriteLine($"ILP loadlevels: ({String.Join(',', r.ConvertAll(x => $"{x.Item4}"))})");
                Console.WriteLine($"ILP score: {model.ObjVal}");

                var test = edgeX.Cast<GRBVar>().ToArray();
                //edgeX.CopyTo(test,0);
                var res = test.ToList().Any(x => x.Start != x.X);
                Console.WriteLine($"ILP found different route: {res}");
                //test.AddRange(edgeX.);

                //route.CalcObjective();
            }
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
                if (Math.Round(columnDecisions[i].X, 6) == 1)
                    solution.Add(columList[i]);
            }
            double val = model.ObjVal;
            double time = model.Runtime;
            model.Dispose();
            return (solution, val, time);
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
