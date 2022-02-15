// See https://aka.ms/new-console-template for more information
using SA_ILP;
using System.Diagnostics;
using Gurobi;


string baseDir = "../../../../../";

var solver = new Solver();
Stopwatch watch = new Stopwatch();




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




solver.SolveSolomonInstance(@"C:\Users\samca\Documents\GitHub\thesis-experiments\solomon_instances\r104.txt", numIterations: 50000000);
//await solver.SolveSolomonInstanceAsync(@"C:\Users\samca\Documents\GitHub\thesis-experiments\solomon_1000\R1_10_10.TXT", numThreads:4, numIterations: 50000000);


//var result = VRPLTT.ParseVRPLTTInstance(Path.Join(baseDir, "vrpltt_instances/large", "madrid_full.csv"));
//Console.WriteLine(VRPLTT.CalculateTravelTime(10,10,800,350));
//await SolveAllAsync(@"..\..\..\..\..\solomon_instances", Path.Join(@"..\..\..\..\..\solutions\solomon_instances",DateTime.Now.ToString("dd-MM-yy_HH-mm-ss")),numThreads:1);


void RunTest()
{
    double total = 0;
    double best = double.MaxValue;
    int num = 10;
    watch.Start();
    //Zonder reverse operator sydney: Average score: 145,097, best score: 139,5727, Total time: 324,000
    //Met reverse operator sydney: Average score: 143,894, best score: 141,1361, Total time: 326,000 

    for (int i = 0; i < num; i++)
    {
        double res = solver.SolveVRPLTTInstance(Path.Join(baseDir, "vrpltt_instances/large", "madrid_full.csv"), numLoadLevels: 10, numIterations: 50000000);
        if (res < best)
            best = res;
        total += res;
    }
    Console.WriteLine($"Finsihed all. Average score: {(total / num).ToString("0.000")}, best score: {best.ToString("0.0000")}, Total time: {(watch.ElapsedMilliseconds / 1000).ToString("0.000")}");
}

async Task SolveAllAsync(string dir,string solDir, int numThreads = 4, int numIterations = 3000000)
{
    if(!Directory.Exists(solDir))
        Directory.CreateDirectory(solDir);

    var solver = new Solver();
    Stopwatch watch = new Stopwatch();
    watch.Start();
    using (var totalWriter = new StreamWriter(Path.Join(solDir, "allSolutions.txt")))
    {


        foreach (var file in Directory.GetFiles(dir))
        {
            (bool failed, List<RouteStore> ilpSol, double ilpVal, double ilpTime, double lsTime,double lsVal) = await solver.SolveSolomonInstanceAsync(file, numThreads: numThreads, numIterations: numIterations);
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
            totalWriter.WriteLine($"Instance: {Path.GetFileName(file)}, Score: {Math.Round(ilpVal,3)}, ilpTime: {Math.Round(ilpTime,3)}, lsTime: {Math.Round(lsTime,3)}, lsVal: {Math.Round(lsVal,3)}, ilpImp: {Math.Round((lsVal-ilpVal)/lsVal * 100,3)}%");
            totalWriter.Flush();
        }
    }
}