// See https://aka.ms/new-console-template for more information
using SA_ILP;
using System.Diagnostics;
using Gurobi;


string baseDir = "../../../../../";

var solver = new Solver();
Stopwatch watch = new Stopwatch();

watch.Start();
//for(int i = 0; i < 10; i++)
solver.SolveSolomonInstance(@"C:\Users\samca\Documents\GitHub\thesis-experiments\solomon_instances\c101.txt", numIterations: 80000000);
//await solver.SolveSolomonInstanceAsync(@"..\..\..\..\..\solomon_instances\rc103.txt",numThreads:1, numIterations: 60000000);


//var result = VRPLTT.ParseVRPLTTInstance(Path.Join(baseDir, "vrpltt_instances/large", "madrid_full.csv"));
//Console.WriteLine(VRPLTT.CalculateTravelTime(10,10,800,350));
//await SolveAllAsync(@"..\..\..\..\..\solomon_instances", Path.Join(@"..\..\..\..\..\solutions\solomon_instances",DateTime.Now.ToString("dd-MM-yy_HH-mm-ss")),numThreads:1);



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