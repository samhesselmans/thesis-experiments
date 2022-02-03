// See https://aka.ms/new-console-template for more information
using SA_ILP;
using System.Diagnostics;

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
var solver = new Solver();
Stopwatch watch = new Stopwatch();

watch.Start();
//for(int i = 0; i < 10; i++)
solver.SolveInstance(@"C:\Users\samca\Documents\GitHub\thesis-experiments\solomon_1000\R1_10_1.TXT", numInterations: 3000000);


//double[,] solutions = new double[1000, 1000];
//double[] solutions2 = new double[1000000];
//solutions[300, 30] = 1;
//solutions2[300 * 1000 + 30] = 1;
//double total = 0;
//for (int i = 0; i < 100000000; i++)
//{
//    total += solutions[300, 30];
//    total += solutions2[300 * 1000 + 30];
//}
//Console.WriteLine(total);

//Console.WriteLine(watch.ElapsedMilliseconds);
//watch.Restart();
//total = 0;
//for (int i = 0; i < 100000000; i++)
//{
//    total += solutions2[300 * 1000 + 30];
//}

//Console.WriteLine(total);

//Console.WriteLine(watch.ElapsedMilliseconds);

//double Test()
//{
//    return solutions[0, 0];
//}