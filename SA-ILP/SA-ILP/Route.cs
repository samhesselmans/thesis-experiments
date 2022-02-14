using System;
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
        public int numLoadLevels;
        public double time_done;
        //public Customer lastCust;
        public double used_capacity;
        public double max_capacity;

        private double penalty = 100;

        private Random random;

#if DEBUG
        public long numReference = 0;
#endif
        public Route(Customer depot, double[,,] distanceMatrix, double maxCapacity)
        {
            this.route = new List<Customer>() { depot, depot };
            this.arrival_times = new List<double>() { 0, 0 };
            this.objective_matrix = distanceMatrix;
            this.numLoadLevels = distanceMatrix.GetLength(2);
            this.time_done = 0;
            //this.lastCust = depot;
            used_capacity = 0;
            this.max_capacity = maxCapacity;
            random = new Random();

        }

        public Route(List<Customer> route, List<double> arrivalTimes, double[,,] distanceMatrix, double usedCapcity, double maxCapacity)
        {
            this.route = route;
            this.arrival_times = arrivalTimes;
            this.objective_matrix = distanceMatrix;
            this.numLoadLevels = distanceMatrix.GetLength(2);
            this.time_done = 0;
            used_capacity = usedCapcity;
            this.max_capacity = maxCapacity;
            random = new Random();

        }

        public double CalculatePenaltyTerm(double arrivalTime,double timewindowStart)
        {
            return 0;// 1000 + timewindowStart - arrivalTime;// timewindowStart - arrivalTime;
        }

        public double CalcObjective()
        {
            var total_dist = 0.0;
            double total_time_waited = 0;
            var totalWeight = used_capacity;
            for (int i = 0; i < route.Count - 1; i++)
            {
                total_dist += this.CustomerDist(route[i], route[i + 1], totalWeight);

                //Adding heavy penalty for violating timewindow start
                if (arrival_times[i] + CustomerDist(route[i], route[i + 1], totalWeight) + route[i].ServiceTime < route[i + 1].TWStart)
                    //total_dist += penalty;
                    total_dist += CalculatePenaltyTerm((arrival_times[i] + CustomerDist(route[i], route[i + 1], totalWeight) + route[i].ServiceTime), route[i + 1].TWStart);//route[i + 1].TWStart - (arrival_times[i] + CustomerDist(route[i], route[i + 1], totalWeight) + route[i].ServiceTime);
                totalWeight -= route[i + 1].Demand;
            }
            return total_dist;
        }

        public double CustomerDist(Customer cust1, Customer cust2, double weight)
        {
#if DEBUG
            numReference += 1;
#endif
            int loadLevel = (int)((weight / max_capacity) * numLoadLevels);

            //This happens if the vehicle is fully loaded. It wants to check the next loadlevel
            if (loadLevel == numLoadLevels)
                loadLevel--;

            var val = objective_matrix[cust1.Id, cust2.Id, loadLevel];
            return val;
        }

        public void RemoveCust(Customer cust)
        {
            double newArriveTime = 0;
            int index = -1;
            Customer lastCust = null;
            Customer previous_cust = route[0];
            this.used_capacity -= cust.Demand;
            double load = used_capacity;
            for (int i = 1; i < route.Count - 1; i++)
            {
                var c = route[i];
                if (c.Id != cust.Id)
                {
                    var dist = CustomerDist(previous_cust, c, load);
                    load -= c.Demand;
                    if (newArriveTime + dist < c.TWStart)
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
            if (index == -1)
                Console.WriteLine("Helpt");
            route.RemoveAt(index);
            arrival_times.RemoveAt(index);
            //this.lastCust = lastCust;


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

                if (arrival_time > currentCust.TWEnd)
                    return (false, double.MinValue);

                if(arrival_time < currentCust.TWStart)
                {
                    totalTravelTime += CalculatePenaltyTerm(arrival_time, currentCust.TWStart);

                    //Wil ik dit nog doen bij de violation van een timewindow? Misschien moet ik alleen de penalty toepassen en niet de arrivaltime aanpassen
                    arrival_time = currentCust.TWStart;
                }
                var dist = CustomerDist(currentCust, nextCust, load);
                load -= nextCust.Demand;
                totalTravelTime += dist;
                arrival_time += dist + currentCust.ServiceTime;

                if (arrival_time > nextCust.TWEnd)
                    return (false, double.MinValue);

            }
            return (true, totalTravelTime - CalcObjective());
        }

        public (bool possible, bool possibleInLaterPosition, double objectiveIncrease) CustPossibleAtPos(Customer cust, int pos, int skip = 0)
        {
            double totalTravelTime = 0;
            double load = used_capacity + cust.Demand;

            //Remove the demand of the removed customers from the inital load
            for (int i = 0; i < skip; i++)
            {
                load -= route[pos + i].Demand;
            }

            //Need to check capacity, otherwise loadlevel claculation fails
            if (load > max_capacity)
                return (false, false, double.MinValue);

            double arrivalTime = 0;
            for (int i = 0; i < route.Count; i++)
            {
                if (i == pos)
                {
                    if (arrivalTime > cust.TWEnd)
                        return (false, false, double.MinValue);

                    //Wait for the timewindow start
                    if (arrivalTime < cust.TWStart)
                    {
                        totalTravelTime += CalculatePenaltyTerm(arrivalTime,cust.TWStart);//cust.TWStart - arrivalTime;
                        arrivalTime = cust.TWStart;

                        //For testing not allowing wait
                        //return (false,true,double.MinValue);
                        //totalTravelTime += penalty;

                    }

                    load -= cust.Demand;
                    var time = CustomerDist(cust, route[i + skip], load);
                    totalTravelTime += time;
                    arrivalTime += time + cust.ServiceTime;
                    i += skip;
                }
                //else
                //{
                if (arrivalTime > route[i].TWEnd)
                    //If we dont meet the timewindow on the route and we have not visited the new customer, we are late because of the additional load. The customer can never fit in this route
                    if (i < pos)
                        return (false, false, double.MinValue);
                    else
                        return (false, true, double.MinValue);

                //Wait for the timewindow start
                if (arrivalTime < route[i].TWStart)
                {
                    totalTravelTime += CalculatePenaltyTerm(arrivalTime,route[i].TWStart);//route[i].TWStart - arrivalTime;
                    arrivalTime = route[i].TWStart;

                    //For testing not allowing wait
                    //return (false, true, double.MinValue);
                    //totalTravelTime += penalty;

                }
                load -= route[i].Demand;

                if (i != route.Count - 1)
                {
                    double time;
                    //If the current customer is the customer before the potential position of the new customer update the time accordingly
                    if (i == pos - 1)
                        time = CustomerDist(route[i], cust, load);
                    else
                        time = CustomerDist(route[i], route[i + 1], load);
                    totalTravelTime += time;
                    arrivalTime += time + route[i].ServiceTime;

                }
                //}
            }
            //TODO: cache current objective value
            return (true, true, totalTravelTime - CalcObjective());


            ////Check upper timewindow of the new customer
            //if (arrival_times[pos - 1] + route[pos - 1].ServiceTime > cust.TWEnd && skip == 0)
            //    return (false, false, double.MinValue);

            ////TODO: cache these values
            //double delivered = 0;
            //for(int i =0; i< pos;i++)
            //    delivered += route[i].Demand;
            //double currentLoad = used_capacity - delivered;


            ////Check upper timewindow with distance
            //double TArrivalNewCust = arrival_times[pos-1] + route[pos-1].ServiceTime + CustomerDist( route[pos-1],cust,currentLoad);
            //if (TArrivalNewCust > cust.TWEnd)
            //    return (false, false, double.MinValue);
            ////Set the arrival time to the start of the timewindow if the vehicle arrives to early
            //if (TArrivalNewCust < cust.TWStart)
            //    TArrivalNewCust = cust.TWStart;

            //currentLoad -= cust.Demand;

            //double newArrivalTime = TArrivalNewCust + CustomerDist(cust,route[pos+skip],currentLoad) + cust.ServiceTime;
            //for(int i = pos + skip; i < route.Count; i++)
            //{
            //    if (newArrivalTime > route[i].TWEnd)
            //        return (false, true, double.MinValue);
            //    if(newArrivalTime < route[i].TWStart)
            //        newArrivalTime = route[i].TWStart;
            //    currentLoad -= cust.Demand;
            //    if (i != route.Count - 1)
            //        newArrivalTime += CustomerDist(route[i], route[i + 1], currentLoad) + route[i].ServiceTime;
            //}
            ////Objective has to be completly recalculated...
            //double distIncrease = CustomerDist(route[pos - 1], cust, used_capacity - delivered) + CustomerDist(cust, route[pos + skip], used_capacity - delivered + cust.Demand) - CustomerDist(route[pos - 1], route[pos], used_capacity - delivered);

            ////Kan dit geen problemen veroorzaken?
            //for(int i=0;i< skip; i++)
            //{
            //    distIncrease -= CustomerDist(route[pos + i],route[pos + i + 1]);
            //}

            //return (true, true, distIncrease);
        }

        public (int, double) BestPossibleInsert(Customer cust)
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
                if (possible)
                    if (distIncrease < bestDistIncr)
                    {
                        bestDistIncr = distIncrease;
                        bestIndex = i;
                    }


            }
            return (bestIndex, bestDistIncr);
        }

        public (Customer? toRemove, double objectiveDecrease) RandomCust()
        {
            if (route.Count == 2)
                return (null, double.MaxValue);

            var i = random.Next(1, route.Count - 1);
            double newCost = 0;
            double load = used_capacity - route[i].Demand;
            double arrival_time = 0;
            for (int j = 0; j < route.Count - 1; j++)
            {
                double time;
                Customer nextCust;

                //Skip the to be removed customer
                if (i == j)
                {
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
                    newCost += CalculatePenaltyTerm(arrival_time, nextCust.TWStart);//nextCust.TWStart- arrival_time;
                    //newCost += penalty;
                    arrival_time = nextCust.TWStart;
                }
                load -= nextCust.Demand;
            }

            return (route[i], CalcObjective() - newCost);
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
            double newCustArrivalTime = 0;
            for (int i = 0; i < route.Count; i++)
            {
                if (i == pos)
                {
                    if (newArrivalTime < cust.TWStart)
                        newArrivalTime = cust.TWStart;

                    newCustArrivalTime = newArrivalTime;
                    load -= cust.Demand;
                    newArrivalTime += CustomerDist(cust, route[i], load) + cust.ServiceTime;

                    //if (newArrivalTime < route[i].TWStart)
                    //    newArrivalTime = route[i].TWStart;

                    //arrival_times[i] = newArrivalTime;
                }
                //else
                //{
                if (newArrivalTime < route[i].TWStart)
                    newArrivalTime = route[i].TWStart;
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

        public bool CheckRouteValidity()
        {
            //TODO: update to use loadlevels
            double arrivalTime = 0;
            bool failed = false;
            double usedCapacity = 0;
            for (int i = 0; i < route.Count - 1; i++)
            {
                usedCapacity += route[i + 1].Demand;
                if (usedCapacity > max_capacity)
                {
                    failed = true;
                    Console.WriteLine($"FAIL exceeded vehicle capacity {route}");
                }
                if (arrivalTime > route[i].TWEnd)
                {
                    failed = true;
                    Console.WriteLine($"FAIL did not meet customer {route[i].Id}:{route[i]} due date. Arrived on {arrivalTime} on route {route}");
                }
                if (arrivalTime < route[i].TWStart)
                    arrivalTime = route[i].TWStart;
                if (arrivalTime < arrival_times[i] - Math.Pow(10, -9) || arrivalTime > arrival_times[i] + Math.Pow(10, -9))
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
            return new Route(route.ConvertAll(i => i), arrival_times.ConvertAll(i => i), objective_matrix, used_capacity, max_capacity);
        }

        public List<int> CreateIdList()
        {
            return route.ConvertAll(i => i.Id);
        }

    }
}
