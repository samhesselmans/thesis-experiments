using MathNet.Numerics.Distributions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SA_ILP
{
    internal static class VRPLTT
    {
        private static double CalculateSpeed(double heightDiff, double length, double vehicleMass,double powerInput)
        {
            double speed = 25;
            //double slope = Math.Atan(heightDiff / length);// * Math.PI / 180;
            double slope = Math.Asin(heightDiff / length);// * Math.PI / 180;
            double requiredPow = CalcRequiredForce(speed / 3.6, vehicleMass, slope);
            double orignalPow = requiredPow;
            if (powerInput >= requiredPow)
            {
                    return speed;
            }
            while (speed > 0)
            {
                if (powerInput >= requiredPow)
                {
                    if (orignalPow + requiredPow - 2 * powerInput < 0)
                        speed += 0.01;
                    return speed;
                }

                speed -= 0.01;
                requiredPow = CalcRequiredForce(speed / 3.6, vehicleMass, slope);
            }
            return 0;
        }

        public static double CalculateTravelTime(double heightDiff, double length, double vehicleMass, double powerInput)
        {
            if (length == 0)
                return 0;
            length *= 1000;
            //Speed in m/s
            double speed = CalculateSpeed(heightDiff, length, vehicleMass, powerInput)/3.6;

            //Return travel time in minutes
            return length  / speed /60;
        }

        public static Gamma CreateTravelTimeDistribution(double weight, double traveltime)
        {
            double shape = 1.5 + traveltime * 0.5 + weight/(290);
            //RATE moet zelfde blijven, niet shape
            double rate = 10;// (1 / (traveltime * 0.1 / 8))/(1 + weight/(290*3));
            var gamma = new Gamma(shape, rate);

            return gamma;
        }


        public static (double[,,],Gamma[,,]) CalculateLoadDependentTimeMatrix(List<Customer> customers, double[,] distanceMatrix, double minWeight, double maxWeight, int numLoadLevels, double powerInput)
        {
            double[,,] matrix = new double[customers.Count, customers.Count, numLoadLevels];
            Gamma[,,] distributionMatrix = new Gamma[customers.Count,customers.Count, numLoadLevels];
            //List<(double, double, double)> plotData = new List<(double, double, double)>();
            Parallel.For(0, customers.Count, i =>
            {
                for (int j = 0; j < customers.Count; j++)
                {
                    double dist;
                    if (i < j)
                        dist = distanceMatrix[i, j];
                    else
                        dist = distanceMatrix[j, i];
                    double heightDiff = customers[j].Elevation - customers[i].Elevation;

                    //double slope = Math.Atan(heightDiff /( dist * 1000));
                    //if (dist == 0)
                    //    slope = 0;
                    ////if(j > i)
                    //total += Math.Abs(slope);
                    for (int l = 0; l < numLoadLevels; l++)
                    {
                        double loadLevelWeight = minWeight + ((maxWeight - minWeight) / numLoadLevels) * l + ((maxWeight - minWeight) / numLoadLevels) / 2;



                        matrix[i, j, l] = VRPLTT.CalculateTravelTime(heightDiff, dist, loadLevelWeight, powerInput);
                        distributionMatrix[i, j, l] = CreateTravelTimeDistribution(loadLevelWeight, matrix[i, j, l]);
                    }
                }

            }); // (int i = 0; i < customers.Count; i++)

            //for (int i = 0; i < customers.Count; i++)
            //    for (int j = 0; j < customers.Count; j++)
            //        for (int l = 0; l < numLoadLevels; l++)
            //        {
            //            double loadLevelWeight = minWeight + ((maxWeight - minWeight) / numLoadLevels) * l + ((maxWeight - minWeight) / numLoadLevels) / 2;

            //            double dist;
            //            if (i < j)
            //                dist = distanceMatrix[i, j];
            //            else
            //                dist = distanceMatrix[j, i];

            //            matrix[i, j, l] = VRPLTT.CalculateTravelTime(customers[i].Elevation - customers[j].Elevation, dist, loadLevelWeight, powerInput);
            //        }


            Gamma gam = null;

            double longest = 0;

            using(StreamWriter w = new StreamWriter("data.txt"))

            for (int i = 0; i < customers.Count; i++)
                for (int j = 0; j < customers.Count; j++)
                    for (int l = 0; l < numLoadLevels; l++)
                    {
                            if (matrix[i, j, l] > longest)
                            {
                                longest = matrix[i, j, l];
                                gam = distributionMatrix[i, j, l];
                            }
                            w.WriteLine($"{matrix[i,j,l]};{distributionMatrix[i,j,l].Mean};{distributionMatrix[i, j, l].Mode}");
                    }
            Console.WriteLine($"{gam.Shape};{gam.Rate}");
        return (matrix,distributionMatrix);
        }

        public static (double[,] distances,List<Customer> customers) ParseVRPLTTInstance(string file)
        {
            List<Customer> customers = new List<Customer>();
            
            using(StreamReader sr = new StreamReader(file))
            {
                var len = sr.ReadLine().Split(',').Length - 8;

                double[,] distances = new double[len, len];
                string line = "";
                while((line = sr.ReadLine()) != null)
                {
                    var lineSplit = line.Split(',');
                    int id = int.Parse(lineSplit[0]);
                    double x = double.Parse(lineSplit[1], NumberStyles.Any, CultureInfo.InvariantCulture);
                    double y = double.Parse(lineSplit[2], NumberStyles.Any, CultureInfo.InvariantCulture);
                    double elevation = double.Parse(lineSplit[3], NumberStyles.Any, CultureInfo.InvariantCulture);
                    double demand, twstart, twend,serviceTime;
                    if (lineSplit[4] != "")
                        demand = double.Parse(lineSplit[4], NumberStyles.Any, CultureInfo.InvariantCulture);
                    else
                        demand = 0;
                    //double twstart = double.Parse(lineSplit[5]);
                    //double twend = double.Parse(lineSplit[6]);

                    if (lineSplit[5] != "")
                        twstart = double.Parse(lineSplit[5], NumberStyles.Any, CultureInfo.InvariantCulture);
                    else
                        twstart = 0;
                    if (lineSplit[6] != "")
                        twend = double.Parse(lineSplit[6], NumberStyles.Any, CultureInfo.InvariantCulture);
                    else
                        twend = 0;
                    if (lineSplit[7] != "")
                        serviceTime = double.Parse(lineSplit[7], NumberStyles.Any, CultureInfo.InvariantCulture);
                    else
                        serviceTime = 0;

                    //double serviceTime = double.Parse(lineSplit[7]);
                    var customer = new Customer(id,x,y,demand,twstart,twend,serviceTime,elevation);
                    for(int i = 8; i< lineSplit.Length; i++)
                    {
                        distances[id, i - 8] = double.Parse(lineSplit[i], NumberStyles.Any, CultureInfo.InvariantCulture);
                    }
                    customers.Add(customer);
                }
                return (distances,customers);
            }

        }


        public static double CalcRequiredForce(double v, double mass, double slope)
        {
            double Cd = 1.18;
            double A = 0.83;
            double Ro = 1.18;
            double Cr = 0.01;
            double g = 9.81;

            return ((Cd * A * Ro * Math.Pow(v, 2) / 2) + Cr * mass * g * Math.Cos(Math.Atan(slope)) + mass * g * Math.Sin(Math.Atan(slope))) * v / 0.95;
        }
    }
}
