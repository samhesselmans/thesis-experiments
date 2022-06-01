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


    String solutionDir = Path.Join("Solutions", DateTime.Now.ToString("dd-MM-yy_HH-mm-ss") + opts.TestName);

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
    else if (opts.Mode == "allvrpltt")
    {
        await Tests.RunVRPLTTTests(opts.Instance, solutionDir, opts.NumRepeats, opts);
    }
    else if (opts.Mode == "allvrpsltt")
    {
        await Tests.RunVRPSLTTTests(opts.Instance, solutionDir, opts.NumRepeats, opts);
    }
    else if (opts.Mode == "allvrplttWind")
    {
        await Tests.RunVRPLTTWindtests(opts.Instance, solutionDir, opts.NumRepeats, opts);
    }
    else if( opts.Mode == "allvrp")
    {
        var skip = new List<string>();
        await Tests.SolveAllAsync(opts.Instance,solutionDir,skip,opts);
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

//solver.SolveVRPLTTInstance(Path.Join(baseDir, "vrpltt_instances/large", "madrid_full.csv"), numLoadLevels: 10, numIterations: 50000000, timelimit: 500 * 1000,bikeMinMass:140,bikeMaxMass:290,inputPower:350);
//await solver.SolveVRPLTTInstanceAsync(Path.Join(baseDir, "vrpltt_instances/large", "madrid_full.csv"), numLoadLevels: 10, numIterations: 500000000, timelimit: 1 * 1000, numThreads: 4, numStarts: 16, bikeMinMass: 140, bikeMaxMass: 290, inputPower: 350);

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
        (bool failed, List<Route> ilpSol, double ilpVal, double ilpTime, double lsTime, double lsVal,string solutionJSON) = await solver.SolveVRPLTTInstanceAsync(Path.Join(baseDir, "vrpltt_instances/large", "madrid_full.csv"), numLoadLevels: 10, numIterations: 50000000,timelimit:30000);
        if (ilpVal < best)
            best = ilpVal;
        if (ilpVal > worst)
            worst = ilpVal;
        total += ilpVal;
    }
    Console.WriteLine($"Finsihed all. Average score: {(total / num).ToString("0.000")}, best score: {best.ToString("0.000")}, worst score: {worst.ToString("0.000")}, Total time: {(watch.ElapsedMilliseconds / 1000).ToString("0.000")}");
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

    [Value(0, MetaName = "mode", HelpText = "Program mode.\n\tOptions:\n\t\t- vrpltt\n\t\t- vrptw\n\t\t- vrplttmt\n\t\t- vrptwmt\n\t\t- allvrpltt\n\t\t- allvrpsltt\n\t\t- allvrplttWind")]
    public string Mode { get; set; }

    [Value(1, MetaName = "instance", HelpText = "Path to instance")]
    public string Instance { get; set; }

    [Option("iterations", Default = 50000000, HelpText = "LS timelimit.")]
    public int Iterations { get; set; }

    [Option("threads", Default = 4, HelpText = "Number of threads in multithreaded modes.")]
    public int NumThreads { get; set; }

    [Option("starts", Default = 4, HelpText = "Number of starts in multithreaded modes.")]
    public int NumStarts { get; set; }

    [Option("lstl", Default = 60, HelpText = "LS timelimit.")]
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

    [Option("repeats", Default = 5, HelpText = "Number of reptitions per instance for the selected tests. This only works for the collection of tests")]
    public int NumRepeats { get; set; }

    [Option("testname", Default = "", HelpText = "Name of the test currently ran")]
    public string TestName { get; set; }
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
