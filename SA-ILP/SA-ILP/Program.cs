// See https://aka.ms/new-console-template for more information
using SA_ILP;
using System.Diagnostics;
using Gurobi;
using CommandLine;
using System.Text;
using MathNet.Numerics.Distributions;

string baseDir = "../../../../../";

var solver = new Solver();
Stopwatch watch = new Stopwatch();

//PROBLEM might not want to create new routes it seems like

//var arguments = Environment.GetCommandLineArgs();

if (args.Length >= 1)
{
    //string mode = args[0];
    //string instance = args[1];
    //int timeLimit = 60;
    Options? opts = null;

    Parser.Default.ParseArguments<Options>(args)
      .WithParsed<Options>(o =>
       {
           opts = o;
       });
    if (opts == null)
        return;

    if (opts.Mode == "vrpltt")
    {
        solver.SolveVRPLTTInstance(opts.Instance, numLoadLevels: opts.NumLoadLevels, numIterations: opts.Iterations, timelimit: opts.TimeLimitLS * 1000, bikeMinMass: opts.BikeMinWeight, bikeMaxMass: opts.BikeMaxWeight, inputPower: opts.BikePower);

    }
    else if (opts.Mode == "vrptw")
    {
        solver.SolveSolomonInstance(opts.Instance, numIterations: opts.Iterations, timeLimit: opts.TimeLimitLS * 1000);

    }
    else if (opts.Mode == "vrplttmt")
    {
        await solver.SolveVRPLTTInstanceAsync(opts.Instance, numLoadLevels: opts.NumLoadLevels, numIterations: opts.Iterations, timelimit: opts.TimeLimitLS * 1000, bikeMinMass: opts.BikeMinWeight, bikeMaxMass: opts.BikeMaxWeight, inputPower: opts.BikePower, numStarts: opts.NumStarts, numThreads: opts.NumThreads);
    }
    else if (opts.Mode == "vrptwmt")
    {
        await solver.SolveSolomonInstanceAsync(opts.Instance, numIterations: opts.Iterations, timeLimit: opts.TimeLimitLS * 1000, numStarts: opts.NumStarts, numThreads: opts.NumThreads);

    }
    else if(opts.Mode == "allvrpltt")
    {
        await RunVRPLTTTests(opts.Instance,Path.Join("Solutions",DateTime.Now.ToString("dd-MM-yy_HH-mm-ss")),5,opts);
    }
    else if (opts.Mode == "allvrpsltt")
    {
        await RunVRPSLTTTests(opts.Instance, Path.Join("Solutions", DateTime.Now.ToString("dd-MM-yy_HH-mm-ss")), 5, opts);
    }
    else
    {
        Console.WriteLine("Enter correct mode");
        return;
    }

    return;
}

//double[,,] test = new double[1000,1000,10];
//double[] test2 = new double[1000 * 1000 * 10];
//Random random = new Random();
//watch.Start();
//double total  = 0;
//for(int i =0; i< 10000000; i++)
//{
//    total += test[random.Next(1000),random.Next(1000),random.Next(10)];
//}
//Console.WriteLine(watch.ElapsedMilliseconds);
//watch.Restart();
//total = 0;
//for (int i = 0; i < 10000000; i++)
//{
//    total += test2[random.Next(1000)* random.Next(1000)* random.Next(10)];
//}
//Console.WriteLine(watch.ElapsedMilliseconds);

//var testarr = new List<int>() { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
//testarr.Reverse(3, 5);
//Console.WriteLine(testarr);
//for(int i = 0; i < 10; i++)
//Console.Write(10.00683123.ToString("0.000"));

//await RunTestAsync();


//solver.SolveSolomonInstance(@"C:\Users\samca\Documents\GitHub\thesis-experiments\solomon_1000\R1_10_1.TXT", numIterations: 50000000,timeLimit: 45 * 1000);
//await solver.SolveSolomonInstanceAsync(@"C:\Users\samca\Documents\GitHub\thesis-experiments\solomon_instances\c107.txt", numThreads:4, numIterations: 500000000,timeLimit:45 * 1000);

