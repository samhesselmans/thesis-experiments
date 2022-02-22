using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SA_ILP
{
    static class Operators
    {

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

                (bool possible, double imp, List<double> arrivalTimes) = routes[routeIndex].CanReverseSubRoute(index1, index2);

                if (possible)
                    return (imp, () => {
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
            (Customer cust, double decr) = routes[routeIndex].RandomCust();


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
            (Customer cust, double decr) = routes[routeIndex].RandomCust();

            Route bestRoute = null;
            double bestImp = double.MinValue;
            int bestPos = -1;

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
                    bestImp = decr - increase;
                    bestPos = pos;
                    bestRoute = routes[i];
                }

            }
            if (bestRoute != null)
                return (bestImp, () =>
                {
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

            (cust1,  decr1) = routes[src].RandomCust();


            if (cust1 != null && cust1 != cust2)
            {
                (bool possible, _, double objectiveIncr) = routes[dest].CustPossibleAtPos(cust1, pos);

                if (possible)
                    return (decr1 - objectiveIncr, () => {
                        routes[src].RemoveCust(cust1);
                        routes[dest].InsertCust(cust1, pos);
                    }
                    );
            }

            return (double.MinValue, null);

        }

        public static (double, Action?) MoveRandomCustomer(List<Route> routes, List<int> viableRoutes,Random random)
        {
            int bestDest = -1, bestSrc = -1, bestPos = -1;
            double bestImp = double.MinValue;
            Customer bestCust = null;


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

                (Customer? cust, double decrease) = routes[src].RandomCust();
                if (cust != null)
                {
                    (var pos, double increase) = routes[dest].BestPossibleInsert(cust);
                    if (pos == -1)
                        continue;
                    double imp = decrease - increase;
                    if (imp > bestImp)
                    {
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
                    routes[bestSrc].RemoveCust(bestCust);
                    routes[bestDest].InsertCust(bestCust, bestPos);
                }
                );
            else
                return (bestImp, null);
        }

    }
}
