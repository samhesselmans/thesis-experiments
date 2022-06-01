using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SA_ILP
{
    internal static class Tests
    {
        static Solver solver = new Solver();
        public static async Task RunVRPLTTTests(string dir, string solDir, int numRepeats, Options opts)
        {
            Console.WriteLine("Testing on all vrpltt instances");
            if (!Directory.Exists(solDir))
                Directory.CreateDirectory(solDir);

            using (var totalWriter = new StreamWriter(Path.Join(solDir, "allSolutionsVRPLTT.txt")))
            {
                totalWriter.WriteLine(opts.ToString());
                using (var csvWriter = new StreamWriter(Path.Join(solDir, "allSolutionsVRPLTT.csv")))
                {
                    csvWriter.WriteLine("SEP=;");
                    csvWriter.WriteLine("Instance;AllowWaiting;Score;N;ILP time;LS time;LS score;ILP imp(%)");
                    foreach (var file in Directory.GetFiles(dir))
                    {
                        Console.WriteLine($"Testing on { Path.GetFileNameWithoutExtension(file)}");
                        double totalValue = 0.0;
                        bool newInstance = false;
                        LocalSearchConfiguration config = LocalSearchConfigs.VRPLTT;
                        if (Path.GetExtension(file) != ".csv")
                            continue;

                        //string append = "Waiting allowed";
                        for (int i = 0; i < numRepeats; i++)
                        {
                            //if (i == numRepeats)
                            //{
                            //    totalWriter.WriteLine($"AVG: {totalValue / numRepeats}");
                            //    totalValue = 0;
                            //    config.AllowEarlyArrival = false;
                            //    config.PenalizeEarlyArrival = true;
                            //    //append = "Waiting not allowed";
                            //}
                            (bool failed, List<Route> ilpSol, double ilpVal, double ilpTime, double lsTime, double lsVal, string solutionJSON) = await solver.SolveVRPLTTInstanceAsync(file, numLoadLevels: opts.NumLoadLevels, numIterations: opts.Iterations, timelimit: opts.TimeLimitLS * 1000, bikeMinMass: opts.BikeMinWeight, bikeMaxMass: opts.BikeMaxWeight, inputPower: opts.BikePower, numStarts: opts.NumStarts, numThreads: opts.NumThreads, config: config);//solver.SolveSolomonInstanceAsync(file, numThreads: numThreads, numIterations: numIterations, timeLimit: 30 * 1000);
                            using (var writer = new StreamWriter(Path.Join(solDir, Path.GetFileNameWithoutExtension(file) + $"_{i}.txt")))
                            {
                                if (failed)
                                    writer.Write("FAIL did not meet check");
                                writer.WriteLine($"Score: {ilpVal}, ilpTime: {ilpTime}, lsTime: {lsTime}, Early arrival allowed {config.AllowEarlyArrival}");
                                foreach (var route in ilpSol)
                                {
                                    writer.WriteLine($"{route}");
                                }
                                writer.WriteLine(solutionJSON);
                            }
                            if (failed)
                                totalWriter.Write("FAIL did not meet check");


                            totalValue += ilpVal;

                            totalWriter.WriteLine($"Instance: { Path.GetFileNameWithoutExtension(file)},Early arrival allowed {config.AllowEarlyArrival}, Score: {Math.Round(ilpVal, 3)}, Vehicles: {ilpSol.Count}, ilpTime: {Math.Round(ilpTime, 3)}, lsTime: {Math.Round(lsTime, 3)}, lsVal: {Math.Round(lsVal, 3)}, ilpImp: {Math.Round((lsVal - ilpVal) / lsVal * 100, 3)}%");
                            csvWriter.WriteLine($"{ Path.GetFileNameWithoutExtension(file)};{config.AllowEarlyArrival};{Math.Round(ilpVal, 3)};{ilpSol.Count};{Math.Round(ilpTime, 3)};{Math.Round(lsTime, 3)};{Math.Round(lsVal, 3)};{Math.Round((lsVal - ilpVal) / lsVal * 100, 3)}");
                            totalWriter.Flush();
                            csvWriter.Flush();
                        }
                    }
                }
            }
        }

        public static async Task RunVRPLTTWindtests(string dir, string solDir, int numRepeats, Options opts)
        {
            Console.WriteLine("Testing with wind on all vrpltt instances");

            if (!Directory.Exists(solDir))
                Directory.CreateDirectory(solDir);

            using (var totalWriter = new StreamWriter(Path.Join(solDir, "allSolutionsVRPLTT.txt")))
            {
                totalWriter.WriteLine(opts.ToString());
                totalWriter.Flush();
                using (var csvWriter = new StreamWriter(Path.Join(solDir, "allSolutionsVRPLTT.csv")))
                {
                    csvWriter.WriteLine("SEP=;");
                    csvWriter.WriteLine("Instance;Windspeed;WindDirection;Score;N;ILP time;LS time;LS score;ILP imp;TimeCycledAgainstWind");

                    LocalSearchConfiguration config = LocalSearchConfigs.VRPLTTWithWind;

                    for (int test = 0; test < 5; test++)
                    {
                        if (test == 1)
                        {
                            config.WindDirection = new double[] { 0, -1 };
                        }
                        else if (test == 2)
                        {
                            config.WindDirection = new double[] { 1, 0 };
                        }
                        else if (test == 3)
                        {
                            config.WindDirection = new double[] { -1, 0 };
                        }
                        else if (test == 4)
                        {
                            //Testing the default vrpltt
                            config.WindSpeed = 0;
                        }

                        foreach (var file in Directory.GetFiles(dir))
                        {
                            for (int repeat = 0; repeat < numRepeats; repeat++)
                            {
                                //Running the test
                                (bool failed, List<Route> ilpSol, double ilpVal, double ilpTime, double lsTime, double lsVal, string solutionJSON) = await solver.SolveVRPLTTInstanceAsync(file, numLoadLevels: opts.NumLoadLevels, numIterations: opts.Iterations, timelimit: opts.TimeLimitLS * 1000, bikeMinMass: opts.BikeMinWeight, bikeMaxMass: opts.BikeMaxWeight, inputPower: opts.BikePower, numStarts: opts.NumStarts, numThreads: opts.NumThreads, config: config);//solver.SolveSolomonInstanceAsync(file, numThreads: numThreads, numIterations: numIterations, timeLimit: 30 * 1000);

                                var windResult = VRPLTT.CalculateWindCyclingTime(file, opts.BikeMinWeight, opts.BikeMaxWeight, opts.NumLoadLevels, opts.BikePower, config.WindDirection, ilpSol);


                                csvWriter.WriteLine($"{Path.GetFileNameWithoutExtension(file)};{config.WindSpeed};({config.WindDirection[0]},{config.WindDirection[1]});{ilpVal};{ilpTime};{lsTime};{lsVal};{(ilpVal - lsVal) / lsVal * 100};{windResult}");

                                using (var writer = new StreamWriter(Path.Join(solDir, Path.GetFileNameWithoutExtension(file) + $"_{test}_{repeat}.txt")))
                                {
                                    if (failed)
                                        writer.Write("FAIL did not meet check");
                                    writer.WriteLine($"Score: {ilpVal}, ilpTime: {ilpTime}, lsTime: {lsTime}, Early arrival allowed {config.AllowEarlyArrival}");
                                    foreach (var route in ilpSol)
                                    {
                                        writer.WriteLine($"{route}");
                                    }
                                }
                                if (failed)
                                    totalWriter.Write("FAIL did not meet check");
                            }
                        }
                    }

                }
            }

        }
        public static async Task RunVRPSLTTTests(string dir, string solDir, int numRepeats, Options opts)
        {
            Console.WriteLine("Testing on all vrpltt instances");
            if (!Directory.Exists(solDir))
                Directory.CreateDirectory(solDir);

            using (var totalWriter = new StreamWriter(Path.Join(solDir, "allSolutionsVRPLTT.txt")))
            {
                totalWriter.WriteLine(opts.ToString());
                using (var csvWriter = new StreamWriter(Path.Join(solDir, "allSolutionsVRPLTT.csv")))
                {
                    csvWriter.WriteLine("SEP=;");
                    csvWriter.WriteLine("Instance;Uncertanty penalty;UseMean;Score;N;ILP time;LS time;LS score;ILP imp(%);avg simulation distance;avg sim OTP; avg sim worst OTP;worst sim OTP;worst sim route;worst sim cust index;avg est OTP; avg est worst OTP;worst est OTP;worst est route;worst est cust index");
                    LocalSearchConfiguration config = LocalSearchConfigs.VRPSLTTWithoutWaiting;


                    for (int test = 0; test < 3; test++)
                    {

                        if (test == 0)
                        {

                            config.ExpectedEarlinessPenalty = 0;
                            config.ExpectedLatenessPenalty = 0;
                            config.UseMeanOfDistributionForScore = true;
                            config.UseMeanOfDistributionForTravelTime = true;


                        }
                        else if (test == 1)
                        {
                            config.ExpectedEarlinessPenalty = 0;
                            config.ExpectedLatenessPenalty = 0;
                            config.UseMeanOfDistributionForScore = false;
                            config.UseMeanOfDistributionForTravelTime = false;
                        }
                        else if (test == 2)
                        {
                            config.ExpectedEarlinessPenalty = 10;
                            config.ExpectedLatenessPenalty = 10;
                            config.UseMeanOfDistributionForScore = false;
                            config.UseMeanOfDistributionForTravelTime = false;
                        }
                        foreach (var file in Directory.GetFiles(dir))
                        {
                            Console.WriteLine($"Testing on { Path.GetFileNameWithoutExtension(file)}");
                            double totalValue = 0.0;
                            bool newInstance = false;
                            if (Path.GetExtension(file) != ".csv")
                                continue;






                            //string append = "Waiting allowed";
                            for (int r = 0; r < numRepeats; r++)
                            {

                                (bool failed, List<Route> ilpSol, double ilpVal, double ilpTime, double lsTime, double lsVal, string solutionJSON) = await solver.SolveVRPLTTInstanceAsync(file, numLoadLevels: opts.NumLoadLevels, numIterations: opts.Iterations, timelimit: opts.TimeLimitLS * 1000, bikeMinMass: opts.BikeMinWeight, bikeMaxMass: opts.BikeMaxWeight, inputPower: opts.BikePower, numStarts: opts.NumStarts, numThreads: opts.NumThreads, config: config);//solver.SolveSolomonInstanceAsync(file, numThreads: numThreads, numIterations: numIterations, timeLimit: 30 * 1000);

                                //List<Route> sol = ilpSol.ConvertAll(x=> new Route(custx))

                                double totalDist = 0;
                                double totalOntimePercentage = 0;
                                double averageWorstOntimePercentage = 0;
                                double worstOntimePercentage = double.MaxValue;
                                Route? worstRoute = null;
                                int worstRouteCustomer = -1;

                                double estimatedTotalOntimePercentage = 0;
                                double estimatedAverageWorstOntimePercentage = 0;
                                double estimatedWorstOntimePercentage = double.MaxValue;
                                Route? estimatedWorstRoute = null;
                                int estimatedWorstRouteCustomer = -1;


                                foreach (var route in ilpSol)
                                {
                                    if (route.route.Count != 2)
                                    {
                                        //Console.WriteLine($"{route}; ST {route.startTime} ; SST {route.route[1].TWStart - route.CustomerDist(route.route[0], route.route[1], route.used_capacity).Item1}");
                                        double avg = 0;
                                        int total = 0;
                                        double worst = double.MaxValue;
                                        Customer? worstCust = null;
                                        int worstIndex = -1;

                                        for (int i = 0; i < route.route.Count; i++)
                                        {

                                            if (route.customerDistributions[i] != null)
                                            {
                                                total += 1;

                                                double difUp = route.route[i].TWEnd - route.arrival_times[i];
                                                double difDown = route.route[i].TWStart - route.arrival_times[i];

                                                if (difUp < 0)
                                                    difUp = 0;
                                                if (difDown < 0)
                                                    difDown = 0;

                                                double pEarly = route.customerDistributions[i].CumulativeDistribution(difDown);

                                                double pOnTime = (route.customerDistributions[i].CumulativeDistribution(difUp));

                                                if (Double.IsNaN(pOnTime))
                                                    pOnTime = 1;

                                                if (((LocalSearchConfiguration)config).AllowEarlyArrival)
                                                    pEarly = 0;

                                                double p = pOnTime - pEarly;

                                                avg += p;
                                                if (p < worst)
                                                {
                                                    worstCust = route.route[i];
                                                    worst = p;
                                                    worstIndex = i;
                                                }
                                            }
                                        }
                                        estimatedTotalOntimePercentage += avg;
                                        estimatedAverageWorstOntimePercentage += worst;
                                        if (worst < estimatedWorstOntimePercentage)
                                        {
                                            estimatedWorstOntimePercentage = worst;
                                            estimatedWorstRoute = route;
                                            estimatedWorstRouteCustomer = worstIndex;
                                        }
                                        Console.WriteLine($"On time performance: {avg / total} worst: {worst} at {worstCust} at {worstIndex}");

                                        var res = route.Simulate(1000000);
                                        totalDist += res.AverageTravelTime;
                                        totalOntimePercentage += res.OnTimePercentage;

                                        int minIndex = -1;
                                        double min = double.MaxValue;
                                        for (int j = 0; j < res.CustomerOnTimePercentage.Length; j++)
                                        {
                                            if (res.CustomerOnTimePercentage[j] < min)
                                            {
                                                min = res.CustomerOnTimePercentage[j];
                                                minIndex = j;
                                            }

                                        }
                                        averageWorstOntimePercentage += min;
                                        if (min < worstOntimePercentage)
                                        {
                                            worstOntimePercentage = min;
                                            worstRoute = route;
                                            worstRouteCustomer = minIndex;
                                        }
                                        Console.WriteLine($"Simmulated on time perfomance: {res.OnTimePercentage} worst: {min} at {minIndex}\n");
                                    }

                                }




                                using (var writer = new StreamWriter(Path.Join(solDir, Path.GetFileNameWithoutExtension(file) + $"_{test}_{r}.txt")))
                                {
                                    if (failed)
                                        writer.Write("FAIL did not meet check");
                                    writer.WriteLine($"Score: {ilpVal}, ilpTime: {ilpTime}, lsTime: {lsTime}, Early arrival allowed {config.AllowEarlyArrival}");
                                    foreach (var route in ilpSol)
                                    {
                                        writer.WriteLine($"{route}");
                                    }
                                }
                                if (failed)
                                    totalWriter.Write("FAIL did not meet check");


                                totalValue += ilpVal;

                                totalWriter.WriteLine($"Instance: { Path.GetFileNameWithoutExtension(file)},Early arrival allowed {config.AllowEarlyArrival}, Score: {Math.Round(ilpVal, 3)}, Vehicles: {ilpSol.Count}, ilpTime: {Math.Round(ilpTime, 3)}, lsTime: {Math.Round(lsTime, 3)}, lsVal: {Math.Round(lsVal, 3)}, ilpImp: {Math.Round((lsVal - ilpVal) / lsVal * 100, 3)}%");
                                csvWriter.WriteLine($"{ Path.GetFileNameWithoutExtension(file)};{config.ExpectedEarlinessPenalty};{config.UseMeanOfDistributionForTravelTime};{Math.Round(ilpVal, 3)};{ilpSol.Count};{Math.Round(ilpTime, 3)};{Math.Round(lsTime, 3)};{Math.Round(lsVal, 3)};{Math.Round((lsVal - ilpVal) / lsVal * 100, 3)};{totalDist.ToString("0.000")};{totalOntimePercentage / ilpSol.Count};{averageWorstOntimePercentage / ilpSol.Count};{worstOntimePercentage};{worstRoute};{worstRouteCustomer};{estimatedTotalOntimePercentage / ilpSol.Count};{estimatedAverageWorstOntimePercentage / ilpSol.Count};{estimatedWorstOntimePercentage};{estimatedWorstRoute};{estimatedWorstRouteCustomer}");
                                totalWriter.Flush();
                                csvWriter.Flush();
                            }
                        }
                    }
                }
            }
        }




        public static async Task SolveAllAsync(string dir, string solDir, List<String> skip, Options opts)
        {
            if (!Directory.Exists(solDir))
                Directory.CreateDirectory(solDir);

            var solver = new Solver();
            Stopwatch watch = new Stopwatch();
            watch.Start();
            using (var totalWriter = new StreamWriter(Path.Join(solDir, "allSolutions.txt")))
            using (var csvWriter = new StreamWriter(Path.Join(solDir, "allSolutionsVRPLTT.csv")))
            {
                csvWriter.WriteLine("SEP=;");
                csvWriter.WriteLine("Instance;Score;N;ILP time;LS time;LS score;ILP imp(%)");

                string fileNameStart = "";
                double totalValue = 0.0;
                int total = 0;
                foreach (var file in Directory.GetFiles(dir).OrderBy(f => f))
                {
                    Console.WriteLine(file);
                    bool newInstance = false;
                    if (skip.Contains(Path.GetFileName(file)))
                        continue;

                    if (!Path.GetFileName(file).StartsWith(fileNameStart))
                    {
                        totalWriter.WriteLine($"AVERAGE {fileNameStart}: {totalValue / total}");
                        totalValue = 0;
                        total = 0;
                        fileNameStart = Path.GetFileName(file).Substring(0, 2);
                    }

                    (bool failed, List<RouteStore> ilpSol, double ilpVal, double ilpTime, double lsTime, double lsVal) = await solver.SolveSolomonInstanceAsync(file, numThreads: opts.NumThreads,numStarts:opts.NumStarts, numIterations: opts.Iterations, timeLimit: opts.TimeLimitLS * 1000);

                    csvWriter.WriteLine($"{Path.GetFileNameWithoutExtension(file)};{ilpVal};{ilpSol.Count};{ilpTime};{lsTime};{lsVal};{(ilpVal - lsVal) / lsVal * 100}");
                    
                    using (var writer = new StreamWriter(Path.Join(solDir, Path.GetFileName(file))))
                    {
                        if (failed)
                            writer.Write("FAIL did not meet check");
                        writer.WriteLine($"Score: {ilpVal}, ilpTime: {ilpTime}, lsTime: {lsTime}");
                        foreach (var route in ilpSol)
                        {
                            writer.WriteLine($"{route}");
                        }
                    }
                    if (failed)
                        totalWriter.Write("FAIL did not meet check");


                    totalValue += ilpVal;
                    total++;

                    totalWriter.WriteLine($"Instance: {Path.GetFileName(file)}, Score: {Math.Round(ilpVal, 3)}, Vehicles: {ilpSol.Count}, ilpTime: {Math.Round(ilpTime, 3)}, lsTime: {Math.Round(lsTime, 3)}, lsVal: {Math.Round(lsVal, 3)}, ilpImp: {Math.Round((lsVal - ilpVal) / lsVal * 100, 3)}%");
                    totalWriter.Flush();
                    csvWriter.Flush();
                }
            }




        }
    }
}