//await solver.DoTest(Path.Join(baseDir, "solomon_1000", "R1_10_1.TXT"), numIterations: 500000000, timeLimit: 45000);


//_extra_tiny_tw
solver.SolveVRPLTTInstance(Path.Join(baseDir, "vrpltt_instances/large", "sydney_full.csv"), numLoadLevels: 10, numIterations: 50000000, timelimit: 1000 * 1000,bikeMinMass:140,bikeMaxMass:290,inputPower:350,config:LocalSearchConfigs.VRPSLTT);
//await solver.SolveVRPLTTInstanceAsync(Path.Join(baseDir, "vrpltt_instances/large", "madrid_full.csv"), numLoadLevels: 10, numIterations: 500000000, timelimit: 1000 * 1000, numThreads: 4, numStarts: 16, bikeMinMass: 140, bikeMaxMass: 290, inputPower: 350);

//const LocalSearchConfiguration config = LocalSearchConfigs.VRPLTT;

//var result = VRPLTT.ParseVRPLTTInstance(Path.Join(baseDir, "vrpltt_instances/large", "madrid_full.csv"));
//Console.WriteLine(VRPLTT.CalculateTravelTime(10,10,800,350));
//var skip = new List<String>() {"c101.txt", "c102.txt", "c103.txt", "c104.txt", "c105.txt", "c106.txt", "c107.txt", "c108.txt", "c109.txt", "c201.txt", "c202.txt", "c203.txt", "c204.txt", "c205.txt", "c206.txt", "c207.txt", "c208.txt" };
//for( int i =1; i< 13; i++)
//{
//    string str = i.ToString();
//    if (i < 10)
//        str = "0" + str;
//    skip.Add($"r1{str}.txt");
//}

//for (int i = 1; i < 12; i++)
//{
//    string str = i.ToString();
//    if (i < 10)
//        str = "0" + str;
//    skip.Add($"r2{str}.txt");
//}
//var skip = new List<String>();
//await SolveAllAsync(@"..\..\..\..\..\solomon_instances", Path.Join(@"..\..\..\..\..\solutions\solomon_instances",DateTime.Now.ToString("dd-MM-yy_HH-mm-ss")),skip,numThreads:4,numIterations:50000000);


var gamma = new Gamma(2.0, 1.5);



async Task RunTestAsync()
{
    double total = 0;
    double best = double.MaxValue;
    double worst = double.MinValue;
    int num = 10;
    watch.Start();
    //LAPTOP
    //Zonder reverse operator sydney:                 Average score: 145,097, best score: 139,5727, Total time: 324,000
    //Met reverse operator sydney:                    Average score: 143,894, best score: 141,1361, Total time: 326,000
    //Met nieuwe move operator sydney:  Finsihed all. Average score: 143,777, best score: 140,0621, Total time: 325,000
    //Met 4 threads, zelfde timelimit:  Finsihed all. Average score: 143,849, best score: 140,196, worst score: 158,264, Total time: 345,000
    //Met 4 threads, 45s timelimit:     Finsihed all. Average score: 142,300, best score: 139,281, worst score: 144,666, Total time: 505,000

    //avg 600

    //Madrid full
    //Met greedy move op                Average score: 586,591, best score: 548,0854, Total time: 325,000


    //DESKTOP
    //Seattle full
    //Zonder greedy move op             Finsihed all. Average score: 459,158, best score: 400,1997, Total time: 341,000 WAARSCHJNLIJK NIET VALID
    //Met greedy move op                Finsihed all. Average score: 398,136, best score: 394,7354, Total time: 340,000 WAARSCHJNLIJK NIET VALID
    //Met greedy move op (less chance)  Finsihed all. Average score: 397,662, best score: 393,0073, Total time: 340,000 WAARSCHJNLIJK NIET VALID



    for (int i = 0; i < num; i++)
    {
        (bool failed, _, double ilpVal, double ilpTime, double lsTime, double lsVal) = await solver.SolveVRPLTTInstanceAsync(Path.Join(baseDir, "vrpltt_instances/large", "madrid_full.csv"), numLoadLevels: 10, numIterations: 50000000,timelimit:30000);
        if (ilpVal < best)
            best = ilpVal;
        if(ilpVal > worst)
            worst = ilpVal;
        total += ilpVal;
    }
    Console.WriteLine($"Finsihed all. Average score: {(total / num).ToString("0.000")}, best score: {best.ToString("0.000")}, worst score: {worst.ToString("0.000")}, Total time: {(watch.ElapsedMilliseconds / 1000).ToString("0.000")}");
}

