using MathNet.Numerics.Distributions;
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
        public static (double improvement, Action? performOperator) ScrambleSubRoute(List<Route> routes, List<int> viableRoutes, Random random)
        {
            //Operator cant be performed if all routes are empty
            if (viableRoutes.Count == 0)
                return (double.MinValue, null);


            int bestRoute = -1;
            List<Customer>? bestScramble = null;
            List<double>? bestArrivalTimes = null;
            List<IContinuousDistribution>? bestDistributions = null;
            double bestImp = double.MinValue;
            bool bestVLTW = false;
            bool bestVUTW = false;

            for (int j = 0; j < 1; j++)
            {
                int routeIndex = viableRoutes[random.Next(viableRoutes.Count)];

                (_, int index1) = routes[routeIndex].RandomCustIndex();
                (_, int index2) = routes[routeIndex].RandomCustIndex();


                if (index1 != index2)
                {
                    //Swap the variables
                    if (index2 < index1)
                    {
                        int temp = index1;
                        index1 = index2;
                        index2 = temp;
                    }

                    List<int> newIndexes = new List<int>();
                    for (int i = index1; i <= index2; i++)
                    {
                        newIndexes.Add(i);
                    }

                    //Might take a lot of time?
                    while (IsOrdered(newIndexes))
                        newIndexes = newIndexes.OrderBy(x => random.Next()).ToList();



                    List<Customer> newRoute = new List<Customer>(routes[routeIndex].route.Capacity);

                    for (int i = 0; i < routes[routeIndex].route.Count; i++)
                    {
                        Customer cust;
                        if (i >= index1 && i <= index2)
                            cust = routes[routeIndex].route[newIndexes[i - index1]];
                        else
                            cust = routes[routeIndex].route[i];

                        newRoute.Add(cust);
                    }

                    (bool possible, double imp, List<double> newArrivalTimes, List<IContinuousDistribution> newDistributions, bool violatesLowerTimeWindow, bool violatesUpperTimeWindow) = routes[routeIndex].NewRoutePossible(newRoute, 0);

                    if (possible && imp > bestImp)
                    {
                        bestImp = imp;
                        bestRoute = routeIndex;
                        bestScramble = newRoute;
                        bestArrivalTimes = newArrivalTimes;
                        bestVLTW = violatesLowerTimeWindow;
                        bestVUTW = violatesUpperTimeWindow;
                        bestDistributions = newDistributions;
                    }



                }
            }
            if (bestArrivalTimes != null && bestScramble != null)
            {
                return (bestImp, () =>
                {
                    routes[bestRoute].SetNewRoute(bestScramble, bestArrivalTimes, bestDistributions, bestVLTW, bestVUTW);
                }
                );
            }

            return (double.MinValue, null);


        }
        //Reverses a random subroute
        public static (double, Action?) ReverseOperator(List<Route> routes, List<int> viableRoutes, Random random)
        {
            if (viableRoutes.Count == 0)
                return (double.MinValue, null);

            int bestRoute = -1;
            int bestIndex1 = -1;
            int bestIndex2 = -1;
            double bestImp = double.MinValue;
            List<double>? bestArrivalTimes = null;
            List<IContinuousDistribution>? bestDistributions = null;
            bool bvltw = false, bvutw = false;

            for (int i = 0; i < 1; i++)
            {
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
                    (bool possible, double imp, List<double> arrivalTimes, List<IContinuousDistribution> newDistributions, bool vltw, bool vutw) = routes[routeIndex].CanReverseSubRoute(index1, index2);

                    if (possible && imp > bestImp)
                    {
                        bestIndex1 = index1;
                        bestIndex2 = index2;
                        bestRoute = routeIndex;
                        bestArrivalTimes = arrivalTimes;
                        bestImp = imp;
                        bvltw = vltw;
                        bvutw = vutw;
                        bestDistributions = newDistributions;
                    }

                }
            }
            if (bestIndex1 != -1 && bestArrivalTimes != null && bestDistributions != null)
            {
                return (bestImp, () =>
                {
                    routes[bestRoute].ReverseSubRoute(bestIndex1, bestIndex2, bestArrivalTimes, bestDistributions, bvltw, bvutw);
                }
                );
            }
            return (double.MinValue, null);

        }

        //Swaps customers within a route
        public static (double, Action?) SwapInsideRoute(List<Route> routes, List<int> viableRoutes, Random random)
        {
            if (viableRoutes.Count == 0)
                return (double.MinValue, null);
            int src = viableRoutes[random.Next(viableRoutes.Count)];

            int bestSrc = -1, bestPos1 = -1, bestPos2 = -1;
            Customer bestCust1 = null, bestCust2 = null;
            double bestImp = double.MinValue;

            for (int i = 0; i < 1; i++)
            {

                (var cust1, int index1) = routes[src].RandomCustIndex();
                (var cust2, int index2) = routes[src].RandomCustIndex();

                if (cust1 != null && cust2 != null && cust1.Id != cust2.Id)
                {
                    (bool possible, double increase1) = routes[src].CanSwapInternally(cust1, cust2, index1, index2);
                    //possible = possible1;
                    double imp = -increase1;

                    if (possible)
                    {
                        //double imp = -(increase1 + increase2);
                        if (imp > bestImp)
                        {
                            bestImp = imp;
                            bestSrc = src;
                            bestCust1 = cust1;
                            bestCust2 = cust2;
                            bestPos1 = index1;
                            bestPos2 = index2;
                        }
                    }

                }
            }

            if (bestCust1 != null && bestCust2 != null)
                return (bestImp, () =>
                {
                    //Remove old customer and insert new customer in its place

                    routes[bestSrc].RemoveCust(bestCust1);


                    //Remove old customer and insert new customer in its place
                    routes[bestSrc].RemoveCust(bestCust2);

                    if (bestPos2 < bestPos1)
                        bestPos1--;


                    routes[bestSrc].InsertCust(bestCust2, bestPos1);
                    routes[bestSrc].InsertCust(bestCust1, bestPos2);


                }
                );
            else
                return (bestImp, null);

        }

        //Swaps random customers
        public static (double, Action?) SwapRandomCustomers(List<Route> routes, List<int> viableRoutes, Random random)
        {
            if (viableRoutes.Count == 0)
                return (double.MinValue, null);

            int bestDest = -1, bestSrc = -1, bestPos1 = -1, bestPos2 = -1;
            Customer bestCust1 = null, bestCust2 = null;
            double bestImp = double.MinValue;

            //viableRoutes = Enumerable.Range(0, routes.Count).Where(i => routes[i].route.Count > 2).ToList();
            var numRoutes = viableRoutes.Count;
            for (int i = 0; i < 1; i++)
            {
                //Select destination
                int dest_index = random.Next(numRoutes);
                int dest = viableRoutes[dest_index];


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

        public static (double, Action?) RemoveRandomCustomer(List<Route> routes, List<int> viableRoutes, Random random, List<Customer> removed, LocalSearch ls)
        {
            if (viableRoutes.Count == 0)
                return (double.MinValue, null);

            var routeIndex = viableRoutes[random.Next(viableRoutes.Count)];
            (Customer cust, double decr, int i) = routes[routeIndex].RandomCust();


            double penalty = 0;
            penalty = Solver.CalcRemovedPenalty(removed.Count + 1, ls) - Solver.CalcRemovedPenalty(removed.Count, ls); //diff * Solver.BaseRemovedCustomerPenalty / temp;
            double imp = decr - penalty;

            return (imp, () =>
            {
                routes[routeIndex].RemoveCust(cust);
                removed.Add(cust);
            }
            );

        }

        public static (double, Action?) AddRandomRemovedCustomer(List<Route> routes, List<int> viableRoutes, Random random, List<Customer> removed, LocalSearch ls, bool allowNewRouteCreation = true)
        {
            if (removed.Count == 0)
                return (double.MinValue, null);

            Customer cust = removed[random.Next(removed.Count)];

            int extra = 0;
            if (allowNewRouteCreation)
                extra = 1;

            int routeIndex = random.Next(viableRoutes.Count + extra);
            Route route;
            if (routeIndex >= viableRoutes.Count)
                if (viableRoutes.Count != routes.Count)
                    route = routes.FirstOrDefault(x => x.route.Count == 2);
                else
                    route = routes[viableRoutes.Count - 1];
            else
                route = routes[viableRoutes[routeIndex]];
            int pos = random.Next(1, route.route.Count);

            (bool possible, _, double incr) = route.CustPossibleAtPos(cust, pos);

            double penalty = 0;

            penalty = Solver.CalcRemovedPenalty(removed.Count, ls) - Solver.CalcRemovedPenalty(removed.Count - 1, ls); // diff * Solver.BaseRemovedCustomerPenalty / temp;
            if (possible)
                return (penalty - incr, () =>
                {
                    removed.Remove(cust);
                    route.InsertCust(cust, pos);

                }
                );
            return (double.MinValue, null);


        }

        //Function used to repeat opertors multiple times and choose the best one
        public static (double, Action?) RepeatNTimes(int n, Operator op, List<Route> routes, List<int> viableRoutes, Random random, List<Customer> removed, LocalSearch ls)
        {
            double bestVale = double.MinValue;
            Action? bestAction = null;

            for (int i = 0; i < n; i++)
            {
                (double val, Action? act) = op(routes, viableRoutes, random, removed, ls);

                if (val > bestVale)
                {
                    bestVale = val;
                    bestAction = act;
                }

            }
            return (bestVale, bestAction);
        }

        public static (double, Action?) GreedilyMoveRandomCustomer(List<Route> routes, List<int> viableRoutes, Random random)
        {
            if (viableRoutes.Count == 0)
                return (double.MinValue, null);
            var routeIndex = viableRoutes[random.Next(viableRoutes.Count)];
            (Customer cust, double decr, _) = routes[routeIndex].RandomCust();

            Route bestRoute = null;
            double bestImp = double.MinValue;
            int bestPos = -1;
            double bestDecr = double.MinValue, bestIncr = double.MaxValue;

            for (int i = 0; i < routes.Count; i++)
            {
                //Do not swap inside the current route
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

        public static (double, Action?) MoveRandomCustomerToRandomCustomer(List<Route> routes, List<int> viableRoutes, Random random, bool allowNewRouteCreation = true)
        {
            if (viableRoutes.Count <= 1)
                return (double.MinValue, null);
            int src_index = random.Next(viableRoutes.Count);
            int src = viableRoutes[src_index];

            //Used to allow for moving to an empty route
            int extra = 0;
            if (allowNewRouteCreation && viableRoutes.Count < routes.Count)
                extra = 1;

            int destIndex = random.Next(viableRoutes.Count + extra);


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

            (cust1, decr1, int i) = routes[src].RandomCust();


            if (cust1 != null && cust1 != cust2 && pos != i + 1)
            {
                bool possible; double objectiveIncr;
                if (src == dest)
                    (possible, _, objectiveIncr) = routes[dest].CustPossibleAtPos(cust1, pos, ignore: i);
                else
                    (possible, _, objectiveIncr) = routes[dest].CustPossibleAtPos(cust1, pos);


                double imp;
                if (src == dest)
                    //Taking into account that the moved customer is counted already in the score for possible at pos
                    imp = -objectiveIncr;
                else imp = decr1 - objectiveIncr;

                if (possible)
                    return (imp, () =>
                    {
                        double bestdcr = decr1;
                        double increase = objectiveIncr;

                        if (src == dest && i < pos)
                            pos--;

                        routes[src].RemoveCust(cust1);
                        routes[dest].InsertCust(cust1, pos);
                    }
                    );
            }

            return (double.MinValue, null);

        }

        public static (double, Action?) SwapRandomTails(List<Route> routes, List<int> viableRoutes, Random random)
        {
            //We need two viable routes
            if (viableRoutes.Count <= 1)
                return (double.MinValue, null);

            int srcIndex = random.Next(viableRoutes.Count);
            int src = viableRoutes[srcIndex];

            int destIndex = random.Next(viableRoutes.Count - 1);
            if (destIndex >= srcIndex)
                destIndex += 1;
            int dest = viableRoutes[destIndex];

            (_, int index1) = routes[src].RandomCustIndex();
            (_, int index2) = routes[dest].RandomCustIndex();

            List<Customer> newSrcRoute = new List<Customer>();
            List<Customer> newDestRouteTail = new List<Customer>();
            List<Customer> newDestRoute = new List<Customer>();
            for (int i = 0; i < routes[src].route.Count; i++)
            {
                if (i < index1)
                    newSrcRoute.Add(routes[src].route[i]);

                else
                {
                    newDestRouteTail.Add(routes[src].route[i]);
                }

            }
            for (int i = 0; i < routes[dest].route.Count; i++)
            {
                if (i < index2)
                {
                    newDestRoute.Add(routes[dest].route[i]);
                }
                else
                {
                    newSrcRoute.Add(routes[dest].route[i]);
                }
            }

            newDestRoute.AddRange(newDestRouteTail);

            (bool possible, double improvement, var newArrivalTimes, List<IContinuousDistribution> newDistributions1, bool violatesLowerTimeWindow1, bool violatesUpperTimeWindow1) = routes[src].NewRoutePossible(newSrcRoute, newSrcRoute.Sum(x => x.Demand) - routes[src].used_capacity);
            (bool possible2, double improvement2, var newArrivalTimes2, List<IContinuousDistribution> newDistributions2, bool violatesLowerTimeWindow2, bool violatesUpperTimeWindow2) = routes[dest].NewRoutePossible(newDestRoute, newDestRoute.Sum(x => x.Demand) - routes[dest].used_capacity);

            if (possible2 && possible)
            {
                return (improvement + improvement2, () =>
                {
                    routes[src].SetNewRoute(newSrcRoute, newArrivalTimes, newDistributions1, violatesLowerTimeWindow1, violatesUpperTimeWindow1);
                    routes[dest].SetNewRoute(newDestRoute, newArrivalTimes2, newDistributions2, violatesLowerTimeWindow2, violatesUpperTimeWindow2);
                }
                );
            }

            return (double.MinValue, null);

        }
        public static (double, Action?) MoveRandomCustomerToRandomRoute(List<Route> routes, List<int> viableRoutes, Random random)
        {

            if (viableRoutes.Count == 0)
                return (double.MinValue, null);

            int bestDest = -1, bestSrc = -1, bestPos = -1;
            double bestImp = double.MinValue;
            Customer bestCust = null;
            double bestDecr = double.MinValue, bestIncr = double.MaxValue;

            var numRoutes = viableRoutes.Count;

            for (int i = 0; i < 1; i++)
            {
                //Select destination from routes with customers
                int src = viableRoutes[random.Next(numRoutes)];

                //Select src excluding destination from all routes
                int dest = random.Next(numRoutes - 1);
                if (dest >= src)
                    dest += 1;

                (Customer? cust, double decrease, _) = routes[src].RandomCust();
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
