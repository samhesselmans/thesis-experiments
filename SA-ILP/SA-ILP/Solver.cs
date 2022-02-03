using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace SA_ILP
{
    public class Customer
    {
        public int Id { get; private set; }
        public int X { get; private set; }
        public int Y { get; private set; }
        public double Demand    { get; private set; }
        public double TWStart { get; private set; }
        public double TWEnd { get; private set; }

        public double ServiceTime { get; private set; }

        public Customer(int id, int x, int y, double demand, double twstart, double twend,double serviceTime)
        {
            this.Id = id;
            this.X = x;
            this.Y = y;
            this.Demand = demand;
            this.TWStart = twstart;
            this.TWEnd = twend;
            this.ServiceTime = serviceTime;

        }
    }

    class RouteStore
    {
       public List<int> Route { get; private set; }

      public RouteStore(List<int> route)
        {
            this.Route = route;
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

        //Programmed to efficiently support up to 1000 customers. If more customers are in the problem this might integer overflows and more hash matches.
        public override int GetHashCode()
        {
            int total = 0;
            for( int i=0; i< Route.Count; i++)
            {
                total += Route[i] * (i +1001); 
            }
            return total;
            //return Route.Sum(x=>);//String.Join(";", Route).GetHashCode();
        }
    }
    class Route
    {
        public List<Customer> route;
        public List<double> arrival_times;
        public double[,] distance_matrix;
        public double time_done;
        //public Customer lastCust;
        public double used_capacity;
        public double max_capacity;
        private Random random;

        public long numReference = 0;
        public Route(Customer depot,double[,] distanceMatrix,double maxCapacity)
        {
            this.route = new List<Customer>() { depot, depot };
            this.arrival_times = new List<double>() { 0,0};
            this.distance_matrix = distanceMatrix;
            this.time_done = 0;
            //this.lastCust = depot;
            used_capacity = 0;
            this.max_capacity = maxCapacity;
            random = new Random();

        }

        public Route(List<Customer> route, List<double> arrivalTimes, double[,] distanceMatrix, double usedCapcity, double maxCapacity)
        {
            this.route = route;
            this.arrival_times = arrivalTimes;
            this.distance_matrix = distanceMatrix;
            this.time_done = 0;
            used_capacity = usedCapcity;
            this.max_capacity = maxCapacity;
            random = new Random();

        }

        public double CalcDist()
        {
            var total_dist = 0.0;
            for(int i = 0;i < route.Count-1; i++)
            {
                total_dist += this.CustomerDist(route[i], route[i + 1]);
            }
            return total_dist;
        }

        public double CustomerDist(Customer cust1, Customer cust2)
        {
            numReference += 1;
            if(cust1.Id < cust2.Id)
            {
                return distance_matrix[cust1.Id,cust2.Id];
            }
            else
            {
                return distance_matrix[cust2.Id, cust1.Id];
            }
        }

        public void RemoveCust(Customer cust)
        {
            double newArriveTime = 0;
            int index = -1;
            Customer lastCust = null;
            Customer previous_cust = route[0];
            for(int i = 1; i < route.Count-1; i++)
            {
                var c = route[i];
                if(c != cust)
                {
                    var dist = CustomerDist(previous_cust, c);
                    if(newArriveTime + dist < c.TWStart)
                        newArriveTime = c.TWStart;
                    else
                        newArriveTime += dist;
                    arrival_times[i] = newArriveTime;
                    newArriveTime += c.ServiceTime;
                    lastCust = c;
                    previous_cust = c;
                }
                else
                    index = i;
            }
            route.RemoveAt(index);
            arrival_times.RemoveAt(index);
            //this.lastCust = lastCust;
            this.used_capacity -= cust.Demand;

        }

        public (bool,bool,double) CustPossibleAtPos(Customer cust, int pos,int skip=0)
        {
            if (arrival_times[pos - 1] + route[pos - 1].ServiceTime > cust.TWEnd)
                return (false, false, double.MinValue);

            double TArrivalNewCust = arrival_times[pos-1] + route[pos-1].ServiceTime + CustomerDist(cust, route[pos-1]);
            if (TArrivalNewCust > cust.TWEnd)
                return (false, false, double.MinValue);

            if (TArrivalNewCust < cust.TWStart)
                TArrivalNewCust = cust.TWStart;
            double newArrivalTime = TArrivalNewCust + CustomerDist(cust,route[pos+skip]) + cust.ServiceTime;
            for(int i = pos + skip; i < route.Count; i++)
            {
                if (newArrivalTime > route[i].TWEnd)
                    return (false, true, double.MinValue);
                if(newArrivalTime < route[i].TWStart)
                    newArrivalTime = route[i].TWStart;
                if (i != route.Count - 1)
                    newArrivalTime += CustomerDist(route[i], route[i + 1]) + route[i].ServiceTime;
            }
            double distIncrease = CustomerDist(route[pos - 1], cust) + CustomerDist(route[pos + skip], cust) - CustomerDist(route[pos - 1], route[pos]);

            //Kan dit geen problemen veroorzaken?
            for(int i=0;i< skip; i++)
            {
                distIncrease -= CustomerDist(route[pos + i],route[pos + i + 1]);
            }

            return (true, true, distIncrease);
        }

        public (int,double) BestPossibleInsert(Customer cust)
        {
            double bestDistIncr = double.MaxValue;
            int bestIndex = -1;
            if (this.used_capacity + cust.Demand > max_capacity)
                return (bestIndex, bestDistIncr);
            for (int i = 1; i < route.Count; i++)
            {
                (bool possible, bool everPossible, double distIncrease) = CustPossibleAtPos(cust, i);
                if (!everPossible)
                    break;
                if(possible)
                    if(distIncrease<bestDistIncr)
                        bestDistIncr = distIncrease;
                        bestIndex =i;

            }
            return (bestIndex, bestDistIncr);
        }

        public (Customer?,double) RandomCust()
        {
            if (route.Count == 2)
                return (null, double.MaxValue);

            var i = random.Next(1, route.Count - 1);
            return (route[i], CustomerDist(route[i - 1], route[i]) + CustomerDist(route[i + 1], route[i]) - CustomerDist(route[i - 1], route[i + 1]));
        }

        public (Customer?, int) RandomCustIndex()
        {
            if (route.Count == 2)
                return (null, -1);

            var i = random.Next(1, route.Count - 1);
            return (route[i], i);
        }

        public void InsertCust(Customer cust, int pos)
        {
            double TArrivalNewCust = arrival_times[pos - 1] + route[pos - 1].ServiceTime + CustomerDist(cust, route[pos - 1]);
            if(TArrivalNewCust < cust.TWStart)
                TArrivalNewCust = cust.TWStart;
            double newArrivalTime = TArrivalNewCust + CustomerDist(route[pos], cust) + cust.ServiceTime;
            for(int i=pos; i< route.Count; i++)
            {
                if (newArrivalTime < route[i].TWStart)
                    newArrivalTime = route[i].TWStart;
                arrival_times[i] = newArrivalTime;
                if (i != route.Count - 1)
                    newArrivalTime += CustomerDist(route[i], route[i + 1]) + route[i].ServiceTime;
            }
            arrival_times.Insert(pos, TArrivalNewCust);
            route.Insert(pos, cust);
            used_capacity += cust.Demand;
        }

        public (bool,double,int) CanSwap(Customer cust1, Customer cust2, int index)
        {
            //for (int i=0; i < route.Count - 1; i++)
            //{
            //    if (route[i] == cust1)
            //    {
                    (bool possible, _, double distIncrease) = CustPossibleAtPos(cust2, index, 1);
                    possible &= (used_capacity - cust1.Demand + cust2.Demand < max_capacity);
                    return (possible, distIncrease, index);
            //    }
            //}
            //return (false,double.MaxValue,-1);
        }

        public bool CheckRouteValidity()
        {
            double arrivalTime = 0;
            bool failed = false;
            double usedCapacity = 0;
            for(int i=0;i < route.Count - 1; i++)
            {
                usedCapacity += route[i + 1].Demand;
                if(usedCapacity > max_capacity)
                {
                    failed = true;
                    Console.WriteLine($"FAIL exceeded vehicle capacity {route}");
                }
                if(arrivalTime > route[i].TWEnd)
                {
                    failed = true;
                    Console.WriteLine($"FAIL did not meet customer {route[i].Id}:{route[i]} due date. Arrived on {arrivalTime} on route {route}");
                }
                if(arrivalTime < route[i].TWStart)
                    arrivalTime = route[i].TWStart;
                if(arrivalTime < arrival_times[i] - Math.Pow(10,-9) || arrivalTime > arrival_times[i] + Math.Pow(10, -9))
                {
                    Console.WriteLine($"FAIL arrival times did not match {arrivalTime} and {arrival_times[i]} for cust {route[i].Id} on route {route}");
                    failed = true;
                }
                arrivalTime += Math.Sqrt(Math.Pow(route[i].X - route[i + 1].X, 2) + Math.Pow(route[i].Y - route[i + 1].Y, 2)) + route[i].ServiceTime;

            }
            return failed;
        }

        public Route CreateDeepCopy()
        {
            return new Route(route.ConvertAll(i=>i),arrival_times.ConvertAll(i => i), distance_matrix,used_capacity,max_capacity);
        }

        public List<int> CreateIdList()
        {
            return route.ConvertAll(i => i.Id);
        }

    }

    internal class Solver
    {
        Random random;
        private double CalcTotalDistance(List<Route> routes)
        {
            return routes.Sum(x => x.CalcDist());
        }


        private (double,Action?) SwapRandomCustomers(List<Route> routes, List<int> viableRoutes)
        {
            int bestDest = -1, bestSrc = -1, bestPos1 = -1, bestPos2 = - 1;
            Customer bestCust1 = null, bestCust2 = null;
            double bestImp = double.MinValue;

            //viableRoutes = Enumerable.Range(0, routes.Count).Where(i => routes[i].route.Count > 2).ToList();
            var numRoutes = viableRoutes.Count;
            for (int i = 0; i < 4; i++)
            {
                //Select destination
                int dest = viableRoutes[random.Next(numRoutes)];

                //Select src excluding destination
                //var range = Enumerable.Range(0, numRoutes).Where(i => i != dest).ToList();
                //int src = range[random.Next(numRoutes - 1)];
                int src = random.Next(numRoutes - 1);
                if (src >= dest)
                    src += 1;

                (var cust1, int index1) = routes[src].RandomCustIndex();
                (var cust2, int index2) = routes[dest].RandomCustIndex();


                if(cust1 != null && cust2 != null)
                {
                    (bool possible1, double increase1, int pos1) = routes[src].CanSwap(cust1, cust2,index1);
                    (bool possible2, double increase2, int pos2) = routes[dest].CanSwap(cust2, cust1,index2);

                    if(possible1 && possible2)
                    {
                        double imp = -(increase1 + increase2);
                        if(imp > bestImp)
                        {
                            bestImp = imp;
                            bestDest = dest;
                            bestSrc = src;
                            bestCust1 = cust1;
                            bestCust2 = cust2;
                            bestPos1 = pos1;
                            bestPos2 = pos2;
                        }
                    }
                }
            }
            if (bestDest != -1 && bestCust1 != null && bestCust2 != null)
                return (bestImp, () =>
                {
                    //Remove old customer and insert new customer in its place
                    routes[bestSrc].RemoveCust(bestCust1);
                    routes[bestSrc].InsertCust(bestCust2, bestPos1);


                    //Remove old customer and insert new customer in its place
                    routes[bestDest].RemoveCust(bestCust2);
                    routes[bestDest].InsertCust(bestCust1, bestPos2);


                }
                );
            else
                return (bestImp, null);
                
        }

        private (double,Action?) MoveRandomCustomer(List<Route> routes,List<int> viableRoutes)
        {
            int bestDest = -1, bestSrc = -1, bestPos=-1;
            double bestImp = double.MinValue;
            Customer bestCust = null;


            //viableRoutes = Enumerable.Range(0, routes.Count).Where(i => routes[i].route.Count > 2).ToList();
            var numRoutes = viableRoutes.Count;

            for (int i =0; i< 4; i++)
            {
                //Select destination from routes with customers
                int src = viableRoutes[random.Next(numRoutes)];

                //Select src excluding destination from all routes
                //var range = Enumerable.Range(0, routes.Count).Where(i => i != src).ToList();
                //int dest = range[random.Next(routes.Count - 1)];
                int dest = random.Next(numRoutes - 1);
                if (dest >= src)
                    dest += 1;

                (Customer? cust, double decrease) = routes[src].RandomCust();
                if(cust != null)
                {
                    (var pos, double increase) = routes[dest].BestPossibleInsert(cust);
                    if (pos == -1)
                        continue;
                    double imp = decrease - increase;
                    if(imp > bestImp)
                    {
                        bestImp= imp;
                        bestDest = dest;
                        bestSrc = src;
                        bestCust = cust;
                        bestPos = pos;
                    }

                }
            }
            if (bestDest != -1)
                return (bestImp, () =>
                {
                    routes[bestSrc].RemoveCust(bestCust);
                    routes[bestDest].InsertCust(bestCust, bestPos);
                }
                );
            else
                return (bestImp, null);
        }

        private double[,] CalculateDistanceMatrix(List<Customer> customers)
        {
            double[,] matrix = new double[customers.Count,customers.Count];
            for(int i =0; i< customers.Count; i++) 
                for(int j = i;j<customers.Count;j++)
                    matrix[i,j] = Math.Sqrt(Math.Pow(customers[i].X - customers[j].X,2) + Math.Pow(customers[i].Y - customers[j].Y,2));
            return matrix;
        }

        public Solver()
        {
            random = new Random();
        }

        private void LocalSearchInstance(int id, string name, int numVehicles, double vehicleCapacity, List<Customer> customers,bool printExtendedInfo=false,int numInterations=3000000)
        {
            var distanceMatrix = CalculateDistanceMatrix(customers);
            //customers.Sort(1, customers.Count - 1, delegate (Customer x, Customer y) { x.TWEnd.CompareTo(y.TWEnd); });
            var c = new TWCOmparer();
            customers.Sort(1, customers.Count - 1, c);
            List<Route> routes = new List<Route>();
            

            //Generate routes
            for (int i = 0; i < numVehicles; i++)
                routes.Add(new Route(customers[0],distanceMatrix,vehicleCapacity));

            //Create initial solution
            foreach (Customer cust in customers)
            {
                if (cust.Id == 0)
                    continue;
                double bestIncrease = double.MaxValue;
                int bestPos = -1;
                Route bestRoute = null;
                foreach (Route r in routes)
                {
                    (int pos, double increase) = r.BestPossibleInsert(cust);
                    if(increase < bestIncrease)
                    {
                        bestIncrease = increase;
                        bestPos = pos;
                        bestRoute = r;
                    }
                }
                if (bestPos != -1)
                    bestRoute.InsertCust(cust, bestPos);
                else
                    throw new Exception("Cust past niet!");
            }

            //Validate intial solution
            foreach (Route route in routes)
                if (route.CheckRouteValidity())
                {
                    Console.WriteLine("Initiele oplossing niet valid");
                    throw new Exception();
                }

            int amtImp = 0, amtWorse = 0, amtNotDone = 0;
            double temp = 30;
            double alpha = 0.98;
            double totalP = 0;
            double countP = 0;
            Stopwatch timer = new Stopwatch();
            timer.Start();

            List<int> viableRoutes = Enumerable.Range(0, routes.Count).Where(i => routes[i].route.Count > 2).ToList();

            int lastChangeExceptedOnIt = 0;
            var comp = new ListEqCompare();
            HashSet<RouteStore> Columns = new HashSet<RouteStore>();
            List<Route> BestSolution = routes.ConvertAll(i => i.CreateDeepCopy());
            double bestSolValue = CalcTotalDistance(BestSolution);
            double currentValue = bestSolValue;
            int bestImprovedIteration = 0;
            for (int iteration = 0; iteration < numInterations; iteration++)
            {
                double p = random.NextDouble();
                double imp = 0;
                Action? act = null;
                if (p <= 0.5)
                    (imp, act) = SwapRandomCustomers(routes,viableRoutes);
                else if (p <= 1)
                    (imp, act) = MoveRandomCustomer(routes,viableRoutes);
                if (act != null)
                {
                    //Accept all improvements
                    if (imp > 0)
                    {
                        act();
                        viableRoutes = Enumerable.Range(0, routes.Count).Where(i => routes[i].route.Count > 2).ToList();
                        currentValue -= imp;
                        if (currentValue < bestSolValue)
                        {
                            //New best solution found
                            bestSolValue = currentValue;
                            bestImprovedIteration = iteration;
                            BestSolution = routes.ConvertAll(i => i.CreateDeepCopy());
                        }

                        //Add columns
                        foreach (Route route in routes)
                        {
                            var res = Columns.Add(new RouteStore(route.CreateIdList()));

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
                            act();
                            viableRoutes = Enumerable.Range(0, routes.Count).Where(i => routes[i].route.Count > 2).ToList();
                            currentValue -= imp;
                            //Add columns
                            foreach (Route route in routes)
                            {
                                var res = Columns.Add(new RouteStore(route.CreateIdList()));
                            }
                            lastChangeExceptedOnIt = iteration;
                        }
                    }
                }
                else
                    amtNotDone += 1;
                if (iteration % 10000 == 0 && iteration != 0)
                    temp *= alpha;
                if(iteration - bestImprovedIteration > 300000 && temp < 0.03)
                {
                    //Restart
                    temp = 30;
                    routes = BestSolution.ConvertAll(i => i.CreateDeepCopy());
                    viableRoutes = Enumerable.Range(0, routes.Count).Where(i => routes[i].route.Count > 2).ToList();
                    currentValue = bestSolValue;
                    Console.WriteLine($"{id}:Best solution changed to long ago. Restarting from best solution with T: {temp}");
                }
                if(iteration % 100000 == 0 && iteration != 0)
                {
                    int cnt = routes.Count(x => x.route.Count > 2);
                    Console.WriteLine($"{id}: T: {Math.Round(temp, 3)}, S: {Math.Round(CalcTotalDistance(routes), 3)}, TS: {Math.Round(currentValue, 3)}, N: {cnt}, IT: {iteration}, LA {iteration - lastChangeExceptedOnIt}, B: {Math.Round(bestSolValue, 3)}, BI: {bestImprovedIteration}");
                }
            }
            foreach (Route route in routes)
                route.CheckRouteValidity();
            Console.WriteLine($"DONE {id}: {name}, Score: {CalcTotalDistance(BestSolution)}, Columns: {Columns.Count}, in {Math.Round((double)timer.ElapsedMilliseconds / 1000, 3)}s");
            Console.WriteLine(routes.Sum(x => x.numReference));
            if (printExtendedInfo)
                Console.WriteLine($"  {id}: Total: {amtNotDone + amtImp + amtWorse}, improvements: {amtImp}, worse: {amtWorse}, not done: {amtNotDone}");

        }
    
        public void SolveInstance(string fileName,int numInterations= 3000000)
        {
            (string name, int numV, double capV, List<Customer> customers) = SolomonParser.ParseInstance(fileName);
            LocalSearchInstance(0, name, numV, capV, customers,numInterations: numInterations);
        }

    }

    static class SolomonParser
    {
        public static (string,int,double,List<Customer>) ParseInstance(string fileName)
        {
            using(var reader = new StreamReader(fileName))
            {
                var name = reader.ReadLine().Replace("\n","");

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
                    // ...and y is not null, compare the
                    // lengths of the two strings.
                    //
                    return x.TWEnd.CompareTo(y.TWEnd);//x.Length.CompareTo(y.Length);

                    //if (retval != 0)
                    //{
                    //    // If the strings are not of equal length,
                    //    // the longer string is greater.
                    //    //
                    //    return retval;
                    //}
                    //else
                    //{
                    //    // If the strings are of equal length,
                    //    // sort them with ordinary string comparison.
                    //    //
                    //    return x.CompareTo(y);
                    //}
                }
            }
        }
    }

    class ListEqCompare : IEqualityComparer<List<int>>
    {
        public bool Equals(List<int> x, List<int> y)
        {
            if (x.Count != y.Count)
                return false;
            for (int i = 0; i < x.Count; i++)
            {
                if (x[i] != y[i])
                    return false;
            }
            return true;
        }

        public int GetHashCode(List<int> obj)
        {
            int hash = 0;
            foreach (int num in obj)
                hash = hash ^ EqualityComparer<int>.Default.GetHashCode(num);

            return hash;
        }
    }

}
