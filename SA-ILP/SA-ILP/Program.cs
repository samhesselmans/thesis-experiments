// See https://aka.ms/new-console-template for more information
using SA_ILP;
using System.Diagnostics;
using Gurobi;


using System;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;

namespace MyApp // Note: actual namespace depends on the project name.
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var solver = new Solver();
            Stopwatch watch = new Stopwatch();

            watch.Start();
            //for(int i = 0; i < 10; i++)
            //solver.SolveInstance(@"C:\Users\samca\Documents\GitHub\thesis-experiments\solomon_instances\rc101.txt", numIterations: 80000000);
            await solver.SolveInstanceAsync(@"..\..\..\..\..\solomon_instances\rc103.txt", numThreads: 4, numIterations: 60000000);

        }


        async Task SolveAllAsync(string dir, string solDir, int numThreads = 4, int numIterations = 3000000)
        {
            if (!Directory.Exists(solDir))
                Directory.CreateDirectory(solDir);

            var solver = new Solver();
            Stopwatch watch = new Stopwatch();
            watch.Start();
            using (var totalWriter = new StreamWriter(Path.Join(solDir, "allSolutions.txt")))
            {


                foreach (var file in Directory.GetFiles(dir))
                {
                    (bool failed, List<RouteStore> ilpSol, double ilpVal, double ilpTime, double lsTime, double lsVal) = await solver.SolveInstanceAsync(file, numThreads: numThreads, numIterations: numIterations);
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
                    totalWriter.WriteLine($"Instance: {Path.GetFileName(file)}, Score: {Math.Round(ilpVal, 3)}, ilpTime: {Math.Round(ilpTime, 3)}, lsTime: {Math.Round(lsTime, 3)}, lsVal: {Math.Round(lsVal, 3)}, ilpImp: {Math.Round((lsVal - ilpVal) / lsVal * 100, 3)}%");
                    totalWriter.Flush();
                }
            }
        }
    }
}

//Console.WriteLine("Hello, World!");
//var comp = new ListEqCompare();
//HashSet<List<int>> Columns = new HashSet<List<int>>(comparer:comp );


//Columns.Add(new List<int> { 1, 2, 3 });
//Columns.Add(new List<int> { 1, 2, 3 });
//Console.Write(Columns.Count);
//122889
//118940
//114210

//46731

//133084
//55088


//await SolveAllAsync(@"../../../../../solomon_instances", Path.Join(@"../../../../../solutions/solomon_instances",DateTime.Now.ToString("dd-MM-yy_HH-mm-ss")),numThreads:6,numIterations:50000000);




