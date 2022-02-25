using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SA_ILP
{
    static class Operators
    {

        private static bool IsOrdered(List<int> list)
        {
            int prev = -1;
            foreach (int i in list)
                if (prev > i)
                    return false;
                else
                    prev = i;
            return true;
        }
        public static (double improvement,Action? performOperator) ScrambleSubRoute(List<Route> routes, List<int> viableRoutes,Random random)
        {
            //Operator cant be performed if all routes are empty
            if (viableRoutes.Count == 0)
                return (double.MinValue, null);

            int routeIndex = viableRoutes[random.Next(viableRoutes.Count)];

            (_, int index1) = routes[routeIndex].RandomCustIndex();
            (_, int index2) = routes[routeIndex].RandomCustIndex();


            if(index1 != index2)
            {
                //Swap the variables
                if(index2 < index1)
                {
                    int temp = index1;
                    index1 = index2;
                    index2 = temp;
                }

                List<int> newIndexes = new List<int>();
                for(int i = index1;i <= index2; i++)
                {
                    newIndexes.Add(i);
                }

                //Might take a lot of time?
                while(IsOrdered(newIndexes))
                    newIndexes = newIndexes.OrderBy(x=>random.Next()).ToList();
                


                List<Customer> newRoute = new List<Customer>(routes[routeIndex].route.Count);

                for(int i =0; i< routes[routeIndex].route.Count; i++)
                {
                    Customer cust;
                    if(i >= index1 && i <= index2)
                        cust = routes[routeIndex].route[newIndexes[i-index1]];
                    else
                        cust = routes[routeIndex].route[i];

                    newRoute.Add(cust);
                }

                (bool possible, double imp, List<double> newArrivalTimes) = routes[routeIndex].NewRoutePossible(newRoute, 0);

                if (possible)
                    return (imp, () =>
                    {
                        routes[routeIndex].SetNewRoute(newRoute, newArrivalTimes);
                    }
                    );


            }
            return (double.MinValue, null);
            //throw new NotImplementedException();


        }
        public static (double, Action?) ReverseOperator(List<Route> routes, List<int> viableRoutes, Random random)
        {
            if (viableRoutes.Count == 0)
                return (double.MinValue, null);
            int routeIndex = viableRoutes[random.Next(viableRoutes.Count)];

            (_, int index1) = routes[routeIndex].RandomCustIndex();
            (_, int index2) = routes[routeIndex].RandomCustIndex();


            if (index1 != index2)
            {
                //Swap the indexes
                if (index2 < index1)
                {
                    int temp = index2;
                    index2 = index1;
                    index1 = temp;
                }

                //List<Customer> newRoute = new List<Customer>(routes[routeIndex].route.Count);

                //for(int i =0; i< routes[routeIndex].route.Count; i++)
                //{
                //    Customer currentCust;
                //    if (i >= index1 && i <= index2)
                //    {
                //        //In the to be reversed subroute, select in reversed order
                //        currentCust = routes[routeIndex].route[index2 - i + index1];


                //    }
                //    else
                //    {
                //        currentCust = routes[routeIndex].route[i];
                //    }
                //    newRoute.Add(currentCust);
                //}
                //(bool posssible, double imp, List<double> arrivalTimes) = routes[routeIndex].NewRoutePossible(newRoute, 0);
                //if (posssible)
                //    return (imp, () => { routes[routeIndex].SetNewRoute(newRoute, arrivalTimes); }
                //    );
                (bool possible, double imp, List<double> arrivalTimes) = routes[routeIndex].CanReverseSubRoute(index1, index2);

                if (possible)
                    return (imp, () =>
                    {
                        routes[routeIndex].ReverseSubRoute(index1, index2, arrivalTimes);
                    }
                    );


            }
            //throw new Exception();
            return (double.MinValue, null);

        }

       public  static (double, Action?) SwapRandomCustomers(List<Route> routes, List<int> viableRoutes,Random random)
        {
            if (viableRoutes.Count == 0)
                return (double.MinValue, null);

            int bestDest = -1, bestSrc = -1, bestPos1 = -1, bestPos2 = -1;
            Customer bestCust1 = null, bestCust2 = null;
            double bestImp = double.MinValue;

            //viableRoutes = Enumerable.Range(0, routes.Count).Where(i => routes[i].route.Count > 2).ToList();
            var numRoutes = viableRoutes.Count;
            for (int i = 0; i < 4; i++)
            {
                //Select destination
                int dest_index = random.Next(numRoutes);
                int dest = viableRoutes[dest_index];

                //Select src excluding destination
                //var range = Enumerable.Range(0, numRoutes).Where(i => i != dest).ToList();

                //int src = random.Next(numRoutes - 1);//range[random.Next(numRoutes - 1)];
                //if (src >= dest_index)
                //    src += 1;
                //src = viableRoutes[src];


                int src = viableRoutes[random.Next(numRoutes)];


                (var cust1, int index1) = routes[src].RandomCustIndex();
                (var cust2, int index2) = routes[dest].RandomCustIndex();


                if (cust1 != null && cust2 != null && cust1.Id != cust2.Id)
                {
                    bool possible;
                    double imp;
                    if (src != dest)
                    {
                        (bool possible1, double increase1, int pos1) = routes[src].CanSwapBetweenRoutes(cust1, cust2, index1);
                        (bool possible2, double increase2, int pos2) = routes[dest].CanSwapBetweenRoutes(cust2, cust1, index2);
                        possible = possible1 && possible2;
                        imp = -(increase1 + increase2);

                    }
                    else
                    {
                        (bool possible1, double increase1) = routes[src].CanSwapInternally(cust1, cust2, index1, index2);
                        possible = possible1;
                        imp = -increase1;
                    }



                    if (possible)
                    {
                        //double imp = -(increase1 + increase2);
                        if (imp > bestImp)
                        {
                            bestImp = imp;
                            bestDest = dest;
                            bestSrc = src;
                            bestCust1 = cust1;
                            bestCust2 = cust2;
                            bestPos1 = index1;
                            bestPos2 = index2;
                        }
                    }
                }
            }
            if (bestDest != -1 && bestCust1 != null && bestCust2 != null)
                return (bestImp, () =>
                {
                    //Remove old customer and insert new customer in its place

                    routes[bestSrc].RemoveCust(bestCust1);


                    //Remove old customer and insert new customer in its place
                    routes[bestDest].RemoveCust(bestCust2);

                    if (bestSrc == bestDest)
                        if (bestPos2 < bestPos1)
                            bestPos1--;


                    routes[bestSrc].InsertCust(bestCust2, bestPos1);
                    routes[bestDest].InsertCust(bestCust1, bestPos2);


                }
                );
            else
                return (bestImp, null);

        }

        public static (double, Action?) RemoveRandomCustomer(List<Route> routes, List<int> viableRoutes,Random random, List<Customer> removed,double temp)
        {
            if (viableRoutes.Count == 0)
                return (double.MinValue, null);

            var routeIndex = viableRoutes[random.Next(viableRoutes.Count)];
            (Customer cust, double decr,int i) = routes[routeIndex].RandomCust();


            double penalty = 0;
            double diff = Math.Pow(removed.Count + 1, Solver.BaseRemovedCustomerPenaltyPow) - Math.Pow(removed.Count, Solver.BaseRemovedCustomerPenaltyPow);
            //TODO: calculate penalty
            penalty = diff * Solver.BaseRemovedCustomerPenalty / temp;
            double imp = decr - penalty; 

            return (imp, () => {
                routes[routeIndex].RemoveCust(cust);
                removed.Add(cust);           
            }
            );

        }

        public static (double,Action?) AddRandomRemovedCustomer(List<Route> routes,List<int> viableRoutes,Random random, List<Customer> removed,double temp)
        {
            if (removed.Count == 0)
                return (double.MinValue, null);

            Customer cust = removed[random.Next(removed.Count)];

            //Probleem dat er heel veel lege routes zijn?
            int routeIndex = random.Next(viableRoutes.Count + 1);
            Route route;
            if (routeIndex == viableRoutes.Count)
                if (viableRoutes.Count != routes.Count)
                    route = routes.FirstOrDefault(x => x.route.Count == 2);
                else
                    route = routes[routeIndex - 1];
            else
                route = routes[viableRoutes[routeIndex]];
                int pos = random.Next(1, route.route.Count);

            //Do we want to try several positions?
            (bool possible, _, double incr) = route.CustPossibleAtPos(cust, pos);

            double penalty = 0;
            double diff = Math.Pow(removed.Count, Solver.BaseRemovedCustomerPenaltyPow) - Math.Pow(removed.Count -1, Solver.BaseRemovedCustomerPenaltyPow); 
            //TODO: calculate penalty
            penalty = diff* Solver.BaseRemovedCustomerPenalty / temp;
            if (possible)
                return (penalty - incr, () =>
                {
                    removed.Remove(cust);
                    route.InsertCust(cust, pos);

                }
                );
            return (double.MinValue, null);


        }

        public static (double, Action?) GreedilyMoveRandomCustomer(List<Route> routes, List<int> viableRoutes,Random random)
        {
            if (viableRoutes.Count == 0)
                return (double.MinValue, null);
            var routeIndex = viableRoutes[random.Next(viableRoutes.Count)];
            (Customer cust, double decr,_) = routes[routeIndex].RandomCust();

            Route bestRoute = null;
            double bestImp = double.MinValue;
            int bestPos = -1;
            double bestDecr = double.MinValue, bestIncr = double.MaxValue;

            for (int i = 0; i < routes.Count; i++)
            {
                //Temporary. Might be nice to swap greedily within a route
                if (i == routeIndex)
                    continue;
                (var pos, double increase) = routes[i].BestPossibleInsert(cust);
                if (pos == -1)
                    continue;

                if (decr - increase > bestImp)
                {
                    bestDecr = decr;
                    bestIncr = increase;
                    bestImp = decr - increase;
                    bestPos = pos;
                    bestRoute = routes[i];
                }

            }
            if (bestRoute != null)
                return (bestImp, () =>
                {
                    double decr = bestDecr;
                    double incr = bestIncr;
                    routes[routeIndex].RemoveCust(cust);
                    bestRoute.InsertCust(cust, bestPos);
                }
                );
            else
                return (bestImp, null);
        }

        public static (double, Action?) MoveRandomCustomerToRandomCustomer(List<Route> routes, List<int> viableRoutes, Random random)
        {
            if (viableRoutes.Count <= 1)
                return (double.MinValue, null);
            int src_index = random.Next(viableRoutes.Count);
            int src = viableRoutes[src_index];

            //Used to allow for moving to a not used route
            int extra = 0;
            if (viableRoutes.Count < routes.Count)
                extra = 1;
            
            int destIndex = random.Next(viableRoutes.Count + extra );
            //if (destIndex >= src_index)
            //    destIndex++;

            int dest;
            Customer? cust1; double decr1;
            Customer? cust2; int pos;
            if (destIndex < viableRoutes.Count)
            {
                dest = viableRoutes[destIndex];
                (cust2, pos) = routes[dest].RandomCustIndex();

            }
            else
            {
                //Select an empty route
                dest = routes.FindIndex(x => x.route.Count == 2);
                cust2 = null;
                pos = 1;
            }

            (cust1,  decr1,int i) = routes[src].RandomCust();

            //if (src == dest && i + 1 == pos)
            //    Console.WriteLine("Wut");

            if (cust1 != null && cust1 != cust2 && pos != i+1)
            {
                bool possible; double objectiveIncr;
                if(src == dest)
                    (possible, _, objectiveIncr) = routes[dest].CustPossibleAtPos(cust1, pos,ignore:i);
                else
                    (possible, _, objectiveIncr) = routes[dest].CustPossibleAtPos(cust1, pos);


                double imp;
                if (src == dest)
                    //Taking into account that the moved customer is counted already in the score for possible at pos
                    imp = -objectiveIncr;
                else imp = decr1 - objectiveIncr;

                if (possible)
                    return (imp, () => {

                        if (src == dest && i < pos)
                            pos--;
                        
                        routes[src].RemoveCust(cust1);
                        routes[dest].InsertCust(cust1, pos);
                    }
                    );
            }

            return (double.MinValue, null);

        }

        public static (double, Action?) MoveRandomCustomerToRandomRoute(List<Route> routes, List<int> viableRoutes,Random random)
        {

            if(viableRoutes.Count == 0)
                return(double.MinValue, null);

            int bestDest = -1, bestSrc = -1, bestPos = -1;
            double bestImp = double.MinValue;
            Customer bestCust = null;
            double bestDecr = double.MinValue, bestIncr = double.MaxValue;

            //viableRoutes = Enumerable.Range(0, routes.Count).Where(i => routes[i].route.Count > 2).ToList();
            var numRoutes = viableRoutes.Count;

            for (int i = 0; i < 4; i++)
            {
                //Select destination from routes with customers
                int src = viableRoutes[random.Next(numRoutes)];

                //Select src excluding destination from all routes
                //var range = Enumerable.Range(0, routes.Count).Where(i => i != src).ToList();
                //int dest = range[random.Next(routes.Count - 1)];
                int dest = random.Next(numRoutes - 1);
                if (dest >= src)
                    dest += 1;

                (Customer? cust, double decrease,_) = routes[src].RandomCust();
                if (cust != null)
                {
                    (var pos, double increase) = routes[dest].BestPossibleInsert(cust);
                    if (pos == -1)
                        continue;
                    double imp = decrease - increase;
                    if (imp > bestImp)
                    {
                        bestDecr = decrease;
                        bestIncr = increase;

                        bestImp = imp;
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

                    double decr = bestDecr;
                    double incr = bestIncr;

                    routes[bestSrc].RemoveCust(bestCust);
                    routes[bestDest].InsertCust(bestCust, bestPos);
                }
                );
            else
                return (bestImp, null);
        }

    }
}
