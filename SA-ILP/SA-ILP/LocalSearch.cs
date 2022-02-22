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


        public double Temperature { get;private set; }
        public double InitialTemperature { get; private set; }

        public bool AllowLateArrivalDuringSearch { get; private set; }
        public bool AllowEarlyArrivalDuringSearch { get; private set; }

        public bool AllowLateArrival { get; private set; }
        public bool AllowEarlyArrival { get; private set; }

        //public static readonly double BaseRemovedCustomerPenalty = 150;
        //public static readonly double BaseRemovedCustomerPenaltyPow = 1.5;

        public double BaseRemovedCustomerPenalty { get; private set; }
        public double BaseRemovedCustomerPenaltyPow { get; private set; }

        public double BaseEarlyArrivalPenalty { get; private set; }
        public double BaseLateArrivalPenalty { get; private set; }

        public double Alpha { get; private set; }

        public bool SaveColumnsDuringLocalSearch { get; private set; }

        public bool PenalizeEarlyArrival { get; set; }
        public bool PenalizeLateArrival { get; set; }

        private Random random;

        private OperatorSelector OS;

        public LocalSearch(LocalSearchConfiguration config, int seed)
        {
            Temperature = config.InitialTemperature;
            InitialTemperature = config.InitialTemperature;
            this.AllowEarlyArrival = config.AllowEarlyArrival;
            this.AllowLateArrival = config.AllowLateArrival;
            AllowEarlyArrivalDuringSearch = config.AllowEarlyArrivalDuringSearch;
            AllowLateArrivalDuringSearch = config.AllowLateArrivalDuringSearch;
            BaseRemovedCustomerPenalty = config.BaseRemovedCustomerPenalty;
            BaseRemovedCustomerPenaltyPow = config.BaseRemovedCustomerPenaltyPow;
            BaseEarlyArrivalPenalty = config.BaseEarlyArrivalPenalty;
            BaseLateArrivalPenalty = config.BaseLateArrivalPenalty;
            PenalizeLateArrival = config.PenalizeLateArrival;
            PenalizeEarlyArrival = config.PenalizeEarlyArrival;
            Alpha = config.Alpha;
            random = new Random(seed);
            OS = new OperatorSelector(random);

            OS.Add(Operators.AddRandomRemovedCustomer, 1);
            OS.Add(Operators.RemoveRandomCustomer, 1);
            OS.Add((x, y, z, w, v) => Operators.MoveRandomCustomerToRandomCustomer(x, y, z), 1);
            OS.Add((x, y, z, w, v) => Operators.GreedilyMoveRandomCustomer(x, y, z), 0.1);
            OS.Add((x, y, z, w, v) => Operators.MoveRandomCustomer(x, y, z), 1);
            OS.Add((x, y, z, w, v) => Operators.SwapRandomCustomers(x, y, z), 1);
            OS.Add((x, y, z, w, v) => Operators.ReverseOperator(x, y, z), 1);
        }
        private void StupidShuffle(List<Customer> customers, double timesShuffle = 1)
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


        private bool IsValidSolution(List<Route> routes,List<Customer> removed)
        {
            if (removed.Count != 0)
                return false;
            bool upperViolations = routes.Exists(x => x.ViolatesUpperTimeWindow);
            bool lowerViolations = routes.Exists(x => x.ViolatesLowerTimeWindow);

            return (!upperViolations|| AllowLateArrival) && (!lowerViolations|| AllowEarlyArrival);
        }

        private bool IsValidRoute(Route route)
        {
            return (!route.ViolatesUpperTimeWindow || AllowLateArrival) && (!route.ViolatesLowerTimeWindow || AllowEarlyArrival);
        }

        public (HashSet<RouteStore>, List<Route>, double) LocalSearchInstance(int id, string name, int numVehicles, double vehicleCapacity, List<Customer> customers, double[,,] distanceMatrix, bool printExtendedInfo = false, int numInterations = 3000000, int timeLimit = 30000, bool checkInitialSolution = true)
        {
            Console.WriteLine("Starting local search");
            //customers.Sort(1, customers.Count - 1, delegate (Customer x, Customer y) { x.TWEnd.CompareTo(y.TWEnd); });
            //var c = new TWCOmparer();
            //customers.Sort(1, customers.Count - 1, c);

            

            StupidShuffle(customers);

            List<Route> routes = new List<Route>();
            List<Customer> removed = new List<Customer>();
            //const double initialTemp = 30.0;

            //Generate routes
            for (int i = 0; i < numVehicles; i++)
                routes.Add(new Route(customers[0], distanceMatrix, vehicleCapacity, seed: random.Next(),this));

            CreateSmartInitialSolution(routes, customers, removed);


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
            //double temp = initialTemp;
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

            double bestSolValue = Solver.CalcTotalDistance(BestSolution, removed, Temperature);
            double currentValue = bestSolValue;
            int bestImprovedIteration = 0;
            int numRestarts = 0;
            int previousUpdateIteration = 0;
            int iteration = 0;
            for (; iteration < numInterations && timer.ElapsedMilliseconds <= timeLimit; iteration++)
            {
                double p = random.NextDouble();
                double imp = 0;
                Action? act = null;
                var nextOperator = OS.Next();
                (imp, act) = nextOperator(routes, viableRoutes, random, removed, Temperature);

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
                        if (currentValue < bestSolValue && removed.Count == 0 && IsValidSolution(routes,removed))
                        {
                            //New best solution found
                            bestSolValue = currentValue;
                            bestImprovedIteration = iteration;
                            BestSolution = routes.ConvertAll(i => i.CreateDeepCopy());

                            foreach (Route route in routes)
                            {
                                if (IsValidRoute(route))
                                    Columns.Add(new RouteStore(route.CreateIdList(), route.Score));

                            }

                        }

                        //Add columns
                        if(SaveColumnsDuringLocalSearch)
                            foreach (Route route in routes)
                            {
                                if (IsValidRoute(route))
                                    Columns.Add(new RouteStore(route.CreateIdList(), route.Score));

                            }


                        amtImp += 1;
                        lastChangeExceptedOnIt = iteration;

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
                    Temperature *= Alpha;

                    //routes.ForEach(x => x.Temperature = Temperature);
                    //TOD: Seperate penalty calculation for optimization
                    currentValue = Solver.CalcTotalDistance(routes, removed, Temperature);
                }
                if (iteration - bestImprovedIteration > 1000000 * (Math.Pow(numRestarts * 3 + 1, 2)) && Temperature < 0.03)
                {
                    numRestarts += 1;
                    //Restart
                    Temperature = 30;
                    //routes.ForEach(x => x.Temperature = Temperature);
                    routes = BestSolution.ConvertAll(i => i.CreateDeepCopy());
                    viableRoutes = Enumerable.Range(0, routes.Count).Where(i => routes[i].route.Count > 2).ToList();
                    removed = new List<Customer>();
                    currentValue = Solver.CalcTotalDistance(routes, removed, Temperature);
                    Console.WriteLine($"{id}:Best solution changed to long ago. Restarting from best solution with T: {Temperature}");
                }
                if (printTimer.ElapsedMilliseconds > 3 * 1000)
                {
                    var elapsed = printTimer.ElapsedMilliseconds;
                    printTimer.Restart();
                    double itsPerSecond = (iteration - previousUpdateIteration) / ((double)elapsed / 1000);
                    previousUpdateIteration = iteration;
                    int cnt = routes.Count(x => x.route.Count > 2);
                    Console.WriteLine($"{id}: T: {Temperature.ToString("0.000")}, S: {Solver.CalcTotalDistance(routes, removed, Temperature).ToString("0.000")}, TS: {currentValue.ToString("0.000")}, N: {cnt}, IT: {iteration}, LA {iteration - lastChangeExceptedOnIt}, B: {bestSolValue.ToString("0.000")}, BI: {bestImprovedIteration}, IT/s: {itsPerSecond.ToString("0.00")}/s");
                }
            }

            //Saving the columns of the best solutions
            foreach (Route route in BestSolution)
            {
                if (IsValidRoute(route))
                    Columns.Add(new RouteStore(route.CreateIdList(), route.Score));

            }


            Console.WriteLine($"DONE {id}: {name}, Score: {Solver.CalcTotalDistance(BestSolution, new List<Customer>(), Temperature)}, Columns: {Columns.Count}. Completed {iteration} iterations in {Math.Round((double)timer.ElapsedMilliseconds / 1000, 3)}s");
