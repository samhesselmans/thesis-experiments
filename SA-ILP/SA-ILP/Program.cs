// See https://aka.ms/new-console-template for more information
using SA_ILP;
using System.Diagnostics;
using Gurobi;


string baseDir = "../../../../../";

var solver = new Solver();
Stopwatch watch = new Stopwatch();

//PROBLEM might not want to create new routes it seems like


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


//solver.SolveSolomonInstance(@"C:\Users\samca\Documents\GitHub\thesis-experiments\tsp_instances\lin318.tsp", numIterations: 50000000,timeLimit: 45 * 1000);
//await solver.SolveSolomonInstanceAsync(@"C:\Users\samca\Documents\GitHub\thesis-experiments\solomon_instances\c107.txt", numThreads:4, numIterations: 500000000,timeLimit:45 * 1000);

//await solver.DoTest(Path.Join(baseDir, "solomon_1000", "R1_10_1.TXT"), numIterations: 500000000, timeLimit: 45000);

//solver.SolveVRPLTTInstance(Path.Join(baseDir, "vrpltt_instances/large", "madrid_full.csv"), numLoadLevels: 150, numIterations: 50000000, timelimit: 45000,bikeMinMass:140,bikeMaxMass:290,inputPower:350);
await solver.SolveVRPLTTInstanceAsync(Path.Join(baseDir, "vrpltt_instances/large", "fukuoka_full.csv"), numLoadLevels: 150, numIterations: 500000000, timelimit: 60 * 1000, numThreads: 4, numStarts: 12, bikeMinMass: 140, bikeMaxMass: 290, inputPower: 350);


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
var skip = new List<String>();
//await SolveAllAsync(@"..\..\..\..\..\solomon_instances", Path.Join(@"..\..\..\..\..\solutions\solomon_instances",DateTime.Now.ToString("dd-MM-yy_HH-mm-ss")),skip,numThreads:4,numIterations:50000000);


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
        double res = await solver.SolveVRPLTTInstanceAsync(Path.Join(baseDir, "vrpltt_instances/large", "madrid_full.csv"), numLoadLevels: 10, numIterations: 50000000,timelimit:30000);
        if (res < best)
            best = res;
        if(res > worst)
            worst = res;
        total += res;
    }
    Console.WriteLine($"Finsihed all. Average score: {(total / num).ToString("0.000")}, best score: {best.ToString("0.000")}, worst score: {worst.ToString("0.000")}, Total time: {(watch.ElapsedMilliseconds / 1000).ToString("0.000")}");
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
