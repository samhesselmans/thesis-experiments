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

var solver = new Solver();
Stopwatch watch = new Stopwatch();
watch.Start();
for(int i = 0; i < 10; i++)
    solver.SolveInstance(@"C:\Users\samca\Documents\GitHub\thesis-experiments\solomon_1000\R1_10_1.TXT", numInterations: 3000000);

Console.Write(watch.ElapsedMilliseconds);