async Task RunVRPLTTTests(string dir, string solDir, int numRepeats, Options opts)
{
    Console.WriteLine("Testing on all vrpltt instances");
    if (!Directory.Exists(solDir))
        Directory.CreateDirectory(solDir);

    using (var totalWriter = new StreamWriter(Path.Join(solDir, "allSolutionsVRPLTT.txt")))
    {
        totalWriter.WriteLine(opts.ToString());
        using(var csvWriter = new StreamWriter(Path.Join(solDir, "allSolutionsVRPLTT.csv"))) {
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
                for (int i = 0; i < numRepeats ; i++)
                {
                    //if (i == numRepeats)
                    //{
                    //    totalWriter.WriteLine($"AVG: {totalValue / numRepeats}");
                    //    totalValue = 0;
                    //    config.AllowEarlyArrival = false;
                    //    config.PenalizeEarlyArrival = true;
                    //    //append = "Waiting not allowed";
                    //}
                    (bool failed, List<Route> ilpSol, double ilpVal, double ilpTime, double lsTime, double lsVal) = await solver.SolveVRPLTTInstanceAsync(file, numLoadLevels: opts.NumLoadLevels, numIterations: opts.Iterations, timelimit: opts.TimeLimitLS * 1000, bikeMinMass: opts.BikeMinWeight, bikeMaxMass: opts.BikeMaxWeight, inputPower: opts.BikePower, numStarts: opts.NumStarts, numThreads: opts.NumThreads,config:config);//solver.SolveSolomonInstanceAsync(file, numThreads: numThreads, numIterations: numIterations, timeLimit: 30 * 1000);
                    using (var writer = new StreamWriter(Path.Join(solDir, Path.GetFileNameWithoutExtension(file) + $"_{i}.txt")))
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
                    csvWriter.WriteLine($"{ Path.GetFileNameWithoutExtension(file)};{config.AllowEarlyArrival};{Math.Round(ilpVal, 3)};{ilpSol.Count};{Math.Round(ilpTime, 3)};{Math.Round(lsTime, 3)};{Math.Round(lsVal, 3)};{Math.Round((lsVal - ilpVal) / lsVal * 100, 3)}");
                    totalWriter.Flush();
                    csvWriter.Flush();
                }
            }
        }
    }
}


async Task RunVRPSLTTTests(string dir, string solDir, int numRepeats, Options opts)
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
            LocalSearchConfiguration config = LocalSearchConfigs.VRPSLTT;


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

                        (bool failed, List<Route> ilpSol, double ilpVal, double ilpTime, double lsTime, double lsVal) = await solver.SolveVRPLTTInstanceAsync(file, numLoadLevels: opts.NumLoadLevels, numIterations: opts.Iterations, timelimit: opts.TimeLimitLS * 1000, bikeMinMass: opts.BikeMinWeight, bikeMaxMass: opts.BikeMaxWeight, inputPower: opts.BikePower, numStarts: opts.NumStarts, numThreads: opts.NumThreads, config: config);//solver.SolveSolomonInstanceAsync(file, numThreads: numThreads, numIterations: numIterations, timeLimit: 30 * 1000);

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




