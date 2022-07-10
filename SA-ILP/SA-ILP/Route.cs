using MathNet.Numerics.Distributions;
using MathNet.Numerics;
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

        public double AverageWaitingTime { get; private set; }

        public RouteSimmulationResult(double totalTravelTime, int numSimmulations, int totalSimmulations, int totalToEarly, int totalToLate, int[] customerToLate, int[] customerToEarly, double totalWaitingTime)
        {
            AverageTravelTime = totalTravelTime / numSimmulations;
            AverageWaitingTime = totalWaitingTime / numSimmulations;
            TotalSimmulations = totalSimmulations;
            TotalToEarly = totalToEarly;
            TotalToLate = totalToLate;
            NumSimmulations = numSimmulations;
            OnTimePercentage = (double)(totalSimmulations - totalToEarly - totalToLate) / totalSimmulations;
            CustomerOnTimePercentage = new double[customerToLate.Length];
            for (int i = 0; i < customerToLate.Length; i++)
            {
                CustomerOnTimePercentage[i] = (double)(numSimmulations - customerToLate[i] - customerToEarly[i]) / numSimmulations;
            }

        }


    }
    internal class Route
    {
        public List<Customer> route;
        public List<double> arrival_times;
        public List<IContinuousDistribution> customerDistributions;
        public readonly double[,,] objective_matrix;
        public readonly Gamma[,,] distributionMatrix;
        public readonly IContinuousDistribution[,,] distributionApproximationMatrix;
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


        public Func<Customer, Customer, double, bool, (double, IContinuousDistribution)> CustomerDist;
        public Func<IContinuousDistribution, IContinuousDistribution, double, IContinuousDistribution> AddDistributions;
        public Func<IContinuousDistribution, Customer, double, double> CalculateUncertaintyPenaltyTerm;

        public Route(Customer depot, double[,,] distanceMatrix, Gamma[,,] distributionMatrix, IContinuousDistribution[,,] approximationMatrix, double maxCapacity, int seed, LocalSearch parent)
        {
            this.route = new List<Customer>(20) { depot, depot };
            this.arrival_times = new List<double>(20) { 0, 0 };
            this.customerDistributions = new List<IContinuousDistribution>(20) { null, null };
            objective_matrix = distanceMatrix;
            this.numX = distanceMatrix.GetLength(0);
            this.numY = distanceMatrix.GetLength(1);
            this.numLoadLevels = distanceMatrix.GetLength(2);

            this.parent = parent;

            this.time_done = 0;
            used_capacity = 0;
            this.max_capacity = maxCapacity;
            random = new Random(seed);
            this.distributionMatrix = distributionMatrix;
            this.distributionApproximationMatrix = approximationMatrix;
            ResetCache();

            //Functions are seperated for with and without distributions. This optimes the results without the need of different branches
            SetFunctions();
        }


        public override string ToString()
        {
            return $"({String.Join(',', CreateIdList())})";
        }


        public Route(List<Customer> route, List<double> arrivalTimes, List<IContinuousDistribution> customerDistributions, double[,,] distanceMatrix, Gamma[,,] distributionMatrix, IContinuousDistribution[,,] approximationMatrix, double usedCapcity, double maxCapacity, int seed, LocalSearch parent, double startTime)
        {
            this.route = route;
            this.arrival_times = arrivalTimes;
            this.customerDistributions = customerDistributions;
            objective_matrix = distanceMatrix;
            this.numX = distanceMatrix.GetLength(0);
            this.numY = distanceMatrix.GetLength(1);
            this.numLoadLevels = distanceMatrix.GetLength(2);
            this.parent = parent;
            this.time_done = 0;
            used_capacity = usedCapcity;
            this.max_capacity = maxCapacity;
            this.startTime = startTime;
            random = new Random(seed);
            this.distributionMatrix = distributionMatrix;
            this.distributionApproximationMatrix = approximationMatrix;
            ResetCache();

            SetFunctions();
        }

        public Route(List<Customer> customers, RouteStore routeStore, Customer depot, double[,,] distanceMatrix, Gamma[,,] distributionMatrix, IContinuousDistribution[,,] approximationMatrix, double maxCapacity, LocalSearch parent)
        {
            this.route = new List<Customer>() { depot, depot };
            this.arrival_times = new List<double>() { 0, 0 };
            this.customerDistributions = new List<IContinuousDistribution>() { null, null };
            objective_matrix = distanceMatrix;
            this.numX = distanceMatrix.GetLength(0);
            this.numY = distanceMatrix.GetLength(1);
            this.numLoadLevels = distanceMatrix.GetLength(2);

            this.parent = parent;
            this.distributionMatrix = distributionMatrix;
            this.distributionApproximationMatrix = approximationMatrix;
            this.time_done = 0;
            used_capacity = 0;
            this.max_capacity = maxCapacity;
            random = new Random();

            ResetCache();

            SetFunctions();

            foreach (int cust in routeStore.Route)
            {
                //DO Not insert the depots
                if (cust != 0)
                    this.InsertCust(customers.First(x => x.Id == cust), route.Count - 1);
            }

        }

        private bool UsesStochasticImplementation()
        {
            return parent.Config.UseStochasticFunctions;
        }
        private void SetFunctions()
        {
            if (UsesStochasticImplementation())
            {
                AddDistributions = AddDistributionsStochastic;
                CalculateUncertaintyPenaltyTerm = CalculateUncertaintyPenaltyTermStochastic;
                CustomerDist = CustomerDistWithDistributions;
            }
            else
            {
                AddDistributions = AddDistributionsDeterministic;
                CalculateUncertaintyPenaltyTerm = CalculateUncertaintyPenaltyTermDeterministic;
                CustomerDist = CustomerDistNoDistributions;
            }

        }

        public double CalculateEarlyPenaltyTerm(double arrivalTime, double timewindowStart)
        {
            if (!parent.Config.PenalizeDeterministicEarlyArrival)
                return 0;// 100 + timewindowStart - arrivalTime;// timewindowStart - arrivalTime;
            else
            {

                //Add possibility to remove ramping based on penalty
                double scale = 1;

                if (parent.Config.ScaleEarlinessPenaltyWithTemperature)
                    scale = parent.Temperature / parent.Config.InitialTemperature;
                return (parent.Config.BaseEarlyArrivalPenalty + timewindowStart - arrivalTime) / scale;

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
                    scale = parent.Temperature / parent.Config.InitialTemperature;


                return (parent.Config.BaseLateArrivalPenalty + arrivalTime - timeWindowEnd) / scale;
            }
        }
        public double CalculateUncertaintyPenaltyTermStochastic(IContinuousDistribution dist, Customer cust, double minArrrivalTime)
        {

            double toLateP = 1;
            double cutOff = dist.CumulativeDistribution(0);
            if (cust.TWEnd - minArrrivalTime >= 0)
                if (cutOff < 1)
                    toLateP = (1 - dist.CumulativeDistribution(cust.TWEnd - minArrrivalTime)) / (1 - cutOff);
                else
                    toLateP = 0;

            double toEarlyP = 0;
            if (cust.TWStart - minArrrivalTime >= 0)
                if (cutOff < 1)
                    toEarlyP = (dist.CumulativeDistribution(cust.TWStart - minArrrivalTime) - cutOff) / (1 - cutOff);
                else
                    toEarlyP = 1;

            if (Double.IsNaN(toLateP))
                toLateP = 0;

            if (Double.IsNaN(toEarlyP) && dist.Mean == cust.TWStart - minArrrivalTime && dist.StdDev == 0)
                toEarlyP = 0;

            double res = toLateP * parent.Config.ExpectedLatenessPenalty + toEarlyP * parent.Config.ExpectedEarlinessPenalty;



            return res;
        }

        public double CalculateUncertaintyPenaltyTermDeterministic(IContinuousDistribution dist, Customer cust, double minArrrivalTime)
        {
            return 0;
        }

        public RouteSimmulationResult Simulate(int numSimulations = 1000)
        {
            int timesOnTime = 0;
            int timesTotal = 0;
            int toLate = 0;
            int toEarly = 0;
            double totalTravelTime = 0;
            double totalWaitingTime = 0;
            int[] toLateCount = new int[route.Count];
            int[] toEarlyCount = new int[route.Count];
            for (int x = 0; x < numSimulations; x++)
            {
                double load = used_capacity;
                double arrivalTime = startTime;
                for (int i = 0; i < route.Count - 1; i++)
                {
                    (double dist, IContinuousDistribution distribution) = CustomerDist(route[i], route[i + 1], load, true);
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
                        toLateCount[i + 1]++;
                    }
                    else
                    {
                        if (!parent.Config.AllowEarlyArrivalInSimulation)
                        {
                            toEarly++;
                            toEarlyCount[i + 1]++;
                        }

                        if (parent.Config.AdjustEarlyArrivalToTWStart)
                        {

                            totalWaitingTime += route[i + 1].TWStart - arrivalTime;
                            arrivalTime = route[i + 1].TWStart;

                        }

                    }
                    timesTotal++;

                }
            }


            return new RouteSimmulationResult(totalTravelTime, numSimulations, timesTotal, toEarly, toLate, toLateCount, toEarlyCount, totalWaitingTime);

        }



        private Gamma AddGammaDistributions(Gamma left, Gamma right, double diffWithLowerTimeWindow = -1)
        {
            if (parent.Config.IgnoreWaitingDuringDistributionAddition)
                return new Gamma(left.Shape + right.Shape, right.Rate);

            const double maxShape = 167;


            //First add the two distributions
            Gamma? newDist = null;

            if (left.Rate == right.Rate)
                newDist = new Gamma(left.Shape + right.Shape, right.Rate);
            else
            {

                //MMM approximation

                double mu = left.Shape * left.Scale + right.Shape * right.Scale;
                double betaSquared = (left.Shape * Math.Pow(left.Scale, 2) + right.Shape * Math.Pow(right.Scale, 2));
                double newAlpha = Math.Pow(mu, 2) / betaSquared;
                double newBeta = betaSquared / mu;

                newDist = new Gamma(newAlpha, 1 / newBeta);

            }
            //If the deterministic arrival time is later than the lower timewindow we do not need to use the approximation of the max between a constant and a distribution
            if (diffWithLowerTimeWindow <= 0)
                return newDist;

            double factor = 1;
            if (newDist.Shape > maxShape)
            {
                factor = newDist.Shape / maxShape;

                newDist = new Gamma(newDist.Shape / factor, newDist.Rate * factor);
            }

            double expected = diffWithLowerTimeWindow - diffWithLowerTimeWindow * SpecialFunctions.GammaUpperIncomplete(newDist.Shape, diffWithLowerTimeWindow / newDist.Scale) / SpecialFunctions.Gamma(newDist.Shape) + newDist.Scale * SpecialFunctions.GammaUpperIncomplete(newDist.Shape + 1, diffWithLowerTimeWindow / newDist.Scale) / SpecialFunctions.Gamma(newDist.Shape);


            double expectedSquared = Math.Pow(diffWithLowerTimeWindow, 2) + (-Math.Pow(diffWithLowerTimeWindow, 2) * SpecialFunctions.GammaUpperIncomplete(newDist.Shape, diffWithLowerTimeWindow / newDist.Scale)) / SpecialFunctions.Gamma(newDist.Shape) + Math.Pow(newDist.Scale, 2) * SpecialFunctions.GammaUpperIncomplete(newDist.Shape + 2, diffWithLowerTimeWindow / newDist.Scale) / SpecialFunctions.Gamma(newDist.Shape);



            double variance = expectedSquared - Math.Pow(expected, 2);





            if (variance <= 0)
                variance = 1e-10;

            double finalAlpha = Math.Pow(expected, 2) / variance;
            double finalBeta = variance / expected;





            if (double.IsInfinity(finalBeta) || double.IsInfinity(finalAlpha))
                Console.WriteLine("INFINTE");

            //Invert the scale parameter to get the rate parameter
            return new Gamma(finalAlpha, 1 / finalBeta);

        }

        private Normal AddNormalDistributions(Normal left, Normal right, double diffWithLowerTimeWindow)
        {

            var dist = new Normal(left.Mean + right.Mean, Math.Sqrt(left.Variance + right.Variance));
            if (parent.Config.IgnoreWaitingDuringDistributionAddition || diffWithLowerTimeWindow < 0)
                return dist;

            var standardNormal = new Normal(0, 1);

            double expected = dist.Mean * standardNormal.CumulativeDistribution((dist.Mean - diffWithLowerTimeWindow) / dist.StdDev)
                            + diffWithLowerTimeWindow * standardNormal.CumulativeDistribution((diffWithLowerTimeWindow - dist.Mean) / dist.StdDev)
                            + dist.StdDev * standardNormal.Density((dist.Mean - diffWithLowerTimeWindow) / dist.StdDev);

            double expectedSquared = (dist.Variance + Math.Pow(dist.Mean, 2)) * standardNormal.CumulativeDistribution((dist.Mean - diffWithLowerTimeWindow) / dist.StdDev)
                                    + Math.Pow(diffWithLowerTimeWindow, 2) * standardNormal.CumulativeDistribution((diffWithLowerTimeWindow - dist.Mean) / dist.StdDev)
                                    + (dist.Mean + diffWithLowerTimeWindow) * dist.StdDev * standardNormal.Density((dist.Mean - diffWithLowerTimeWindow) / dist.StdDev);

            double newVariance = expectedSquared - Math.Pow(expected, 2);

            //TEMP
            if (newVariance < 0)
                newVariance = 0;


            return new Normal(expected, Math.Sqrt(newVariance));

            //throw new NotImplementedException();
        }


        private IContinuousDistribution AddDistributionsStochastic(IContinuousDistribution left, IContinuousDistribution right, double diffWithLowerTimeWindow = -1)
        {

            if (left.GetType() == typeof(Gamma))
            {
                return AddGammaDistributions((Gamma)left, (Gamma)right, diffWithLowerTimeWindow);
            }
            else if (left.GetType() == typeof(Normal))
                return AddNormalDistributions((Normal)left, (Normal)right, diffWithLowerTimeWindow);

            throw new NotImplementedException("Unsupported distribution");


        }

        private IContinuousDistribution AddDistributionsDeterministic(IContinuousDistribution left, IContinuousDistribution right, double diffWithLowerTimeWindow = -1)
        {
            return left;
        }

        //Calculate the total objective value of this route
        public double CalcObjective()
        {


            double totalObjectiveValue = 0;
            double totalWeight = used_capacity;
            double arrivalTime = startTime;


            IContinuousDistribution total = parent.Config.DefaultDistribution;
            for (int i = 0; i < route.Count - 1; i++)
            {
                (double dist, IContinuousDistribution distribution) = this.CustomerDist(route[i], route[i + 1], totalWeight, false);

                totalObjectiveValue += dist;
                arrivalTime += dist + route[i].ServiceTime;

                if (parent.Config.UseMeanOfDistributionForTravelTime)
                {
                    arrivalTime += distribution.Mean;

                }
                if (parent.Config.UseMeanOfDistributionForScore)
                    totalObjectiveValue += distribution.Mean;

                total = AddDistributions(total, distribution, route[i + 1].TWStart - arrivalTime);
                totalObjectiveValue += CalculateUncertaintyPenaltyTerm(total, route[i + 1], arrivalTime);
                if (arrivalTime < route[i + 1].TWStart)
                {
                    if (i != 0)
                    {
                        totalObjectiveValue += CalculateEarlyPenaltyTerm(arrivalTime, route[i + 1].TWStart);
                        if (parent.Config.AdjustDeterministicEarlyArrivalToTWStart)
                            arrivalTime = route[i + 1].TWStart;
                    }
                    else if (parent.Config.AdjustDeterministicEarlyArrivalToTWStart)
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
        public static long numDistCalls = 0;
        public (double deterministicDistance, IContinuousDistribution dist) CustomerDistWithDistributions(Customer start, Customer finish, double weight, bool provide_actualDistribution = false)
        {
            double ll = (weight / max_capacity) * numLoadLevels;
            int loadLevel = (int)ll;

            //The upperbound is inclusive
            if (ll == loadLevel && weight != 0)
                loadLevel--;


            var val = objective_matrix[start.Id, finish.Id, loadLevel];

            //True distribution is used during simulation, otherwise use the approximation
            if (provide_actualDistribution)
                return (val, distributionMatrix[start.Id, finish.Id, loadLevel]);
            else
                return (val, distributionApproximationMatrix[start.Id, finish.Id, loadLevel]);
        }


        //Distance function without stochastic parts
        public (double deterministicDistance, IContinuousDistribution dist) CustomerDistNoDistributions(Customer start, Customer finish, double weight, bool provide_actualDistribution = false)
        {
            double ll = (weight / max_capacity) * numLoadLevels;
            int loadLevel = (int)ll;

            //The upperbound is inclusive
            if (ll == loadLevel && weight != 0)
                loadLevel--;

            var val = objective_matrix[start.Id, finish.Id, loadLevel];

            return (val, null);
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

            IContinuousDistribution total = parent.Config.DefaultDistribution;

            for (int i = 1, actualIndex = 1; i < route.Count; i++, actualIndex++)
            {
                var c = route[i];
                if (c.Id != cust.Id)
                {
                    (var dist, IContinuousDistribution distribution) = CustomerDist(previous_cust, c, load, false);

                    load -= c.Demand;
                    newArriveTime += dist;
                    total = AddDistributions(total, distribution, c.TWStart - newArriveTime);
                    if (parent.Config.UseMeanOfDistributionForTravelTime)
                        newArriveTime += distribution.Mean;

                    if (newArriveTime < c.TWStart)
                    {
                        if (actualIndex != 1)
                        {
                            ViolatesLowerTimeWindow = true;
                            if (parent.Config.AdjustDeterministicEarlyArrivalToTWStart)
                                newArriveTime = c.TWStart;
                        }
                        else if (parent.Config.AdjustDeterministicEarlyArrivalToTWStart)
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
            ResetCache();

        }

        public (bool possible, double decrease) CanSwapInternally(Customer cust1, Customer cust2, int index1, int index2)
        {
            double load = used_capacity;
            double arrival_time = OptimizeStartTime(route, load, swapIndex1: index1, swapIndex2: index2);
            double objectiveValue = 0;

            IContinuousDistribution total = parent.Config.DefaultDistribution;

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


                if (arrival_time < currentCust.TWStart)
                {
                    if (i != 1)
                    {
                        objectiveValue += CalculateEarlyPenaltyTerm(arrival_time, currentCust.TWStart);

                        if (parent.Config.AdjustDeterministicEarlyArrivalToTWStart)
                            arrival_time = currentCust.TWStart;
                    }
                    else if (parent.Config.AdjustDeterministicEarlyArrivalToTWStart)
                    {
                        arrival_time = currentCust.TWStart;
                    }
                }
                (var dist, IContinuousDistribution distribution) = CustomerDist(currentCust, nextCust, load, false);


                load -= nextCust.Demand;
                objectiveValue += dist;
                arrival_time += dist + currentCust.ServiceTime;
                total = AddDistributions(total, distribution, nextCust.TWStart - arrival_time);
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


            return (true, objectiveValue - this.Score);
        }



        public (bool possible, bool possibleInLaterPosition, double objectiveIncrease) CustPossibleAtPos(Customer cust, int pos, int skip = 0, int ignore = -1)
        {
            double totalObjectiveValue = 0;
            double load = used_capacity + cust.Demand;

            //Remove the demand of the removed customers from the inital load
            for (int i = 0; i < skip; i++)
            {
                load -= route[pos + i].Demand;
            }

            if (ignore != -1)
                load -= route[ignore].Demand;



            //Need to check capacity, otherwise loadlevel claculation fails
            if (load > max_capacity)
            {
                //CustPossibleAtPosCache[(cust.Id, pos)] = (false, false, double.MinValue);
                return (false, false, double.MinValue);
            }

            //int actualIndex = 0;
            IContinuousDistribution total = parent.Config.DefaultDistribution;
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
                            if (parent.Config.AdjustDeterministicEarlyArrivalToTWStart)
                                arrivalTime = cust.TWStart;
                        }
                        else if (parent.Config.AdjustDeterministicEarlyArrivalToTWStart)
                        {
                            arrivalTime = cust.TWStart;
                        }

                        //For testing not allowing wait
                        //return (false,true,double.MinValue);
                        //totalTravelTime += penalty;

                    }

                    load -= cust.Demand;
                    (var time, IContinuousDistribution distribution) = CustomerDist(cust, route[i + skip], load, false);


                    totalObjectiveValue += time;
                    arrivalTime += time + cust.ServiceTime;

                    if (parent.Config.UseMeanOfDistributionForTravelTime)
                        arrivalTime += distribution.Mean;
                    if (parent.Config.UseMeanOfDistributionForScore)
                        totalObjectiveValue += distribution.Mean;

                    total = AddDistributions(total, distribution, route[i + skip].TWStart - arrivalTime);
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
                        if (parent.Config.AllowLateArrivalDuringSearch)
                            totalObjectiveValue += CalculateLatePenaltyTerm(arrivalTime, route[i].TWEnd);
                        else
                            return (false, false, double.MinValue);

                    }
                    else
                    {
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
                        totalObjectiveValue += CalculateEarlyPenaltyTerm(arrivalTime, route[i].TWStart);//route[i].TWStart - arrivalTime;
                        if (parent.Config.AdjustDeterministicEarlyArrivalToTWStart)
                            arrivalTime = route[i].TWStart;
                    }
                    else if (parent.Config.AdjustDeterministicEarlyArrivalToTWStart)
                    {
                        arrivalTime = route[i].TWStart;
                    }

                }
                load -= route[i].Demand;

                if (i != route.Count - 1)
                {
                    double time;
                    Customer nextCust;
                    //If the current customer is the customer before the potential position of the new customer update the time accordingly
                    if (i == pos - 1)
                        nextCust = cust;
                    else if (i == ignore - 1)
                        nextCust = route[i + 2];
                    else
                        nextCust = route[i + 1];

                    (time, IContinuousDistribution distribution) = CustomerDist(route[i], nextCust, load, false);

                    totalObjectiveValue += time;
                    arrivalTime += time + route[i].ServiceTime;

                    if (parent.Config.UseMeanOfDistributionForTravelTime)
                        arrivalTime += distribution.Mean;
                    if (parent.Config.UseMeanOfDistributionForScore)
                        totalObjectiveValue += distribution.Mean;

                    total = AddDistributions(total, distribution, nextCust.TWStart - arrivalTime);
                    totalObjectiveValue += CalculateUncertaintyPenaltyTerm(total, nextCust, arrivalTime);

                }

            }

            return (true, true, totalObjectiveValue - this.Score);

        }

        public (int, double) BestPossibleInsert(Customer cust)
        {
            double bestDistIncr = double.MaxValue;
            if (BestCustomerPos.ContainsKey(cust.Id))
            {
                return BestCustomerPos[cust.Id];
            }
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
            //Optimizes the start time for all different parts of the route
            //If early arrival is allowed this optimization of the start time is unneccesary.
            if (parent.Config.AllowEarlyArrivalInSimulation)
                return 0;
            lastOptimizationFailed = false;
            double startTimeLowerBound = 0;
            double startTimeUpperBound = double.MaxValue;
            double arrivalTime = 0;
            double val = 0;
            double l = load;

            Customer currentCust;
            Customer nextCust;

            for (int i = 0; i < toOptimizeOver.Count; i++)
            {

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
                    (var dist, IContinuousDistribution distribution) = CustomerDist(toAdd, currentCust, l, false);
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
                    (var dist, IContinuousDistribution distribution) = CustomerDist(currentCust, nextCust, l, false);
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
            List<double> newArrivalTimes = new List<double>(newRoute.Capacity) { };
            List<IContinuousDistribution> newDistributions = new List<IContinuousDistribution>(newRoute.Capacity);


            arrivalTime = OptimizeStartTime(newRoute, load);

            //Adding the arrival time for the depot. This is used for setting the start time of the route.
            newArrivalTimes.Add(arrivalTime);
            newDistributions.Add(null);
            IContinuousDistribution total = parent.Config.DefaultDistribution;
            for (int i = 0; i < newRoute.Count - 1; i++)
            {
                (var dist, IContinuousDistribution distribution) = CustomerDist(newRoute[i], newRoute[i + 1], load, false);

                arrivalTime += dist + newRoute[i].ServiceTime;
                total = AddDistributions(total, distribution, newRoute[i + 1].TWStart - arrivalTime);

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
                            newObjectiveValue += CalculateEarlyPenaltyTerm(arrivalTime, newRoute[i + 1].TWStart);
                            if (parent.Config.AdjustDeterministicEarlyArrivalToTWStart)
                                arrivalTime = newRoute[i + 1].TWStart;
                            violatesLowerTimeWindow = true;
                        }
                        else
                            return (false, double.MinValue, newArrivalTimes, newDistributions, false, false);
                    }
                    else if (parent.Config.AdjustDeterministicEarlyArrivalToTWStart)
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

            List<double> newArrivalTimes = new List<double>(route.Count) { arrival_time };
            List<IContinuousDistribution> newDistributions = new List<IContinuousDistribution>(route.Count) { null };
            IContinuousDistribution total = parent.Config.DefaultDistribution;
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
                (double dist, IContinuousDistribution distribution) = CustomerDist(currentCust, nextCust, load, false);




                //Add travel time to total cost
                newObjectiveValue += dist;

                //Update arrival time for next customer
                arrival_time += dist + currentCust.ServiceTime;
                total = AddDistributions(total, distribution, nextCust.TWStart - arrival_time);
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
                        if (parent.Config.AdjustDeterministicEarlyArrivalToTWStart)
                            arrival_time = nextCust.TWStart;
                        violatesLowerTimeWindow = true;
                    }
                    else if (parent.Config.AdjustDeterministicEarlyArrivalToTWStart)
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
            IContinuousDistribution total = parent.Config.DefaultDistribution;
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


                (var cost, IContinuousDistribution distribution) = CustomerDist(route[j], nextCust, load, false);


                newObjectiveValue += cost;



                arrival_time += cost + route[j].ServiceTime;

                total = AddDistributions(total, distribution, nextCust.TWStart - arrival_time);

                if (parent.Config.UseMeanOfDistributionForTravelTime)
                    arrival_time += distribution.Mean;
                if (parent.Config.UseMeanOfDistributionForScore)
                    newObjectiveValue += distribution.Mean;

                newObjectiveValue += CalculateUncertaintyPenaltyTerm(total, nextCust, arrival_time);


                if (arrival_time < nextCust.TWStart)
                {
                    if (actualIndex != 0)
                    {
                        newObjectiveValue += CalculateEarlyPenaltyTerm(arrival_time, nextCust.TWStart);
                        if (parent.Config.AdjustDeterministicEarlyArrivalToTWStart)
                            arrival_time = nextCust.TWStart;
                    }
                    else if (parent.Config.AdjustDeterministicEarlyArrivalToTWStart)
                    {
                        arrival_time = nextCust.TWStart;
                    }
                }
                else if (arrival_time > nextCust.TWEnd)
                    newObjectiveValue += CalculateLatePenaltyTerm(arrival_time, nextCust.TWEnd);
                load -= nextCust.Demand;
            }


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
            used_capacity += cust.Demand;
            double load = used_capacity;
            double newCustArrivalTime = 0;
            ViolatesLowerTimeWindow = false;
            ViolatesUpperTimeWindow = false;
            double newArrivalTime = OptimizeStartTime(route, used_capacity, toAdd: cust, pos: pos);
            startTime = newArrivalTime;

            IContinuousDistribution total = parent.Config.DefaultDistribution;

            IContinuousDistribution? newCustDistribution = null;
            for (int i = 0, actualIndex = 0; i < route.Count; i++, actualIndex++)
            {
                if (i == pos)
                {
                    if (newArrivalTime < cust.TWStart)
                    {
                        if (actualIndex != 1)
                        {
                            if (parent.Config.AdjustDeterministicEarlyArrivalToTWStart)
                                newArrivalTime = cust.TWStart;
                            ViolatesLowerTimeWindow = true;
                        }
                        else if (parent.Config.AdjustDeterministicEarlyArrivalToTWStart)
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

                    (double dist, IContinuousDistribution distribution) = CustomerDist(cust, route[i], load, false);

                    newArrivalTime += dist + cust.ServiceTime;
                    total = AddDistributions(total, distribution, route[i].TWStart - newArrivalTime);




                    if (parent.Config.UseMeanOfDistributionForTravelTime)
                        newArrivalTime += distribution.Mean;

                    //Take the new customer into account into the route length
                    actualIndex++;

                }

                if (newArrivalTime < route[i].TWStart)
                {
                    if (actualIndex != 1)
                    {
                        if (parent.Config.AdjustDeterministicEarlyArrivalToTWStart)
                            newArrivalTime = route[i].TWStart;
                        ViolatesLowerTimeWindow = true;
                    }
                    else if (parent.Config.AdjustDeterministicEarlyArrivalToTWStart)
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
                        nextCust = cust;
                    else
                        nextCust = route[i + 1];
                    (double dist, IContinuousDistribution distribution) = CustomerDist(route[i], nextCust, load, false);
                    newArrivalTime += dist + route[i].ServiceTime;
                    total = AddDistributions(total, distribution, nextCust.TWStart - newArrivalTime);
                    if (parent.Config.UseMeanOfDistributionForTravelTime)
                        newArrivalTime += distribution.Mean;
                }

                //}

            }
            arrival_times.Insert(pos, newCustArrivalTime);
            route.Insert(pos, cust);


            customerDistributions.Insert(pos, newCustDistribution);
            ResetCache();
        }

        public (bool, double, int) CanSwapBetweenRoutes(Customer cust1, Customer cust2, int index)
        {

            (bool possible, _, double distIncrease) = CustPossibleAtPos(cust2, index, 1);
            possible &= (used_capacity - cust1.Demand + cust2.Demand < max_capacity);
            return (possible, distIncrease, index);

        }

        //Checks wheter timewindows are met, cached arrival times are correct and if the capacity constraints are not violated.
        //Assumes starting time of planning horizon of 0 and that distance matrix is correct.
        public bool CheckRouteValidity()
        {

            double arrivalTime = startTime;
            bool failed = false;
            double usedCapacity = 0;
            double load = route.Sum(x => x.Demand);
            IContinuousDistribution total = parent.Config.DefaultDistribution;
            if (load > max_capacity)
            {
                failed = true;
                Console.WriteLine($"FAIL exceeded vehicle capacity {route}");
            }


            for (int i = 0; i < route.Count - 1; i++)
            {
                (double dist, IContinuousDistribution distribution) = CustomerDist(route[i], route[i + 1], load, false);
                arrivalTime += dist + route[i].ServiceTime;

                if (parent.Config.UseMeanOfDistributionForTravelTime)
                    arrivalTime += distribution.Mean;

                total = AddDistributions(total, distribution, route[i + 1].TWStart - arrivalTime);



                if (arrivalTime < route[i + 1].TWStart)
                {
                    if (!parent.Config.AllowDeterministicEarlyArrival && i != 0)
                    {
                        failed = true;
                        Console.WriteLine("FAIL arrived to early at customer");
                    }

                    if (parent.Config.AdjustDeterministicEarlyArrivalToTWStart)
                        arrivalTime = route[i + 1].TWStart;
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

            }
            return failed;
        }

        public Route CreateDeepCopy()
        {
            return new Route(route.ConvertAll(i => i), arrival_times.ConvertAll(i => i), customerDistributions.ConvertAll(i => i), objective_matrix, distributionMatrix, distributionApproximationMatrix, used_capacity, max_capacity, random.Next(), this.parent, startTime);
        }

        public List<int> CreateIdList()
        {
            return route.ConvertAll(i => i.Id);
        }

    }
}