#if DEBUG
            Console.WriteLine(routes.Sum(x => x.numReference));
#endif
            if (printExtendedInfo)
                Console.WriteLine($"  {id}: Total: {amtNotDone + amtImp + amtWorse}, improvements: {amtImp}, worse: {amtWorse}, not done: {amtNotDone}");
            return (Columns, BestSolution, Solver.CalcTotalDistance(BestSolution, removed, Temperature));
        }
    }

    public struct LocalSearchConfiguration
    {
        public double InitialTemperature { get;  set; }

        public bool AllowLateArrivalDuringSearch { get;  set; }
        public bool AllowEarlyArrivalDuringSearch { get;  set; }

        public bool AllowLateArrival { get;  set; }
        public bool AllowEarlyArrival { get;  set; }

        public double BaseRemovedCustomerPenalty { get;  set; }
        public double BaseRemovedCustomerPenaltyPow { get;  set; }
        public double BaseEarlyArrivalPenalty { get;  set; }
        public double BaseLateArrivalPenalty { get;  set; }

        public double Alpha { get; set; }

        public bool SaveColumnsDuringLocalSearch { get; set; }

        public bool PenalizeEarlyArrival { get; set; }
        public bool PenalizeLateArrival { get; set; }
    }

    public static class LocalSearchConfigs
    {
        public static LocalSearchConfiguration VRPLTT => new LocalSearchConfiguration 
        {   
            InitialTemperature = 30, 
            AllowEarlyArrivalDuringSearch = true, 
            AllowLateArrivalDuringSearch = true, 
            AllowEarlyArrival = true, 
            AllowLateArrival = false, 
            BaseEarlyArrivalPenalty = 100, 
            BaseLateArrivalPenalty = 100, 
            BaseRemovedCustomerPenalty = 150, 
            BaseRemovedCustomerPenaltyPow = 1.5, 
            Alpha = 0.98, 
            SaveColumnsDuringLocalSearch = false, 
            PenalizeEarlyArrival =true, 
            PenalizeLateArrival = true 
        };

        public static LocalSearchConfiguration VRPTW => new LocalSearchConfiguration 
        {   
            InitialTemperature = 30, 
            AllowEarlyArrivalDuringSearch = true,
            AllowLateArrivalDuringSearch = true, 
            AllowEarlyArrival = true, 
            AllowLateArrival = false, 
            BaseEarlyArrivalPenalty = 100, 
            BaseLateArrivalPenalty = 100, 
            BaseRemovedCustomerPenalty = 150, 
            BaseRemovedCustomerPenaltyPow = 1.5, 
            Alpha = 0.98, 
            SaveColumnsDuringLocalSearch = false, 
            PenalizeEarlyArrival = false,
            PenalizeLateArrival = true
        };


        //public static List<Operator> SimpleOperators => new List<Func<List<Route>, List<int>, Random, List<Customer>, double, (double, Action?)>>() {Operators.AddRandomRemovedCustomer,Operators. };

        //public static (List<Operator>, List<double>) SimpleOperators
        //{
        //    get
        //    {
        //        return CreateSimpleOperators();
        //    }
        //}

        //private static (List<Operator>, List<double>) CreateSimpleOperators()
        //{

        //}

    }
}