async Task SolveAllAsync(string dir,string solDir,List<String> skip, int numThreads = 4, int numIterations = 3000000)
{
    if(!Directory.Exists(solDir))
        Directory.CreateDirectory(solDir);

    var solver = new Solver();
    Stopwatch watch = new Stopwatch();
    watch.Start();
    using (var totalWriter = new StreamWriter(Path.Join(solDir, "allSolutions.txt")))
    {

        string fileNameStart = "";
        double totalValue = 0.0;
        int total = 0;
        foreach (var file in Directory.GetFiles(dir))
        {
            bool newInstance = false;
            if (skip.Contains(Path.GetFileName(file)))
                continue;

            if (!Path.GetFileName(file).StartsWith(fileNameStart))
            {
                totalWriter.WriteLine($"AVERAGE {fileNameStart}: {totalValue/total}");
                totalValue = 0;
                total = 0;
                fileNameStart = Path.GetFileName(file).Substring(0,2);
            }

                (bool failed, List<RouteStore> ilpSol, double ilpVal, double ilpTime, double lsTime,double lsVal) = await solver.SolveSolomonInstanceAsync(file, numThreads: numThreads, numIterations: numIterations,timeLimit:30*1000);
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

            totalWriter.WriteLine($"Instance: {Path.GetFileName(file)}, Score: {Math.Round(ilpVal,3)}, Vehicles: {ilpSol.Count}, ilpTime: {Math.Round(ilpTime,3)}, lsTime: {Math.Round(lsTime,3)}, lsVal: {Math.Round(lsVal,3)}, ilpImp: {Math.Round((lsVal-ilpVal)/lsVal * 100,3)}%");
            totalWriter.Flush();
        }
    }




}

class Options
{
    //[Option('r', "read", Required = true, HelpText = "Input files to be processed.")]
    //public IEnumerable<string> InputFiles { get; set; }

    //// Omitting long name, defaults to name of property, ie "--verbose"
    //[Option(
    //  Default = false,
    //  HelpText = "Prints all messages to standard output.")]
    //public bool Verbose { get; set; }

    //[Option("stdin",
    //  Default = false,
    //  HelpText = "Read from stdin")]
    //public bool stdin { get; set; }

    //[Value(0, MetaName = "offset", HelpText = "File offset.")]
    //public long? Offset { get; set; }

    [Value(0, MetaName = "mode", HelpText = "Program mode.\n\tOptions:\n\t\t- vrpltt\n\t\t- vrptw\n\t\t- vrplttmt\n\t\t- vrptwmt\n\t\t- allvrpltt")]
    public string Mode { get; set; }

    [Value(1,MetaName = "instance", HelpText ="Path to instance")]
    public string Instance { get; set; }

    [Option("iterations", Default = 50000000, HelpText = "LS timelimit.")]
    public int Iterations { get; set; }

    [Option("threads", Default = 4, HelpText = "Number of threads in multithreaded modes.")]
    public int NumThreads { get; set; }

    [Option("starts", Default = 4, HelpText = "Number of starts in multithreaded modes.")]
    public int NumStarts { get; set; }

    [Option("lstl", Default =60, HelpText = "LS timelimit.")]
    public int TimeLimitLS { get; set; }

    [Option("ilptl", Default = 3600, HelpText = "ILP timelimit.")]
    public int TimeLimitILP { get; set; }

    [Option("power", Default = 350, HelpText = "Input power.")]
    public int BikePower { get; set; }
    [Option("minweight", Default = 140, HelpText = "Min bikeweight in vrpltt.")]
    public int BikeMinWeight { get; set; }

    [Option("maxweight", Default = 290, HelpText = "Max bikeweight in vrpltt.")]
    public int BikeMaxWeight { get; set; }

    [Option("loadlevels", Default = 150, HelpText = "Number of loadlevels in vrpltt.")]
    public int NumLoadLevels { get; set; }


    public override string ToString()
    {
        return GetType().GetProperties()
    .Select(info => (info.Name, Value: info.GetValue(this, null) ?? "(null)"))
    .Aggregate(
        new StringBuilder(),
        (sb, pair) => sb.AppendLine($"{pair.Name}: {pair.Value}"),
        sb => sb.ToString());
    }
}
