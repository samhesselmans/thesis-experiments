﻿using MathNet.Numerics.Distributions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SA_ILP
{
    internal class LocalSearch
    {

        private static readonly object ConsoleWriterLock = new object();

        public double Temperature { get; private set; }


        public readonly LocalSearchConfiguration Config;

        private Random random;

        private OperatorSelector OS;

        public Dictionary<(int, RouteStore), (int, double)> BestPosCache;
        public List<RouteStore> RouteStores;

        public LocalSearch(LocalSearchConfiguration config, int seed, OperatorSelector os)
        {
            random = new Random(seed);
            Init(config, seed, os);
            Config = config;
        }


        private void Init(LocalSearchConfiguration config, int seed, OperatorSelector os)
        {
            Temperature = config.InitialTemperature;
            OS = os;
        }

        public LocalSearch(LocalSearchConfiguration config, int seed)
        {
            random = new Random(seed);
            var os = new OperatorSelector(random);
            Config = config;
            //os.Add(Operators.AddRandomRemovedCustomer, 1, "add");
            //os.Add(Operators.RemoveRandomCustomer, 1, "remove");
            //os.Add((routes, viableRoutes, random, removed, temp) => Operators.MoveRandomCustomerToRandomCustomer(routes, viableRoutes, random), 1, "move");
            //os.Add((routes, viableRoutes, random, removed, temp) => Operators.MoveRandomCustomerToRandomCustomer(routes, viableRoutes, random), 1, "move",100);

            os.Add(Operators.AddRandomRemovedCustomer, 1, "add",1); //repeated 1 time
            os.Add(Operators.RemoveRandomCustomer, 1, "remove",1); //repeated 1 time
            os.Add((routes, viableRoutes, random, removed, temp) => Operators.MoveRandomCustomerToRandomCustomer(routes, viableRoutes, random), 1, "move",1); //repeated 1 time
            os.Add((x, y, z, w, v) => Operators.GreedilyMoveRandomCustomer(x, y, z), 0.1, "move_to_best"); //repeated 1 time
            os.Add((x, y, z, w, v) => Operators.MoveRandomCustomerToRandomRoute(x, y, z), 1, "move_to_random_route",2); //repeated 4 times
            os.Add((x, y, z, w, v) => Operators.SwapRandomCustomers(x, y, z), 1, "swap",4); //repeated 4 times
            os.Add((x, y, z, w, v) => Operators.SwapInsideRoute(x, y, z), 1, "swap_inside_route",4); //repeated 4 times
            os.Add((x, y, z, w, v) => Operators.ReverseOperator(x, y, z), 1, "reverse"); //repeated 1 time
            os.Add((x, y, z, w, v) => Operators.ScrambleSubRoute(x, y, z), 1, "scramble"); //Repeated 1 time
            os.Add((x, y, z, w, v) => Operators.SwapRandomTails(x, y, z), 1, "swap_tails"); //Repeated 1 time
            Init(config, seed, os);

        }

        public static void StupidShuffle(List<Customer> customers, Random random, double timesShuffle = 1)
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

        private void CreateSmartInitialSolution(List<Route> routes, List<Customer> customers, List<Customer> removed)
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
                (double arrivalTime, IContinuousDistribution distribution) = route.CustomerDist(depot, seed, route.max_capacity, false);

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
                       ( double dist, IContinuousDistribution distribution) = route.CustomerDist(seed, x, route.max_capacity, false);

                        if (arrivalTime + dist < x.TWEnd)
                            if (arrivalTime + dist < x.TWStart)
                                return dist + route.CalculateEarlyPenaltyTerm(arrivalTime + dist, x.TWStart);
                            else
                                return dist + random.NextDouble() * 5;
                        else return double.MaxValue;

                    });
                    (double dist, IContinuousDistribution d) = route.CustomerDist(seed, next, route.max_capacity, false);
                    if (arrivalTime + dist > next.TWEnd || route.used_capacity + next.Demand > route.max_capacity)
                    {
                        seed = customers.MinBy(x => x.TWEnd);
                        break;
                    }
                    arrivalTime += dist;// + next.ServiceTime;

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


        private bool IsValidSolution(List<Route> routes, List<Customer> removed)
        {
            //Always require all customers to be served for a route to be valid
            if (removed.Count != 0)
                return false;
            bool upperViolations = routes.Exists(x => x.ViolatesUpperTimeWindow);
            bool lowerViolations = routes.Exists(x => x.ViolatesLowerTimeWindow);

            return (!upperViolations || Config.AllowLateArrival) && (!lowerViolations || Config.AllowEarlyArrival);
        }

        private bool IsValidRoute(Route route)
        {
            return (!route.ViolatesUpperTimeWindow || Config.AllowLateArrival) && (!route.ViolatesLowerTimeWindow || Config.AllowEarlyArrival);
        }

        private void RunAndCheckOperator(int id, List<Route> routes, List<Customer> removed, double imp, Action op)
        {
            double expectedVal = 0;
            List<Route>? beforeCopy = null;
            if (Config.CheckOperatorScores)
                expectedVal = Solver.CalcTotalDistance(routes, removed, this) - imp;
            if (Config.SaveRoutesBeforeOperator)
            {
                beforeCopy = routes.ConvertAll(i => i.CreateDeepCopy());
            }

            op();
            if (Config.CheckOperatorScores)
                if (Math.Round(Solver.CalcTotalDistance(routes, removed, this), 6) != Math.Round(expectedVal, 6))
                    Solver.ErrorPrint($"{id}: ERROR expected {expectedVal} not equal to {Solver.CalcTotalDistance(routes, removed, this)} with imp: {imp}. Diff:{expectedVal - Solver.CalcTotalDistance(routes, removed, this)} , OP: {OS.LastOperator}");
        }

        private void HandleRoutStores(List<Route> routes, HashSet<RouteStore> Columns)
        {
            //RouteStores = new List<RouteStore>(routes.Count);
            foreach (Route route in routes)
            {
                var rs = new RouteStore(route.CreateIdList(), route.Score);
                //RouteStores.Add(rs);

                route.RouteStore = rs;

                if (IsValidRoute(route))
                    Columns.Add(rs);

            }
        }

        public (HashSet<RouteStore>, List<Route>, double) LocalSearchInstance(int id, string name, int numVehicles, double vehicleCapacity, List<Customer> customers, double[,,] distanceMatrix,Gamma[,,] distributionMatrix,IContinuousDistribution[,,] approximationMatrix, int numInterations = 3000000, int timeLimit = 30000, bool checkInitialSolution = false)
        {
            Console.WriteLine("Starting local search");
            //customers.Sort(1, customers.Count - 1, delegate (Customer x, Customer y) { x.TWEnd.CompareTo(y.TWEnd); });
            //var c = new TWCOmparer();
            //customers.Sort(1, customers.Count - 1, c);



            StupidShuffle(customers, random);

            List<Route> routes = new List<Route>();
            List<Customer> removed = new List<Customer>();
            //const double initialTemp = 30.0;

            //Generate routes
            for (int i = 0; i < numVehicles; i++)
                routes.Add(new Route(customers[0], distanceMatrix,distributionMatrix, approximationMatrix, vehicleCapacity,seed: random.Next(), this));

            CreateSmartInitialSolution(routes, customers, removed);


            if (checkInitialSolution)
                //Validate intial solution
                foreach (Route route in routes)
                    if (route.CheckRouteValidity())
                    {
                        Console.WriteLine("Initiele oplossing niet valid");
                        throw new Exception();
                    }

            Console.WriteLine($"Finished making initial solution with value {Solver.CalcTotalDistance(routes, removed, this).ToString("0.000")}");

            int amtImp = 0, amtWorse = 0, amtNotDone = 0;
            //double temp = initialTemp;
            double totalP = 0;
            double countP = 0;
            Stopwatch timer = new Stopwatch();
            timer.Start();
            Stopwatch printTimer = new Stopwatch();
            printTimer.Start();

            //Used to keep track of the routes which contain customers. This speeds up execution as this only needs to be updated when a change is applied
            List<int> viableRoutes = Enumerable.Range(0, routes.Count).Where(i => routes[i].route.Count > 2).ToList();

            int lastChangeAcceptedOnIt = 0;

            //All saved columns
            HashSet<RouteStore> Columns = new HashSet<RouteStore>();

            //The best found solution so far
            List<Route> BestSolution = routes.ConvertAll(i => i.CreateDeepCopy());
            List<Customer> BestSolutionRemoved = removed.ConvertAll(x => x);

            List<(int, double)> SearchScores = new List<(int, double)>();
            List<(int, double)> BestSolutionScores = new List<(int, double)>();


            //Dictionaries used for tracking the performance of the different operators. These are only used if the option is enabled in the configuration
            Dictionary<string, int> OPImprovementCount = new Dictionary<string, int>();
            Dictionary<string, double> OPImprovementTotal = new Dictionary<string, double>();
            Dictionary<string, double> OPBestImprovement = new Dictionary<string, double>();

            Dictionary<string, int> OPAcceptedWorseCount = new Dictionary<string, int>();
            Dictionary<string, double> OPAcceptedWorseTotal = new Dictionary<string, double>();
            Dictionary<string, int> OPNotPossible = new Dictionary<string, int>();


            //Initialize the Caching for best position
            BestPosCache = new Dictionary<(int, RouteStore), (int, double)>();

            foreach (string op in OS.OperatorList)
            {
                OPImprovementCount[op] = 0;
                OPImprovementTotal[op] = 0;
                OPBestImprovement[op] = 0;
                OPAcceptedWorseTotal[op] = 0;
                OPAcceptedWorseCount[op] = 0;
                OPNotPossible[op] = 0;
            }

            //routes.ForEach(x => { if (x.route.Count != 2) Console.WriteLine(x); });

            double bestSolValue = Solver.CalcTotalDistance(BestSolution, removed, this);
            double currentValue = bestSolValue;
            int bestImprovedIteration = 0;
            int restartPreventionIteration = 0;
            int numRestarts = 0;
            int previousUpdateIteration = 0;
            int iteration = 0;
            for (; iteration < numInterations && timer.ElapsedMilliseconds <= timeLimit; iteration++)
            {
                //double p = random.NextDouble();

                //if(iteration - restartPreventionIteration == 7500 && numRestarts == 1)
                //{
                //    Console.WriteLine($"Current routes: {currentValue}");
                //    Solver.PrintRoutes(routes);
                //    Console.WriteLine($"Best Solution: {bestSolValue}");
                //    Solver.PrintRoutes(BestSolution);

                //    break;
                //}

                double imp = 0;
                Action? act = null;
                var nextOperator = OS.Next();
                (imp, act) = nextOperator(routes, viableRoutes, random, removed, this);

                if (act != null)
                {
                    if (currentValue > double.MaxValue - 1000000)
                        Console.WriteLine("OVERFLOW");
                    //Accept all improvements
                    if (imp > 0)
                    {


                        //Store operator improvement results
                        OPImprovementCount[OS.LastOperator] += 1;
                        OPImprovementTotal[OS.LastOperator] += imp;

                        if (imp > OPBestImprovement[OS.LastOperator])
                            OPBestImprovement[OS.LastOperator] = imp;

                        //Runs and checks the operator scores. Checks only occur if configured that way
                        RunAndCheckOperator(id, routes, removed, imp, act);

                        //Need to recalculate the viable routes cache
                        viableRoutes = Enumerable.Range(0, routes.Count).Where(i => routes[i].route.Count > 2).ToList();

                        currentValue -= imp;
                        if (id == 0 && Config.SaveScoreDevelopment)
                            SearchScores.Add((iteration, currentValue));


                        if (Math.Round(currentValue,6) < Math.Round(bestSolValue,6) && removed.Count == 0 && IsValidSolution(routes, removed))
                        {
                            //New best solution found
                            bestSolValue = currentValue;
                            bestImprovedIteration = iteration;
                            restartPreventionIteration = iteration;
                            BestSolution = routes.ConvertAll(i => i.CreateDeepCopy());
                            if (id == 0 && Config.SaveScoreDevelopment)
                                BestSolutionScores.Add((iteration, bestSolValue));

                            //Now we know all customers are used
                            BestSolutionRemoved = new List<Customer>();
                            
                            HandleRoutStores(routes,Columns);

                        }

                        //Add columns
                        if (Config.SaveColumnsAfterAllImprovements && Temperature < Config.InitialTemperature * Config.SaveColumnThreshold)
                            HandleRoutStores(routes, Columns);


                        amtImp += 1;
                        lastChangeAcceptedOnIt = iteration;

                    }

                    else
                    {
                        double acceptP = Math.Exp(imp / Temperature);
                        totalP += acceptP;
                        countP += 1;
                        if (random.NextDouble() <= acceptP)
                        {
                            //Worse solution accepted
                            amtWorse += 1;


                            OPAcceptedWorseCount[OS.LastOperator] += 1;
                            OPAcceptedWorseTotal[OS.LastOperator] += imp;

                            RunAndCheckOperator(id, routes, removed, imp, act);


                            if (Config.SaveColumnsAfterWorse && Temperature < Config.InitialTemperature * Config.SaveColumnThreshold)
                                HandleRoutStores(routes, Columns);

                            viableRoutes = Enumerable.Range(0, routes.Count).Where(i => routes[i].route.Count > 2).ToList();
                            currentValue -= imp;
                            if (id == 0 && Config.SaveScoreDevelopment)
                                SearchScores.Add((iteration, currentValue));

                            lastChangeAcceptedOnIt = iteration;
                        }
                        //else if (OS.LastOperator == "remove")
                        //{
                        //    Console.WriteLine(imp);
                        //}
                    }
                }
                else
                {
                    OPNotPossible[OS.LastOperator] += 1;
                    amtNotDone += 1;
                }
                if (iteration % Config.IterationsPerAlphaChange == 0 && iteration != 0)
                {
                    Temperature *= Config.Alpha;

                    //Reset cache as scores could have changed
                    routes.ForEach(x => x.ResetCache());
                    BestPosCache = new Dictionary<(int, RouteStore), (int, double)>();

                    //TOD: Seperate penalty calculation for optimization
                    currentValue = Solver.CalcTotalDistance(routes, removed, this);
                    bestSolValue = Solver.CalcTotalDistance(BestSolution, BestSolutionRemoved, this);
                }
                //Check if the restart conditions are met
                if (iteration - restartPreventionIteration > Config.NumIterationsOfNoChangeBeforeRestarting && Temperature < Config.RestartTemperatureBound && numRestarts < Config.NumRestarts) //&& iteration - lastChangeExceptedOnIt > 1000
                {
                    numRestarts += 1;
                    restartPreventionIteration = iteration;
                    //Restart
                    var oldTemp = Temperature;
                    Temperature += Config.InitialTemperature / 3;
                    routes.ForEach(x => x.ResetCache());
                    routes = BestSolution.ConvertAll(i => i.CreateDeepCopy());
                    viableRoutes = Enumerable.Range(0, routes.Count).Where(i => routes[i].route.Count > 2).ToList();
                    removed = BestSolutionRemoved.ConvertAll(x => x);

                    //Reset the scores
                    currentValue = Solver.CalcTotalDistance(routes, removed, this);
                    bestSolValue = Solver.CalcTotalDistance(BestSolution, new List<Customer>(), this);
                    Console.WriteLine($"{id}:Best solution changed to long ago a T: {oldTemp}. Restarting from best solution with T: {Temperature}");
                }
                else if (Temperature < 0.02 && numRestarts >= Config.NumRestarts)
                    break;
                if (printTimer.ElapsedMilliseconds > 3 * 1000)
                {
                    var elapsed = printTimer.ElapsedMilliseconds;
                    printTimer.Restart();
                    double itsPerSecond = (iteration - previousUpdateIteration) / ((double)elapsed / 1000);
                    previousUpdateIteration = iteration;
                    int cnt = routes.Count(x => x.route.Count > 2);
                    Console.WriteLine($"{id}: T: {Temperature.ToString("0.000")}, S: {Solver.CalcTotalDistance(routes, removed, this).ToString("0.000")}, TS: {currentValue.ToString("0.000")}, N: {cnt}, IT: {iteration}, LA {iteration - lastChangeAcceptedOnIt}, B: {bestSolValue},{Solver.CalcTotalDistance(BestSolution,BestSolutionRemoved,this)}, BI: {bestImprovedIteration}, IT/s: {itsPerSecond.ToString("0.00")}/s");
                    //foreach (var rout in BestSolution)
                    //    if(rout.route.Count > 2)
                    //    Console.WriteLine(rout);
                }
            }

            //Saving the columns of the best solutions
            foreach (Route route in BestSolution)
            {
                if (IsValidRoute(route))
                    Columns.Add(new RouteStore(route.CreateIdList(), route.Score));

            }
            if (id == 0 && Config.SaveScoreDevelopment)
                BestSolutionScores.Add((iteration, bestSolValue));

            Console.WriteLine($"DONE {id}: {name}, Score: {Solver.CalcTotalDistance(BestSolution, new List<Customer>(), this)}, Columns: {Columns.Count}. Completed {iteration} iterations in {Math.Round((double)timer.ElapsedMilliseconds / 1000, 3)}s");

            if (Config.PrintExtendedInfo)
            {
                lock (ConsoleWriterLock)
                {
                    Console.WriteLine($"  {id}: Total: {amtNotDone + amtImp + amtWorse}, improvements: {amtImp}, worse: {amtWorse}, not done: {amtNotDone}");
                    Console.WriteLine($" {id}: OPERATOR PERFORMANCE OVERVIEW");
                    foreach (string op in OS.OperatorList)
                    {
                        string spaces = "";
                        if (op.Length < 20)
                            spaces = new string(' ', 20 - op.Length);
                        Console.WriteLine($"\t{op}:{spaces} ICnt: {OPImprovementCount[op]}, AI: {(OPImprovementTotal[op] / OPImprovementCount[op]).ToString("0.00000")}, B: {(OPBestImprovement[op]).ToString("0.00000")}, WCnt: {OPAcceptedWorseCount[op]}, AW: {(OPAcceptedWorseTotal[op] / OPAcceptedWorseCount[op]).ToString("0.00000")}, NP : {OPNotPossible[op]} ");
                    }
                }

            }
            if (id == 0 && Config.SaveScoreDevelopment)
            {
                Console.WriteLine("Saving scores");
                System.IO.File.WriteAllLines("SearchScores.txt", SearchScores.ConvertAll(x => $"{x.Item1};{x.Item2}"));
                File.WriteAllLines("BestScores.txt", BestSolutionScores.ConvertAll(x => $"{x.Item1};{x.Item2}"));
                Console.WriteLine("Done saving scores");
            }
            return (Columns, BestSolution, Solver.CalcTotalDistance(BestSolution, removed, this));
        }
    }


}
