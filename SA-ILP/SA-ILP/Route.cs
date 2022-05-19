using MathNet.Numerics.Distributions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SA_ILP
{
    public struct RouteSimmulationResult
    {
        public double AverageTravelTime { get; private set; }
        public int TotalSimmulations { get; private set; }
        public int TotalToEarly { get; private set; }
        public int TotalToLate { get; private set; }

        public int NumSimmulations { get; private set; }

        public double OnTimePercentage { get; private set; }

        public double[] CustomerOnTimePercentage { get; private set; }

        public RouteSimmulationResult(double totalTravelTime,int numSimmulations, int totalSimmulations, int totalToEarly, int totalToLate,int[] customerToLate,int[] customerToEarly)
        {
            AverageTravelTime = totalTravelTime / numSimmulations;
            TotalSimmulations = totalSimmulations;
            TotalToEarly = totalToEarly;
            TotalToLate = totalToLate;
            NumSimmulations = numSimmulations;
            OnTimePercentage = (double)(totalSimmulations - totalToEarly - totalToLate) / totalSimmulations;
            CustomerOnTimePercentage = new double[customerToLate.Length];
            for(int i =0; i< customerToLate.Length; i++)
            {
                CustomerOnTimePercentage[i] = (double)(numSimmulations - customerToLate[i] - customerToEarly[i])/numSimmulations;
            }

        }


    }
    internal class Route
    {
        public List<Customer> route;
        public List<double> arrival_times;
        public List<IContinuousDistribution> customerDistributions;
        public double[,,] objective_matrix;
        public Gamma[,,] distributionMatrix;
        public double startTime = 0;

        public int numLoadLevels;
        public int numX;
        public int numY;

        public double Score
        {
            get
            {
                if (CachedObjective != -1)
                    return CachedObjective;
                else
                    return CalcObjective();

            }
        }

        private int CashedHashCode = -1;
        public int HashCode
        {
            get
            {
                if (CashedHashCode != -1)
                    return CashedHashCode;
                else
                    return GetHashCode();

            }

        }

        public bool ViolatesLowerTimeWindow { get; private set; }
        public bool ViolatesUpperTimeWindow { get; private set; }

        LocalSearch parent;

        public double time_done;
        //public Customer lastCust;
        public double used_capacity;
        public double max_capacity;
        private double CachedObjective;
        private Dictionary<int, (int, double)> BestCustomerPos;
        private Dictionary<(int, int), (bool, bool, double)> CustPossibleAtPosCache;

        private Random random;

#if DEBUG
        public long bestFitCacheHit = 0;
        public long bestFitCacheMiss = 0;
#endif
        public Route(Customer depot, double[,,] distanceMatrix, Gamma[,,] distributionMatrix, double maxCapacity, int seed, LocalSearch parent)
        {
            this.route = new List<Customer>() { depot, depot };
            this.arrival_times = new List<double>() { 0, 0 };
            this.customerDistributions = new List<IContinuousDistribution>() { null, null };
            objective_matrix = distanceMatrix;
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
            this.distributionMatrix = distributionMatrix;
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

        public Route(List<Customer> route, List<double> arrivalTimes, List<IContinuousDistribution> customerDistributions, double[,,] distanceMatrix, Gamma[,,] distributionMatrix, double usedCapcity, double maxCapacity, int seed, LocalSearch parent, double startTime)
        {
            this.route = route;
            this.arrival_times = arrivalTimes;
            this.customerDistributions = customerDistributions;
            objective_matrix = distanceMatrix;
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
            this.distributionMatrix = distributionMatrix;
            //BestCustomerPos = new Dictionary<int, (int, double)>();
            ResetCache();
        }

        public Route(List<Customer> customers, RouteStore routeStore, Customer depot, double[,,] distanceMatrix, Gamma[,,] distributionMatrix, double maxCapacity, LocalSearch parent)
        {
            this.route = new List<Customer>() { depot, depot };
            this.arrival_times = new List<double>() { 0, 0 };
            this.customerDistributions = new List<IContinuousDistribution>() { null, null };
            objective_matrix = distanceMatrix;
            this.numX = distanceMatrix.GetLength(0);
            this.numY = distanceMatrix.GetLength(1);
            this.numLoadLevels = distanceMatrix.GetLength(2);
            //this.objeciveMatrix1d = new double[numX * numY * numLoadLevels];
            //Create1DMAtrix();
            this.parent = parent;
            this.distributionMatrix = distributionMatrix;
            this.time_done = 0;
            //this.lastCust = depot;
            used_capacity = 0;
            this.max_capacity = maxCapacity;
            random = new Random();

            ResetCache();

            foreach (int cust in routeStore.Route)
            {
                //DO Not insert the depot's
                if (cust != 0)
                    this.InsertCust(customers.First(x => x.Id == cust), route.Count - 1);
            }

        }

        public double CalculateEarlyPenaltyTerm(double arrivalTime, double timewindowStart)
        {
            if (!parent.Config.PenalizeEarlyArrival)
                return 0;// 100 + timewindowStart - arrivalTime;// timewindowStart - arrivalTime;
            else
            {

                //Add possibility to remove ramping based on penalty
                double scale = 1;

                if (parent.Config.ScaleEarlinessPenaltyWithTemperature)
                    scale = parent.Temperature / parent.Config.InitialTemperature;
                return (parent.Config.BaseEarlyArrivalPenalty + timewindowStart - arrivalTime) /scale;

            }

        }
        public double CalculateLatePenaltyTerm(double arrivalTime, double timeWindowEnd)
        {
            if (!parent.Config.PenalizeLateArrival)
                return 0;
            else
            {

                double scale = 1;

                if (parent.Config.ScaleLatenessPenaltyWithTemperature)
                    scale =parent.Temperature / parent.Config.InitialTemperature;


                return (parent.Config.BaseLateArrivalPenalty + arrivalTime - timeWindowEnd) / scale;
            }
        }
        public double CalculateUncertaintyPenaltyTerm(IContinuousDistribution dist, Customer cust, double minArrrivalTime)
        {
            //double onTimeP = 1 - (dist.CumulativeDistribution(cust.TWEnd - minArrrivalTime) - dist.CumulativeDistribution(cust.TWStart - minArrrivalTime));

            if (parent.Config.ExpectedLatenessPenalty == 0 && parent.Config.ExpectedEarlinessPenalty == 0)
                return 0;

            double toLateP = 1;
            if (cust.TWEnd - minArrrivalTime >= 0)
                toLateP = 1 - dist.CumulativeDistribution(cust.TWEnd - minArrrivalTime);
            double toEarlyP = 0;
            if (cust.TWStart - minArrrivalTime >= 0)
                toEarlyP = dist.CumulativeDistribution(cust.TWStart - minArrrivalTime);

            if (Double.IsNaN(toLateP))
                toLateP = 0;

            double res = toLateP * parent.Config.ExpectedLatenessPenalty + toEarlyP * parent.Config.ExpectedEarlinessPenalty;



            return res;
        }

        public RouteSimmulationResult Simulate(int numSimulations = 1000)
        {
            int timesOnTime = 0;
            int timesTotal = 0;
            int toLate = 0;
            int toEarly = 0;
            double totalTravelTime = 0;
            int[] toLateCount = new int[route.Count];
            int[] toEarlyCount = new int[route.Count];
            for (int x = 0; x < numSimulations; x++)
            {
                double load = used_capacity;
                double arrivalTime = startTime;
                for (int i = 0; i < route.Count - 1; i++)
                {
                    (double dist, IContinuousDistribution distribution) = CustomerDist(route[i], route[i + 1],load);
                    load -= route[i + 1].Demand;

                    double tt = dist + distribution.Sample();
                    totalTravelTime += tt;
                    arrivalTime += route[i].ServiceTime + tt;

                    if (arrivalTime <= route[i + 1].TWEnd && arrivalTime >= route[i + 1].TWStart)
                    {
                        timesOnTime++;
                    }
                    else if (arrivalTime > route[i + 1].TWEnd && !parent.Config.AllowLateArrival)
                    {
                        toLate++;
                        toLateCount[i+1]++;
                    }
                    else
                    {
                        if (!parent.Config.AllowEarlyArrival)
                        {
                            toEarly++;
                            toEarlyCount[i+1]++;
                        }

                        if (parent.Config.AdjustEarlyArrivalToTWStart)
                            arrivalTime = route[i + 1].TWStart;

                    }
                    timesTotal++;

                }
            }

            //Console.WriteLine($"Finished {numSimulations} simulations");
            //Console.WriteLine($"On time percentage {((double)(timesTotal-toEarly -toLate)/timesTotal)* 100}%. {timesTotal - timesOnTime} times not on time in {route.Count * numSimulations}. Of these {toEarly} were to early and {toLate} to late");

            //Console.WriteLine($"Average travel time: {totalTravelTime / numSimulations}. Score: {Score}");

            return new RouteSimmulationResult(totalTravelTime,numSimulations, timesTotal, toEarly, toLate,toLateCount,toEarlyCount);

        }



        private Gamma AddDistributions(Gamma left, Gamma right)
        {
            if (parent.Config.ExpectedLatenessPenalty == 0 && parent.Config.ExpectedEarlinessPenalty == 0)
                return left;

            return new Gamma(left.Shape + right.Shape, right.Rate);
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

            double totalObjectiveValue = 0;
            double totalWeight = used_capacity;// route.Sum(x => x.Demand);
            double arrivalTime = startTime;

            //TODO: rate halen uit distributies
            Gamma total = new Gamma(0, 10);
            for (int i = 0; i < route.Count - 1; i++)
            {
                (double dist, Gamma distribution) = this.CustomerDist(route[i], route[i + 1], totalWeight);
                
                totalObjectiveValue += dist;
                arrivalTime += dist + route[i].ServiceTime;

                if (parent.Config.UseMeanOfDistributionForTravelTime)
                {
                    arrivalTime += distribution.Mean;

                }
                if (parent.Config.UseMeanOfDistributionForScore)
                    totalObjectiveValue += distribution.Mean;

                total = AddDistributions(total, distribution);
                totalObjectiveValue += CalculateUncertaintyPenaltyTerm(total, route[i + 1], arrivalTime);
                if (arrivalTime < route[i + 1].TWStart)
                {
                    if (i != 0)
                    {
                        //Console.WriteLine($"Score to early in route {this} at {route[i + 1]}");
                        totalObjectiveValue += CalculateEarlyPenaltyTerm(arrivalTime, route[i + 1].TWStart);
                        if (parent.Config.AdjustEarlyArrivalToTWStart)
                            arrivalTime = route[i + 1].TWStart;
                    }
                    else
                    {
                        arrivalTime = route[i + 1].TWStart;
                    }
                }
                else if (arrivalTime > route[i + 1].TWEnd)
                {
                    totalObjectiveValue += CalculateLatePenaltyTerm(arrivalTime, route[i + 1].TWEnd);
                }

                totalWeight -= route[i + 1].Demand;
            }
            CachedObjective = totalObjectiveValue;
            return totalObjectiveValue;
        }

        public (double, Gamma) CustomerDist(Customer start, Customer finish, double weight)
        {
            int loadLevel = (int)((Math.Max(0, weight - 0.000001) / max_capacity) * numLoadLevels);

            //This happens if the vehicle is fully loaded. It wants to check the next loadlevel
            if (loadLevel == numLoadLevels)
                loadLevel--;

            var val = objective_matrix[start.Id, finish.Id, loadLevel];
            //var val2 = objeciveMatrix1d[cust1.Id + cust2.Id * numX + loadLevel * numX * numY];
            //if (val != val2)
            //    Console.WriteLine("wops");
            return (val, distributionMatrix[start.Id, finish.Id, loadLevel]);
        }

        public void ResetCache()
        {
            BestCustomerPos = new Dictionary<int, (int, double)>();
            CustPossibleAtPosCache = new Dictionary<(int, int), (bool, bool, double)>();
            CachedObjective = -1;
        }

        public void RemoveCust(Customer cust)
        {

            ViolatesUpperTimeWindow = false;
            ViolatesLowerTimeWindow = false;

            arrival_times[0] = 0;
            int index = -1;
            Customer lastCust = null;
            Customer previous_cust = route[0];
            this.used_capacity -= cust.Demand;
            double newArriveTime = OptimizeStartTime(route, used_capacity, toRemove: cust);
            startTime = newArriveTime;
            double load = used_capacity;
            arrival_times[0] = newArriveTime;

            Gamma total = new Gamma(0, 10);

            for (int i = 1, actualIndex = 1; i < route.Count; i++, actualIndex++)
            {
                var c = route[i];
                if (c.Id != cust.Id)
                {
                    (var dist, var distribution) = CustomerDist(previous_cust, c, load);
                    total = AddDistributions(total, distribution);
                    load -= c.Demand;
                    newArriveTime += dist;

                    if (parent.Config.UseMeanOfDistributionForTravelTime)
                        newArriveTime += distribution.Mean;

                    if (newArriveTime < c.TWStart)
                    {
                        if (actualIndex != 1)
                        {
                            ViolatesLowerTimeWindow = true;
                            if (parent.Config.AdjustEarlyArrivalToTWStart)
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

                    customerDistributions[i] = total;
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
            customerDistributions.RemoveAt(index);
            //this.lastCust = lastCust;
            ResetCache();

        }

        public (bool possible, double decrease) CanSwapInternally(Customer cust1, Customer cust2, int index1, int index2)
        {
            double load = used_capacity;
            double arrival_time = OptimizeStartTime(route, load, swapIndex1: index1, swapIndex2: index2);
            double objectiveValue = 0;

            //TODO: Get paramter from actual distributions
            Gamma total = new Gamma(0, 10);

            for (int i = 0; i < route.Count - 1; i++)
            {
                Customer currentCust;
                Customer nextCust;
                if (i == index1 - 1)
                    nextCust = cust2;
                else if (i == index2 - 1)
                    nextCust = cust1;
                else
                    nextCust = route[i + 1];

                if (i == index1)
                    currentCust = cust2;
                else if (i == index2)
                    currentCust = cust1;
                else
                    currentCust = route[i];

                //if (arrival_time > currentCust.TWEnd)
                //    if (allowLateTimewindow)
                //        totalTravelTime += CalculateLatePenaltyTerm(arrival_time, currentCust.TWEnd);
                //    else
                //        return (false, double.MinValue);

                if (arrival_time < currentCust.TWStart)
                {
                    if (i != 1)
                    {
                        objectiveValue += CalculateEarlyPenaltyTerm(arrival_time, currentCust.TWStart);

                        if (parent.Config.AdjustEarlyArrivalToTWStart)
                            arrival_time = currentCust.TWStart;
                    }
                    else
                    {
                        arrival_time = currentCust.TWStart;
                    }
                }
                (var dist, var distribution) = CustomerDist(currentCust, nextCust, load);
                total = AddDistributions(total, distribution);

                load -= nextCust.Demand;
                objectiveValue += dist;
                arrival_time += dist + currentCust.ServiceTime;

                if (parent.Config.UseMeanOfDistributionForTravelTime)
                    arrival_time += distribution.Mean;
                if (parent.Config.UseMeanOfDistributionForScore)
                    objectiveValue += distribution.Mean;

                objectiveValue += CalculateUncertaintyPenaltyTerm(total, nextCust, arrival_time);
                if (arrival_time > nextCust.TWEnd)
                    if (parent.Config.AllowLateArrivalDuringSearch)
                        objectiveValue += CalculateLatePenaltyTerm(arrival_time, nextCust.TWEnd);
                    else
                        return (false, double.MinValue);

            }

            //double objective = CachedObjective;
            //if(objective == -1)
            //    objective = CalcObjective();

            return (true, objectiveValue - this.Score);
        }

        public (bool possible, bool possibleInLaterPosition, double objectiveIncrease) CustPossibleAtPos(Customer cust, int pos, int skip = 0, int ignore = -1)
        {
            //Possible optimization: Cache minimal weight required for a load level change in the route. If the new load is less than that we only have to check from pos

            //if (CustPossibleAtPosCache.ContainsKey((cust.Id, pos)))
            //    return CustPossibleAtPosCache[(cust.Id, pos)];

            //if (skip > 0 && ignore != -1)
            //    throw new Exception("Cant use ignore and skip together");

            double totalObjectiveValue = 0;
            double load = used_capacity + cust.Demand;

            //Remove the demand of the removed customers from the inital load
            for (int i = 0; i < skip; i++)
            {
                load -= route[pos + i].Demand;
            }

            if (ignore != -1)
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
            Gamma total = new Gamma(0, 10);
            double arrivalTime = OptimizeStartTime(route, load, toAdd: cust, pos: pos, skip: skip, ignore: ignore);
            for (int i = 0, actualIndex = 0; i < route.Count; i++, actualIndex++)
            {

                //Arrived at the insert position. Include the new Customer into the check
                if (i == pos)
                {
                    if (arrivalTime > cust.TWEnd)
                    {
                        //CustPossibleAtPosCache[(cust.Id, pos)] = (false, false, double.MinValue);
                        if (parent.Config.AllowLateArrivalDuringSearch)
                            totalObjectiveValue += CalculateLatePenaltyTerm(arrivalTime, cust.TWEnd);
                        else
                            return (false, false, double.MinValue);


                    }

                    //Wait for the timewindow start
                    else if (arrivalTime < cust.TWStart)
                    {
                        if (actualIndex != 1)
                        {
                            totalObjectiveValue += CalculateEarlyPenaltyTerm(arrivalTime, cust.TWStart);//cust.TWStart - arrivalTime;
                            if (parent.Config.AdjustEarlyArrivalToTWStart)
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
                    (var time, var distribution) = CustomerDist(cust, route[i + skip], load);


                    totalObjectiveValue += time;
                    arrivalTime += time + cust.ServiceTime;

                    if (parent.Config.UseMeanOfDistributionForTravelTime)
                        arrivalTime += distribution.Mean;
                    if (parent.Config.UseMeanOfDistributionForScore)
                        totalObjectiveValue += distribution.Mean;

                    total = AddDistributions(total, distribution);
                    totalObjectiveValue += CalculateUncertaintyPenaltyTerm(total, route[i + skip], arrivalTime);

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
                        if (parent.Config.AllowLateArrivalDuringSearch)
                            totalObjectiveValue += CalculateLatePenaltyTerm(arrivalTime, route[i].TWEnd);
                        else
                            return (false, false, double.MinValue);

                    }
                    else
                    {
                        //CustPossibleAtPosCache[(cust.Id, pos)] = (false, true, double.MinValue);
                        if (parent.Config.AllowLateArrivalDuringSearch)
                            totalObjectiveValue += CalculateLatePenaltyTerm(arrivalTime, route[i].TWEnd);
                        else
                            return (false, true, double.MinValue);
                    }

                //Wait for the timewindow start
                else if (arrivalTime < route[i].TWStart)
                {
                    if (actualIndex != 1)
                    {
                        //Console.WriteLine($"Expecting to early in route {this} at {route[i]}");
                        totalObjectiveValue += CalculateEarlyPenaltyTerm(arrivalTime, route[i].TWStart);//route[i].TWStart - arrivalTime;
                        if (parent.Config.AdjustEarlyArrivalToTWStart)
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
                    Gamma distribution;
                    Customer nextCust;
                    //If the current customer is the customer before the potential position of the new customer update the time accordingly
                    if (i == pos - 1)
                        nextCust = cust;
                    //(time,distribution) = CustomerDist(route[i], cust, load);
                    else if (i == ignore - 1)
                        nextCust = route[i + 2];
                    //(time, distribution) = CustomerDist(route[i], route[i+2], load);
                    else
                        nextCust = route[i + 1];
                    //(time, distribution) = CustomerDist(route[i], route[i + 1], load);

                    (time, distribution) = CustomerDist(route[i], nextCust, load);

                    totalObjectiveValue += time;
                    arrivalTime += time + route[i].ServiceTime;

                    if (parent.Config.UseMeanOfDistributionForTravelTime)
                        arrivalTime += distribution.Mean;
                    if (parent.Config.UseMeanOfDistributionForScore)
                        totalObjectiveValue += distribution.Mean;

                    total = AddDistributions(total, distribution);
                    totalObjectiveValue += CalculateUncertaintyPenaltyTerm(total, nextCust, arrivalTime);

                }
                //}
            }
            //double objective = CachedObjective;
            //if (objective == -1)
            //    objective = CalcObjective();

            //CustPossibleAtPosCache[(cust.Id, pos)] = (true, true, totalTravelTime - objective);
            return (true, true, totalObjectiveValue - this.Score);

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
            BestCustomerPos[cust.Id] = (bestIndex, bestDistIncr);
            return (bestIndex, bestDistIncr);
        }

        public void ReverseSubRoute(int index1, int index2, List<double> newArrivalTimes, List<IContinuousDistribution> newDistributions, bool violatesLowerTimewindow, bool violatesUpperTimeWindow)
        {

            //Invalidate the cache

            this.startTime = newArrivalTimes[0];
            this.arrival_times = newArrivalTimes;
            this.customerDistributions = newDistributions;
            this.ViolatesLowerTimeWindow = violatesLowerTimewindow;
            this.ViolatesUpperTimeWindow = violatesUpperTimeWindow;
            this.route.Reverse(index1, index2 - index1 + 1);
            ResetCache();
        }

        bool lastOptimizationFailed = false;

        //WARNING DO NOT USE DIFFERENT COMBINATIONS OF PARAMETERS. SOME COMBINATIONS ARE NOT SUPPORTED AND NOT CHECKED
        public double OptimizeStartTime(List<Customer> toOptimizeOver, double load, Customer? toRemove = null, int skip = 0, Customer? toAdd = null, int pos = -1, int ignore = -1, int swapIndex1 = -1, int swapIndex2 = -1, int reverseIndex1 = -1, int reverseIndex2 = -1)
        {

            //If early arrival is allowed this optimization of the start time is unneccesary.
            if (parent.Config.AllowEarlyArrival)
                return 0;
            lastOptimizationFailed = false;
            double startTimeLowerBound = 0;
            double startTimeUpperBound = double.MaxValue;
            double arrivalTime = 0;
            double val = 0;
            double l = load;

            Customer currentCust;// = toOptimizeOver[0];
            Customer nextCust;

            for (int i = 0; i < toOptimizeOver.Count; i++)
            {
                //if (currentCust == toRemove)
                //    i += 1 + skip;
                if (i == ignore)
                    i++;
                currentCust = toOptimizeOver[i];
                if (currentCust == toRemove)
                {
                    i += 1;
                    currentCust = toOptimizeOver[i];
                }
                else if (i == pos && toAdd != null)
                {
                    currentCust = toOptimizeOver[i + skip];
                    i += skip;
                    if (toAdd.TWStart - val > startTimeLowerBound)
                        startTimeLowerBound = toAdd.TWStart - val;
                    if (toAdd.TWEnd - val < startTimeUpperBound)
                        startTimeUpperBound = toAdd.TWEnd - val;
                    l -= toAdd.Demand;
                    (var dist, var distribution) = CustomerDist(toAdd, currentCust, l);
                    val += dist + toAdd.ServiceTime;

                    if (parent.Config.UseMeanOfDistributionForTravelTime)
                        val += distribution.Mean;


                }
                else if (i == swapIndex1)
                    currentCust = toOptimizeOver[swapIndex2];
                else if (i == swapIndex2)
                    currentCust = toOptimizeOver[swapIndex1];
                else if (i >= reverseIndex1 && i <= reverseIndex2)
                    currentCust = toOptimizeOver[reverseIndex2 - i + reverseIndex1];


                if (currentCust.TWStart - val > startTimeLowerBound)
                    startTimeLowerBound = currentCust.TWStart - val;
                if (currentCust.TWEnd - val < startTimeUpperBound)
                    startTimeUpperBound = currentCust.TWEnd - val;
                //systemOfEquationsLower.Add(newRoute[i].TWStart - val);
                //systemOfEquationsUpper.Add(newRoute[i].TWEnd - val);
                l -= currentCust.Demand;

                if (i < toOptimizeOver.Count - 1)
                {
                    nextCust = toOptimizeOver[i + 1];
                    if (nextCust == toRemove)
                        nextCust = toOptimizeOver[i + 2 + skip];

                    //If the next customer is in the position of the new customer, set next to new.
                    else if (i == pos - 1 && toAdd != null)
                        nextCust = toAdd;
                    else if (i == ignore - 1)
                        nextCust = toOptimizeOver[i + 2];
                    else if (i == swapIndex1 - 1)
                        nextCust = toOptimizeOver[swapIndex2];
                    else if (i == swapIndex2 - 1)
                        nextCust = toOptimizeOver[swapIndex1];
                    else if (i >= reverseIndex1 - 1 && i < reverseIndex2)
                        nextCust = toOptimizeOver[reverseIndex2 - i + reverseIndex1 - 1];
                    (var dist, var distribution) = CustomerDist(currentCust, nextCust, l);
                    val += dist + currentCust.ServiceTime;

                    if (parent.Config.UseMeanOfDistributionForTravelTime)
                        val += distribution.Mean;

                }
            }
            double epsilon = 0.0000001;

            if (startTimeLowerBound != 0 && startTimeLowerBound + epsilon <= startTimeUpperBound)
            {
                arrivalTime = startTimeLowerBound + epsilon;
            }
            else if (startTimeUpperBound >= 0)
            {
                lastOptimizationFailed = true;
                //Console.WriteLine("Did not found start time optimization");
                arrivalTime = startTimeUpperBound;
            }
            return arrivalTime;
        }


        //Using the function is quite a bit slower than using specifically made functions, but is definatly a possibility. 
        //It can therefore be used to test new operators for example
        public (bool possible, double improvement, List<double> newArrivalTimes, List<IContinuousDistribution> newDistributions, bool, bool) NewRoutePossible(List<Customer> newRoute, double changedCapacity)
        {
            double load = used_capacity + changedCapacity;

            if (load > max_capacity)
                return (false, double.MinValue, null, null, false, false);

            double arrivalTime = 0;
            double newObjectiveValue = 0;
            bool violatesLowerTimeWindow = false;
            bool violatesUpperTimeWindow = false;
            List<double> newArrivalTimes = new List<double>(newRoute.Count) { };
            List<IContinuousDistribution> newDistributions = new List<IContinuousDistribution>(newRoute.Count);
            //List<double> systemOfEquationsLower = new List<double>(newRoute.Count);
            //List<double> systemOfEquationsUpper = new List<double>(newRoute.Count);

            arrivalTime = OptimizeStartTime(newRoute, load);

            //Adding the arrival time for the depot. This is used for setting the start time of the route.
            newArrivalTimes.Add(arrivalTime);
            newDistributions.Add(null);
            Gamma total = new Gamma(0, 10);
            for (int i = 0; i < newRoute.Count - 1; i++)
            {
                (var dist, var distribution) = CustomerDist(newRoute[i], newRoute[i + 1], load);
                total = AddDistributions(total, distribution);
                arrivalTime += dist + newRoute[i].ServiceTime;


                if (parent.Config.UseMeanOfDistributionForTravelTime)
                    arrivalTime += distribution.Mean;
                if (parent.Config.UseMeanOfDistributionForScore)
                    newObjectiveValue += distribution.Mean;
                newObjectiveValue += dist;
                newObjectiveValue += CalculateUncertaintyPenaltyTerm(total, newRoute[i + 1], arrivalTime);
                if (arrivalTime < newRoute[i + 1].TWStart)
                {
                    if (i != 0)
                    {
                        if (parent.Config.AllowEarlyArrivalDuringSearch)
                        {
                            //TODO: might want to make this an option in the configuration
                            newObjectiveValue += CalculateEarlyPenaltyTerm(arrivalTime, newRoute[i + 1].TWStart);
                            if (parent.Config.AdjustEarlyArrivalToTWStart)
                                arrivalTime = newRoute[i + 1].TWStart;
                            violatesLowerTimeWindow = true;
                        }
                        else
                            return (false, double.MinValue, newArrivalTimes, newDistributions, false, false);
                    }
                    else
                    {
                        newArrivalTimes[0] = newRoute[i + 1].TWStart - arrivalTime;
                        arrivalTime = newRoute[i + 1].TWStart;
                    }


                }
                else if (arrivalTime > newRoute[i + 1].TWEnd)
                {
                    if (parent.Config.AllowLateArrivalDuringSearch)
                    {
                        newObjectiveValue += CalculateLatePenaltyTerm(arrivalTime, newRoute[i + 1].TWEnd);
                        violatesUpperTimeWindow = true;
                    }
                    else
                        return (false, double.MinValue, newArrivalTimes, newDistributions, false, false);
                }

                load -= newRoute[i + 1].Demand;
                newArrivalTimes.Add(arrivalTime);
                newDistributions.Add(total);

            }
            return (true, this.Score - newObjectiveValue, newArrivalTimes, newDistributions, violatesLowerTimeWindow, violatesUpperTimeWindow);
        }

        public void SetNewRoute(List<Customer> customers, List<double> arrivalTimes, List<IContinuousDistribution> newDistributions, bool violatesLowerTimeWindow, bool violatesUpperTimeWindow)
        {
            this.route = customers;
            this.arrival_times = arrivalTimes;
            this.startTime = arrivalTimes[0];
            this.customerDistributions = newDistributions;
            this.ViolatesLowerTimeWindow = violatesLowerTimeWindow;
            this.ViolatesUpperTimeWindow = violatesUpperTimeWindow;
            this.used_capacity = customers.Sum(x => x.Demand);
            ResetCache();
        }

        public (bool possible, double improvement, List<double> newArrivalTimes, List<IContinuousDistribution> newDistributions, bool violatesLowerTimeWindow, bool violatesUpperTimeWindow) CanReverseSubRoute(int index1, int index2)
        {
            double load = used_capacity;
            double arrival_time = OptimizeStartTime(route, load, reverseIndex1: index1, reverseIndex2: index2);
            double newObjectiveValue = 0;

            bool violatesLowerTimeWindow = false;
            bool violatesUpperTimeWindow = false;

            //int[] newArrivalTimes = new int[arrival_times.Count];
            List<double> newArrivalTimes = new List<double>(route.Count) { arrival_time };
            List<IContinuousDistribution> newDistributions = new List<IContinuousDistribution>(route.Count) { null };
            Gamma total = new Gamma(0, 10);
            //Check if the action would be possible and calculate the new objective score
            for (int i = 0; i < route.Count - 1; i++)
            {
                Customer currentCust;
                Customer nextCust;
                if (i >= index1 && i <= index2)
                {
                    //In the to be reversed subroute, select in reversed order
                    currentCust = route[index2 - i + index1];
                    if (i < index2)
                        nextCust = route[index2 - i + index1 - 1];
                    else
                        nextCust = route[i + 1];

                }
                else if (i == index1 - 1)
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
                (double dist, Gamma distribution) = CustomerDist(currentCust, nextCust, load);
                total = AddDistributions(total, distribution);



                //Add travel time to total cost
                newObjectiveValue += dist;

                //Update arrival time for next customer
                arrival_time += dist + currentCust.ServiceTime;

                if (parent.Config.UseMeanOfDistributionForTravelTime)
                    arrival_time += distribution.Mean;
                if (parent.Config.UseMeanOfDistributionForScore)
                    newObjectiveValue += distribution.Mean;

                newObjectiveValue += CalculateUncertaintyPenaltyTerm(total, nextCust, arrival_time);

                if (arrival_time < nextCust.TWStart)
                {
                    if (i != 0)
                    {
                        newObjectiveValue += CalculateEarlyPenaltyTerm(arrival_time, nextCust.TWStart);
                        if (parent.Config.AdjustEarlyArrivalToTWStart)
                            arrival_time = nextCust.TWStart;
                        violatesLowerTimeWindow = true;
                    }
                    else
                    {
                        newArrivalTimes[0] = nextCust.TWStart - arrival_time;
                        arrival_time = nextCust.TWStart;
                    }
                }

                newArrivalTimes.Add(arrival_time);
                newDistributions.Add(total);
                //Check the timewindow end of the next customer
                if (arrival_time > nextCust.TWEnd)
                    if (parent.Config.AllowLateArrivalDuringSearch)
                    {
                        violatesUpperTimeWindow = true;
                        newObjectiveValue += CalculateLatePenaltyTerm(arrival_time, nextCust.TWEnd);
                    }
                    else
                        return (false, double.MinValue, newArrivalTimes, newDistributions, violatesLowerTimeWindow, false);

                //After traveling to the next customer we can remove it's load
                load -= nextCust.Demand;

            }

            //double objective = CachedObjective;
            //if (objective == -1)
            //    objective = CalcObjective();
            //if (newArrivalTimes[0] != arrival_times[0])
            //    Console.WriteLine("Maybe problemo");
            return (true, this.Score - newObjectiveValue, newArrivalTimes, newDistributions, violatesLowerTimeWindow, violatesUpperTimeWindow);
        }

        public (Customer? toRemove, double objectiveDecrease, int index) RandomCust()
        {
            if (route.Count == 2)
                return (null, double.MaxValue, -1);

            var i = random.Next(1, route.Count - 1);
            double newObjectiveValue = 0;
            double load = used_capacity - route[i].Demand;
            double arrival_time = OptimizeStartTime(route, load, toRemove: route[i]);
            Gamma total = new Gamma(0, 10);
            for (int j = 0, actualIndex = 0; j < route.Count - 1; j++, actualIndex++)
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


                (var cost, var distribution) = CustomerDist(route[j], nextCust, load);
                total = AddDistributions(total, distribution);

                newObjectiveValue += cost;



                arrival_time += cost + route[j].ServiceTime;

                if (parent.Config.UseMeanOfDistributionForTravelTime)
                    arrival_time += distribution.Mean;
                if (parent.Config.UseMeanOfDistributionForScore)
                    newObjectiveValue += distribution.Mean;

                newObjectiveValue += CalculateUncertaintyPenaltyTerm(total, nextCust, arrival_time);


                if (arrival_time < nextCust.TWStart)
                {
                    if (actualIndex != 0)
                    {
                        newObjectiveValue += CalculateEarlyPenaltyTerm(arrival_time, nextCust.TWStart);//nextCust.TWStart- arrival_time;
                                                                                                       //newCost += penalty;
                        if (parent.Config.AdjustEarlyArrivalToTWStart)
                            arrival_time = nextCust.TWStart;
                    }
                    else
                    {
                        arrival_time = nextCust.TWStart;
                    }
                }
                else if (arrival_time > nextCust.TWEnd)
                    newObjectiveValue += CalculateLatePenaltyTerm(arrival_time, nextCust.TWEnd);
                load -= nextCust.Demand;
            }

            //double objective = CachedObjective;
            //if (objective == -1)
            //    objective = CalcObjective();

            //if (route.Count == 3 && newObjectiveValue != 0)
            //    Solver.ErrorPrint("FOUT");

            return (route[i], this.Score - newObjectiveValue, i);
        }

        public (Customer?, int) RandomCustIndex()
        {
            if (route.Count == 2)
                return (null, -1);

            var i = random.Next(1, route.Count - 1);
            return (route[i], i);
        }

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
            for (int i = 0; i < route.Count; i++)
            {
                total += route[i].Id * (i + 1001);
            }
            CashedHashCode = total;
            return total;
            //return Route.Sum(x=>);//String.Join(";", Route).GetHashCode();
        }

        public void InsertCust(Customer cust, int pos)
        {
            //double TArrivalNewCust = arrival_times[pos - 1] + route[pos - 1].ServiceTime + CustomerDist(cust, route[pos - 1]);
            //if(TArrivalNewCust < cust.TWStart)
            //    TArrivalNewCust = cust.TWStart;
            // TArrivalNewCust + CustomerDist(route[pos], cust) + cust.ServiceTime;
            used_capacity += cust.Demand;
            double load = used_capacity;
            double newCustArrivalTime = 0;
            ViolatesLowerTimeWindow = false;
            ViolatesUpperTimeWindow = false;
            double newArrivalTime = OptimizeStartTime(route, used_capacity, toAdd: cust, pos: pos);
            startTime = newArrivalTime;

            Gamma total = new Gamma(0, 10);

            Gamma? newCustDistribution = null;
            for (int i = 0, actualIndex = 0; i < route.Count; i++, actualIndex++)
            {
                if (i == pos)
                {
                    if (newArrivalTime < cust.TWStart)
                    {
                        if (actualIndex != 1)
                        {
                            if (parent.Config.AdjustEarlyArrivalToTWStart)
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
                    newCustDistribution = total;
                    load -= cust.Demand;

                    (double dist, Gamma distribution) = CustomerDist(cust, route[i], load);
                    total = AddDistributions(total, distribution);
                    newArrivalTime += dist + cust.ServiceTime;

                    if (parent.Config.UseMeanOfDistributionForTravelTime)
                        newArrivalTime += distribution.Mean;

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
                        if (parent.Config.AdjustEarlyArrivalToTWStart)
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
                if (i != 0)
                    customerDistributions[i] = total;
                load -= route[i].Demand;
                if (i != route.Count - 1)
                {
                    double time;
                    Customer nextCust;
                    //If the next index is the new cust target pos, use the travel time to the new customer
                    if (i == pos - 1)
                        //time = CustomerDist(route[i], cust, load).Item1;
                        nextCust = cust;
                    else
                        //time = CustomerDist(route[i], route[i + 1], load).Item1;
                        nextCust = route[i + 1];
                    (double dist, Gamma distribution) = CustomerDist(route[i], nextCust, load);
                    total = AddDistributions(total, distribution);
                    newArrivalTime += dist + route[i].ServiceTime;
                    if (parent.Config.UseMeanOfDistributionForTravelTime)
                        newArrivalTime += distribution.Mean;
                }

                //}

            }
            arrival_times.Insert(pos, newCustArrivalTime);
            route.Insert(pos, cust);

            if (newCustDistribution == null)
                throw new Exception("Wrong distribution");

            customerDistributions.Insert(pos, newCustDistribution);
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
            double load = route.Sum(x => x.Demand);
            Gamma total = new Gamma(0, 10);
            if (load > max_capacity)
            {
                failed = true;
                Console.WriteLine($"FAIL exceeded vehicle capacity {route}");
            }


            for (int i = 0; i < route.Count - 1; i++)
            {
                (double dist, Gamma distribution) = CustomerDist(route[i], route[i + 1], load);
                arrivalTime += dist + route[i].ServiceTime;

                if (parent.Config.UseMeanOfDistributionForTravelTime)
                    arrivalTime += distribution.Mean;

                total = AddDistributions(total, distribution);



                if (arrivalTime < route[i + 1].TWStart)
                {
                    if (!parent.Config.AllowEarlyArrival && i != 0)
                    {
                        failed = true;
                        Console.WriteLine("FAIL arrived to early at customer");
                    }

                    if (parent.Config.AdjustEarlyArrivalToTWStart)
                        arrivalTime = route[i + 1].TWStart;
                    //Do something with penalty?
                }

                if (Math.Round(arrival_times[i + 1], 6) != Math.Round(arrivalTime, 6))
                {
                    failed = true;
                    Console.WriteLine($"FAIL arrival times did not match {arrivalTime} and {arrival_times[i + 1]} for cust {route[i + 1].Id} on route {route}");
                }
                if (arrivalTime > route[i + 1].TWEnd)
                {
                    if (!parent.Config.AllowLateArrival)
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
            return new Route(route.ConvertAll(i => i), arrival_times.ConvertAll(i => i), customerDistributions.ConvertAll(i => i), objective_matrix, distributionMatrix, used_capacity, max_capacity, random.Next(), this.parent, startTime);
        }

        public List<int> CreateIdList()
        {
            return route.ConvertAll(i => i.Id);
        }

    }
}
