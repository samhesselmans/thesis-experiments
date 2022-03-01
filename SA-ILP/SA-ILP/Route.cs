﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SA_ILP
{
    internal class Route
    {
        public List<Customer> route;
        public List<double> arrival_times;
        public double[,,] objective_matrix;
        public double[] objeciveMatrix1d;

        double startTime = 0;
        
        public int numLoadLevels;
        public int numX;
        public int numY;

        public double Score { get
            {
                if (CachedObjective != -1)
                    return CachedObjective;
                else
                    return CalcObjective();

            } }


        public bool ViolatesLowerTimeWindow { get;private set; }
        public bool ViolatesUpperTimeWindow { get; private set; }

        LocalSearch parent;

        public double time_done;
        //public Customer lastCust;
        public double used_capacity;
        public double max_capacity;
        private double CachedObjective;
        private Dictionary<int,(int,double)> BestCustomerPos;
        private Dictionary<(int, int), (bool,bool, double)> CustPossibleAtPosCache;

        private Random random;

#if DEBUG
        public long numReference = 0;
        public long bestFitCacheHit = 0;
        public long bestFitCacheMiss = 0;
#endif
        public Route(Customer depot, double[,,] distanceMatrix, double maxCapacity, int seed, LocalSearch parent)
        {
            this.route = new List<Customer>() { depot, depot };
            this.arrival_times = new List<double>() { 0, 0 };
            this.objective_matrix = distanceMatrix;
            this.numX = distanceMatrix.GetLength(0);
            this.numY = distanceMatrix.GetLength(1);
            this.numLoadLevels = distanceMatrix.GetLength(2);
            //this.objeciveMatrix1d = new double[numX * numY * numLoadLevels];
            //Create1DMAtrix();
            this.parent = parent;

            this.time_done = 0;
            //this.lastCust = depot;
            used_capacity = 0;
            this.max_capacity = maxCapacity;
            random = new Random(seed);
            //BestCustomerPos = new Dictionary<int,(int,double)>();
            ResetCache();
        }


        public override string ToString()
        {
            return $"({String.Join(',', CreateIdList())})";
        }

        //private void Create1DMAtrix()
        //{
        //    for(int i=0; i< numX;i++)
        //        for(int j =0;j< numY;j++)
        //            for(int k = 0; k < numLoadLevels; k++)
        //            {
        //                objeciveMatrix1d[i + numX * j + (numY * numX) * k] = objective_matrix[i,j,k];
        //            }
        //}

        public  Route(List<Customer> route, List<double> arrivalTimes, double[,,] distanceMatrix, double usedCapcity, double maxCapacity,int seed,LocalSearch parent,double startTime)
        {
            this.route = route;
            this.arrival_times = arrivalTimes;
            this.objective_matrix = distanceMatrix;
            this.numX = distanceMatrix.GetLength(0);
            this.numY = distanceMatrix.GetLength(1);
            this.numLoadLevels = distanceMatrix.GetLength(2);
            //this.objeciveMatrix1d = new double[numX * numY * numLoadLevels];
            //Create1DMAtrix();
            this.parent = parent;
            this.time_done = 0;
            used_capacity = usedCapcity;
            this.max_capacity = maxCapacity;
            this.startTime = startTime;
            random = new Random(seed);
            //BestCustomerPos = new Dictionary<int, (int, double)>();
            ResetCache();
        }

        public Route(List<Customer> customers,RouteStore routeStore, Customer depot, double[,,] distanceMatrix, double maxCapacity,LocalSearch parent)
        {
            this.route = new List<Customer>() { depot, depot };
            this.arrival_times = new List<double>() { 0, 0 };
            this.objective_matrix = distanceMatrix;
            this.numX = distanceMatrix.GetLength(0);
            this.numY = distanceMatrix.GetLength(1);
            this.numLoadLevels = distanceMatrix.GetLength(2);
            //this.objeciveMatrix1d = new double[numX * numY * numLoadLevels];
            //Create1DMAtrix();
            this.parent = parent;

            this.time_done = 0;
            //this.lastCust = depot;
            used_capacity = 0;
            this.max_capacity = maxCapacity;
            random = new Random();

            ResetCache();

            foreach(int cust in routeStore.Route)
            {
                //DO Not insert the depot's
                if(cust != 0)
                    this.InsertCust(customers.First(x=>x.Id == cust),route.Count -1);
            }

        }

        public double CalculateEarlyPenaltyTerm(double arrivalTime,double timewindowStart)
        {
            if(!parent.PenalizeEarlyArrival)
                return 0;// 100 + timewindowStart - arrivalTime;// timewindowStart - arrivalTime;
            else
            {

                //Add possibility to remove ramping based on penalty

                return (parent.BaseEarlyArrivalPenalty + timewindowStart - arrivalTime);// / (parent.Temperature / parent.InitialTemperature);

            }

        }

        public double CalculateLatePenaltyTerm(double arrivalTime, double timeWindowEnd)
        {
            if (!parent.PenalizeLateArrival)
                return 0;
            else 
                return (parent.BaseLateArrivalPenalty + arrivalTime - timeWindowEnd)/(parent.Temperature/parent.InitialTemperature);// + arrivalTime - timeWindowEnd;
        }

        public double CalcObjective()
        {
            //var total_dist = 0.0;
            //double total_time_waited = 0;
            //var totalWeight = used_capacity;
            //for (int i = 0; i < route.Count - 1; i++)
            //{
            //    total_dist += this.CustomerDist(route[i], route[i + 1], totalWeight);
            //    var actualArrivalTime = arrival_times[i] + CustomerDist(route[i], route[i + 1], totalWeight) + route[i].ServiceTime;
            //    //Adding penalty for violating timewindow start
            //    if (actualArrivalTime  < route[i + 1].TWStart)
            //        total_dist += CalculateEarlyPenaltyTerm(actualArrivalTime, route[i + 1].TWStart);
            //    if (actualArrivalTime > route[i + 1].TWEnd)
            //        total_dist += CalculateLatePenaltyTerm(actualArrivalTime, route[i + 1].TWEnd);

            //    totalWeight -= route[i + 1].Demand;
            //}
            //CachedObjective = total_dist;
            //return total_dist;

            double totalDist = 0;
            double totalWeight = used_capacity;// route.Sum(x => x.Demand);
            double arrivalTime = 0;
            for(int i =0; i< route.Count - 1; i++)
            {
                double dist = this.CustomerDist(route[i], route[i + 1], totalWeight);
                totalDist += dist;
                arrivalTime += dist + route[i].ServiceTime;

                if(arrivalTime < route[i + 1].TWStart)
                {
                    if(i != 0)
                    {
                        totalDist += CalculateEarlyPenaltyTerm(arrivalTime, route[i + 1].TWStart);
                        if (parent.AdjustEarlyArrivalToTWStart)
                            arrivalTime = route[i + 1].TWStart;
                    }
                    else
                    {
                        arrivalTime = route[i + 1].TWStart;
                    }
                }
                else if(arrivalTime > route[i + 1].TWEnd)
                {
                    totalDist += CalculateLatePenaltyTerm(arrivalTime,route[i + 1].TWEnd);
                }

                totalWeight -= route[i + 1].Demand;
            }
            CachedObjective = totalDist;
            return totalDist;
        }

        public double CustomerDist(Customer start, Customer finish, double weight)
        {
#if DEBUG
            numReference += 1;
#endif
            int loadLevel = (int)((weight / max_capacity) * numLoadLevels);

            //This happens if the vehicle is fully loaded. It wants to check the next loadlevel
            if (loadLevel == numLoadLevels)
                loadLevel--;

            var val = objective_matrix[start.Id, finish.Id, loadLevel];
            //var val2 = objeciveMatrix1d[cust1.Id + cust2.Id * numX + loadLevel * numX * numY];
            //if (val != val2)
            //    Console.WriteLine("wops");
            return val;
        }

        public void ResetCache()
        {
            BestCustomerPos = new Dictionary<int, (int,double)>();
            CustPossibleAtPosCache = new Dictionary<(int, int), (bool, bool, double)>();
            CachedObjective = -1;
        }

        public void RemoveCust(Customer cust)
        {
            double newArriveTime = 0;
            ViolatesUpperTimeWindow = false;
            ViolatesLowerTimeWindow = false;

            startTime = 0;
            arrival_times[0] = 0;
            int index = -1;
            Customer lastCust;
            Customer previous_cust = route[0];
            this.used_capacity -= cust.Demand;
            double load = used_capacity;
            for (int i = 1, actualIndex=1; i < route.Count; i++,actualIndex++)
            {
                var c = route[i];
                if (c.Id != cust.Id)
                {
                    var dist = CustomerDist(previous_cust, c, load);
                    load -= c.Demand;
                    newArriveTime += dist;
                    if (newArriveTime < c.TWStart)
                    {
                        if (actualIndex != 1)
                        {
                            ViolatesLowerTimeWindow = true;
                            if (parent.AdjustEarlyArrivalToTWStart)
                                newArriveTime = c.TWStart;
                        }
                        else
                        {
                            startTime = c.TWStart - newArriveTime;
                            arrival_times[0] = startTime;
                            newArriveTime = c.TWStart;
                        }
                    }



                    if (newArriveTime > c.TWEnd)
                        ViolatesUpperTimeWindow = true;

                    arrival_times[i] = newArriveTime;
                    newArriveTime += c.ServiceTime;
                    lastCust = c;
                    previous_cust = c;
                }
                else
                {
                    actualIndex--;
                    index = i;
                }
            }
            if (index == -1)
                Console.WriteLine("Helpt");
            route.RemoveAt(index);
            arrival_times.RemoveAt(index);
            //this.lastCust = lastCust;
            ResetCache();

        }

        public (bool possible, double decrease) CanSwapInternally(Customer cust1, Customer cust2, int index1, int index2)
        {
            double load = used_capacity;
            double arrival_time = 0;
            double totalTravelTime = 0;
            for(int i=0; i<route.Count - 1; i++)
            {
                Customer currentCust;
                Customer nextCust;
                if (i == index1 - 1)
                    nextCust = cust2;
                else if (i == index2 - 1)
                    nextCust = cust1;
                else
                    nextCust = route[i+1];

                if(i == index1)
                    currentCust = cust2;
                else if(i == index2)
                    currentCust = cust1;
                else 
                    currentCust = route[i];

                //if (arrival_time > currentCust.TWEnd)
                //    if (allowLateTimewindow)
                //        totalTravelTime += CalculateLatePenaltyTerm(arrival_time, currentCust.TWEnd);
                //    else
                //        return (false, double.MinValue);

                if(arrival_time < currentCust.TWStart)
                {
                    if (i != 1)
                    {
                        totalTravelTime += CalculateEarlyPenaltyTerm(arrival_time, currentCust.TWStart);

                        if (parent.AdjustEarlyArrivalToTWStart)
                            arrival_time = currentCust.TWStart;
                    }
                    else
                    {
                        arrival_time = currentCust.TWStart;
                    }
                }
                var dist = CustomerDist(currentCust, nextCust, load);
                load -= nextCust.Demand;
                totalTravelTime += dist;
                arrival_time += dist + currentCust.ServiceTime;

                if (arrival_time > nextCust.TWEnd)
                    if(parent.AllowLateArrivalDuringSearch)
                        totalTravelTime += CalculateLatePenaltyTerm(arrival_time, nextCust.TWEnd);
                    else
                        return (false, double.MinValue);

            }

            //double objective = CachedObjective;
            //if(objective == -1)
            //    objective = CalcObjective();

            return (true, totalTravelTime - this.Score);
        }

        public (bool possible, bool possibleInLaterPosition, double objectiveIncrease) CustPossibleAtPos(Customer cust, int pos, int skip = 0, int ignore = -1)
        {
            //Possible optimization: Cache minimal weight required for a load level change in the route. If the new load is less than that we only have to check from pos

            //if (CustPossibleAtPosCache.ContainsKey((cust.Id, pos)))
            //    return CustPossibleAtPosCache[(cust.Id, pos)];

            //if (skip > 0 && ignore != -1)
            //    throw new Exception("Cant use ignore and skip together");

            double totalTravelTime = 0;
            double load = used_capacity + cust.Demand;

            //Remove the demand of the removed customers from the inital load
            for (int i = 0; i < skip; i++)
            {
                load -= route[pos + i].Demand;
            }

            if(ignore != -1)
                load -= route[ignore].Demand;

            ////Handle the ignore if it is right after the skipped
            //if (pos + skip == ignore)
            //    skip += 1;

            //Need to check capacity, otherwise loadlevel claculation fails
            if (load > max_capacity)
            {
                //CustPossibleAtPosCache[(cust.Id, pos)] = (false, false, double.MinValue);
                return (false, false, double.MinValue);
            }

            //int actualIndex = 0;

            double arrivalTime = 0;
            for (int i = 0,actualIndex=0; i < route.Count; i++,actualIndex ++)
            {

                //Arrived at the insert position. Include the new Customer into the check
                if (i == pos)
                {
                    if (arrivalTime > cust.TWEnd)
                    {
                        //CustPossibleAtPosCache[(cust.Id, pos)] = (false, false, double.MinValue);
                        if (parent.AllowLateArrivalDuringSearch)
                            totalTravelTime += CalculateLatePenaltyTerm(arrivalTime, cust.TWEnd);
                        else
                            return (false, false, double.MinValue);


                    }
                    
                    //Wait for the timewindow start
                    else if (arrivalTime < cust.TWStart)
                    {
                        if (actualIndex != 1)
                        {
                            totalTravelTime += CalculateEarlyPenaltyTerm(arrivalTime, cust.TWStart);//cust.TWStart - arrivalTime;
                            if (parent.AdjustEarlyArrivalToTWStart)
                                arrivalTime = cust.TWStart;
                        }
                        else
                        {
                            arrivalTime = cust.TWStart;
                        }

                        //For testing not allowing wait
                        //return (false,true,double.MinValue);
                        //totalTravelTime += penalty;

                    }

                    load -= cust.Demand;
                    var time = CustomerDist(cust, route[i + skip], load);
                    totalTravelTime += time;
                    arrivalTime += time + cust.ServiceTime;
                    i += skip;

                    //We visited the cust so the index in the new route should be increased.
                    actualIndex += 1;
                }

                if (i == ignore)
                    i += 1;

                if (arrivalTime > route[i].TWEnd)
                    //If we dont meet the timewindow on the route and we have not visited the new customer, we are late because of the additional load. The customer can never fit in this route
                    if (i < pos)
                    {
                        //CustPossibleAtPosCache[(cust.Id,pos)] = (false, false, double.MinValue);
                        if (parent.AllowLateArrivalDuringSearch)
                            totalTravelTime += CalculateLatePenaltyTerm(arrivalTime, route[i].TWEnd);
                        else
                            return (false, false, double.MinValue);

                    }
                    else
                    {
                        //CustPossibleAtPosCache[(cust.Id, pos)] = (false, true, double.MinValue);
                        if (parent.AllowLateArrivalDuringSearch)
                            totalTravelTime += CalculateLatePenaltyTerm(arrivalTime, route[i].TWEnd);
                        else
                            return (false, true, double.MinValue);
                    }

                //Wait for the timewindow start
                else if (arrivalTime < route[i].TWStart)
                {
                    if (actualIndex != 1)
                    {
                        totalTravelTime += CalculateEarlyPenaltyTerm(arrivalTime, route[i].TWStart);//route[i].TWStart - arrivalTime;
                        if (parent.AdjustEarlyArrivalToTWStart)
                            arrivalTime = route[i].TWStart;
                    }
                    else
                    {
                        arrivalTime = route[i].TWStart;
                    }

                }
                load -= route[i].Demand;

                if (i != route.Count - 1)
                {
                    double time;
                    //If the current customer is the customer before the potential position of the new customer update the time accordingly
                    if (i == pos - 1)
                        time = CustomerDist(route[i], cust, load);
                    else if (i == ignore - 1)
                        time = CustomerDist(route[i], route[i+2], load);
                    else
                        time = CustomerDist(route[i], route[i + 1], load);
                    totalTravelTime += time;
                    arrivalTime += time + route[i].ServiceTime;

                }
                //}
            }
            //double objective = CachedObjective;
            //if (objective == -1)
            //    objective = CalcObjective();

            //CustPossibleAtPosCache[(cust.Id, pos)] = (true, true, totalTravelTime - objective);
            return (true, true, totalTravelTime - this.Score);

        }

        public (int, double) BestPossibleInsert(Customer cust)
        {
            double bestDistIncr = double.MaxValue;
            if (BestCustomerPos.ContainsKey(cust.Id))
            {
#if DEBUG
                bestFitCacheHit++;
#endif
                return BestCustomerPos[cust.Id];
            }
#if DEBUG
            bestFitCacheMiss ++;
#endif
            //ResetCache();
            int bestIndex = -1;
            if (this.used_capacity + cust.Demand > max_capacity)
                return (bestIndex, bestDistIncr);
            for (int i = 1; i < route.Count; i++)
            {
                (bool possible, bool everPossible, double distIncrease) = CustPossibleAtPos(cust, i);
                if (!everPossible)
                    break;
                if (possible)
                    if (distIncrease < bestDistIncr)
                    {
                        bestDistIncr = distIncrease;
                        bestIndex = i;
                    }


            }
            BestCustomerPos[cust.Id] = (bestIndex,bestDistIncr);
            return (bestIndex, bestDistIncr);
        }

        public void ReverseSubRoute(int index1, int index2, List<double> newArrivalTimes)
        {

            //Invalidate the cache
            

            this.arrival_times = newArrivalTimes;

            this.route.Reverse(index1, index2 - index1 + 1);
            ResetCache();
        }

        //Using the function is quite a bit slower than using specifically made functions, but is definatly a possibility. 
        //It can therefore be used to test new operators for example
        public (bool possible,double improvement,List<double> newArrivalTimes) NewRoutePossible(List<Customer> newRoute,double changedCapacity)
        {
            double load = used_capacity + changedCapacity;
            double arrivalTime = 0;
            double newCost = 0;
            List<double> newArrivalTimes = new List<double>(newRoute.Count) { 0};

            for(int i = 0; i < newRoute.Count-1; i++)
            {
                var dist = CustomerDist(newRoute[i], newRoute[i + 1], load);
                arrivalTime += dist + newRoute[i].ServiceTime;
                newCost += dist;
                if(arrivalTime < newRoute[i + 1].TWStart)
                {
                    if (i != 0)
                    {
                        if (parent.AllowEarlyArrivalDuringSearch)
                        {
                            //TODO: might want to make this an option in the configuration
                            newCost += CalculateEarlyPenaltyTerm(arrivalTime, newRoute[i + 1].TWStart);
                            if (parent.AdjustEarlyArrivalToTWStart)
                                arrivalTime = newRoute[i + 1].TWStart;
                        }
                        else
                            return (false, double.MinValue, newArrivalTimes);
                    }
                    else
                    {
                        newArrivalTimes[0] = newRoute[i + 1].TWStart - arrivalTime;
                        arrivalTime = newRoute[i + 1].TWStart;
                    }


                }
                else if(arrivalTime > newRoute[i+1].TWEnd)
                {
                    if (parent.AllowLateArrivalDuringSearch)
                    {
                        newCost += CalculateLatePenaltyTerm(arrivalTime, newRoute[i+1].TWEnd);
                    }
                    else
                        return(false, double.MinValue,newArrivalTimes);
                }

                load -= newRoute[i + 1].Demand;
                newArrivalTimes.Add(arrivalTime);

            }
            return (true,this.Score - newCost,newArrivalTimes);
        }

        public void SetNewRoute(List<Customer> customers, List<double> arrivalTimes)
        {
            this.route = customers;
            this.arrival_times = arrivalTimes;
            ResetCache();
        }

        public (bool possible, double improvement,List<double> newArrivalTimes) CanReverseSubRoute(int index1, int index2)
        {
            double load = used_capacity;
            double arrival_time = 0;
            double newCost = 0;
            //int[] newArrivalTimes = new int[arrival_times.Count];
            List<double> newArrivalTimes = new List<double>(route.Count) { 0};
            //Check if the action would be possible and calculate the new objective score
            for(int i=0; i<route.Count - 1; i++)
            {
                Customer currentCust;
                Customer nextCust;
                if(i >= index1 && i <= index2)
                {
                    //In the to be reversed subroute, select in reversed order
                    currentCust = route[index2 - i + index1];
                    if (i < index2)
                        nextCust = route[index2 - i + index1 - 1];
                    else
                        nextCust = route[i + 1];

                }
                else if(i == index1 - 1)
                {
                    nextCust = route[index2];
                    currentCust = route[i];
                }
                else
                {
                    currentCust = route[i];
                    nextCust = route[i + 1];
                }






                //Travel time to new customer
                double dist = CustomerDist(currentCust, nextCust,load);
                //Add travel time to total cost
                newCost += dist;

                //Update arrival time for next customer
                arrival_time += dist + currentCust.ServiceTime;

                if (arrival_time < nextCust.TWStart)
                {
                    if (i != 0)
                    {
                        newCost += CalculateEarlyPenaltyTerm(arrival_time, nextCust.TWStart);
                        if (parent.AdjustEarlyArrivalToTWStart)
                            arrival_time = nextCust.TWStart;
                    }
                    else
                    {
                        newArrivalTimes[0] = nextCust.TWStart - arrival_time;
                        arrival_time = nextCust.TWStart;
                    }
                }

                newArrivalTimes.Add(arrival_time);

                //Check the timewindow end of the next customer
                if (arrival_time > nextCust.TWEnd)
                    if (parent.AllowLateArrivalDuringSearch)
                        newCost += CalculateLatePenaltyTerm(arrival_time, nextCust.TWEnd);
                    else
                        return (false, double.MinValue,newArrivalTimes);

                //After traveling to the next customer we can remove it's load
                load -= nextCust.Demand;

            }

            //double objective = CachedObjective;
            //if (objective == -1)
            //    objective = CalcObjective();
            //if (newArrivalTimes[0] != arrival_times[0])
            //    Console.WriteLine("Maybe problemo");
            return (true, this.Score - newCost, newArrivalTimes);
        }

        public (Customer? toRemove, double objectiveDecrease,int index) RandomCust()
        {
            if (route.Count == 2)
                return (null, double.MaxValue,-1);

            var i = random.Next(1, route.Count - 1);
            double newCost = 0;
            double load = used_capacity - route[i].Demand;
            double arrival_time = 0;

            for (int j = 0, actualIndex=0; j < route.Count - 1; j++,actualIndex++ )
            {
                double time;
                Customer nextCust;

                //Skip the to be removed customer
                if (i == j)
                {
                    //Skipping the removed customer should not interfere with the actual index
                    actualIndex--;
                    continue;
                }

                //If the next customer would be the removed customer, skip it.
                if (j == i - 1)
                {
                    nextCust = route[j + 2];
                }
                else
                    nextCust = route[j + 1];


                var cost = CustomerDist(route[j], nextCust, load);
                newCost += cost;
                arrival_time += cost + route[j].ServiceTime;
                if (arrival_time < nextCust.TWStart)
                {
                    if (actualIndex != 0)
                    {
                        newCost += CalculateEarlyPenaltyTerm(arrival_time, nextCust.TWStart);//nextCust.TWStart- arrival_time;
                                                                                             //newCost += penalty;
                        if (parent.AdjustEarlyArrivalToTWStart)
                            arrival_time = nextCust.TWStart;
                    }
                    else
                    {
                        arrival_time = nextCust.TWStart;
                    }
                }
                else if(arrival_time > nextCust.TWEnd)
                        newCost += CalculateLatePenaltyTerm(arrival_time,nextCust.TWEnd);
                load -= nextCust.Demand;
            }

            //double objective = CachedObjective;
            //if (objective == -1)
            //    objective = CalcObjective();

            if (route.Count == 3 && newCost != 0)
                Solver.ErrorPrint("FOUT");

            return (route[i], this.Score - newCost,i);
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
            //double TArrivalNewCust = arrival_times[pos - 1] + route[pos - 1].ServiceTime + CustomerDist(cust, route[pos - 1]);
            //if(TArrivalNewCust < cust.TWStart)
            //    TArrivalNewCust = cust.TWStart;
            double newArrivalTime = 0;// TArrivalNewCust + CustomerDist(route[pos], cust) + cust.ServiceTime;
            used_capacity += cust.Demand;
            double load = used_capacity;
            startTime = 0;
            double newCustArrivalTime = 0;
            ViolatesLowerTimeWindow = false;
            ViolatesUpperTimeWindow  = false;
            
            for (int i = 0, actualIndex = 0; i < route.Count; i++,actualIndex++)
            {
                if (i == pos)
                {
                    if (newArrivalTime < cust.TWStart)
                    {
                        if (actualIndex != 1)
                        {
                            if (parent.AdjustEarlyArrivalToTWStart)
                                newArrivalTime = cust.TWStart;
                            ViolatesLowerTimeWindow = true;
                        }
                        else
                        {
                            this.startTime = cust.TWStart - newArrivalTime;
                            arrival_times[0] = startTime;
                            newArrivalTime = cust.TWStart;
                        }
                    }
                    else if (newArrivalTime > cust.TWEnd)
                        ViolatesUpperTimeWindow = true;

                    newCustArrivalTime = newArrivalTime;
                    load -= cust.Demand;
                    newArrivalTime += CustomerDist(cust, route[i], load) + cust.ServiceTime;

                    //Take the new customer into account into the route length
                    actualIndex++;
                    //if (newArrivalTime < route[i].TWStart)
                    //    newArrivalTime = route[i].TWStart;

                    //arrival_times[i] = newArrivalTime;
                }
                //else
                //{
                if (newArrivalTime < route[i].TWStart)
                {
                    if (actualIndex != 1)
                    {
                        if (parent.AdjustEarlyArrivalToTWStart)
                            newArrivalTime = route[i].TWStart;
                        ViolatesLowerTimeWindow = true;
                    }
                    else
                    {
                        startTime = route[i].TWStart - newArrivalTime;
                        arrival_times[0] = startTime;
                        newArrivalTime = route[i].TWStart;
                    }
                }
                else if (newArrivalTime > route[i].TWEnd)
                    ViolatesUpperTimeWindow = true;
                arrival_times[i] = newArrivalTime;
                load -= route[i].Demand;
                if (i != route.Count - 1)
                {
                    double time;
                    //If the next index is the new cust target pos, use the travel time to the new customer
                    if (i == pos - 1)
                        time = CustomerDist(route[i], cust, load);
                    else
                        time = CustomerDist(route[i], route[i + 1], load);
                    newArrivalTime += time + route[i].ServiceTime;
                }

                //}

            }
            arrival_times.Insert(pos, newCustArrivalTime);
            route.Insert(pos, cust);
            ResetCache();
        }

        public (bool, double, int) CanSwapBetweenRoutes(Customer cust1, Customer cust2, int index)
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

        //Checks wheter timewindows are met, cached arrival times are correct and if the capacity constraints are not violated.
        //Assumes starting time of planning horizon of 0 and that distance matrix is correct.
        public bool CheckRouteValidity()
        {
            
            double arrivalTime = startTime;
            bool failed = false;
            double usedCapacity = 0;
            double load = route.Sum(x=> x.Demand);

            if(load > max_capacity)
            {
                failed = true;
                Console.WriteLine($"FAIL exceeded vehicle capacity {route}");
            }


            for (int i = 0; i < route.Count - 1; i++)
            {
                var dist = CustomerDist(route[i],route[i+1], load);
                arrivalTime += dist + route[i].ServiceTime;

                if(arrivalTime < route[i+1].TWStart)
                {
                    if (!parent.AllowEarlyArrival && i != 0)
                    {
                        failed = true;
                        Console.WriteLine("FAIL arrived to early at customer");
                    }

                    if(parent.AdjustEarlyArrivalToTWStart)
                        arrivalTime = route[i+1].TWStart;
                    //Do something with penalty?
                }

                if(Math.Round(arrival_times[i+1],6) != Math.Round(arrivalTime, 6))
                {
                    failed = true;
                    Console.WriteLine($"FAIL arrival times did not match {arrivalTime} and {arrival_times[i+1]} for cust {route[i+1].Id} on route {route}");
                }
                if(arrivalTime > route[i + 1].TWEnd)
                {
                    if (!parent.AllowLateArrival)
                    {
                        failed = true;
                        Console.WriteLine($"FAIL did not meet customer {route[i + 1].Id}:{route[i + 1]} due date. Arrived on {arrivalTime} on route {route}");
                    }

                }
                load -= route[i + 1].Demand;
                //if (arrivalTime > route[i].TWEnd)
                //{
                //    failed = true;
                //    Console.WriteLine($"FAIL did not meet customer {route[i].Id}:{route[i]} due date. Arrived on {arrivalTime} on route {route}");
                //}
                //if (arrivalTime < route[i].TWStart)
                //    arrivalTime = route[i].TWStart;
                //if (arrivalTime < arrival_times[i] - Math.Pow(10, -9) || arrivalTime > arrival_times[i] + Math.Pow(10, -9))
                //{
                //    Console.WriteLine($"FAIL arrival times did not match {arrivalTime} and {arrival_times[i]} for cust {route[i].Id} on route {route}");
                //    failed = true;
                //}
                //arrivalTime += Math.Sqrt(Math.Pow(route[i].X - route[i + 1].X, 2) + Math.Pow(route[i].Y - route[i + 1].Y, 2)) + route[i].ServiceTime;

            }
            return failed;
        }

        public Route CreateDeepCopy()
        {
            return new Route(route.ConvertAll(i => i), arrival_times.ConvertAll(i => i), objective_matrix, used_capacity, max_capacity,random.Next(),this.parent, startTime);
        }

        public List<int> CreateIdList()
        {
            return route.ConvertAll(i => i.Id);
        }

    }
}
