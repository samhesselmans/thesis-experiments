// See https://aka.ms/new-console-template for more information
using SA_ILP;

//Console.WriteLine("Hello, World!");
//var comp = new ListEqCompare();
//HashSet<List<int>> Columns = new HashSet<List<int>>(comparer:comp );


//Columns.Add(new List<int> { 1, 2, 3 });
//Columns.Add(new List<int> { 1, 2, 3 });
//Console.Write(Columns.Count);


var solver = new Solver();

solver.SolveInstance(@"C:\Users\samca\Documents\GitHub\thesis-experiments\solomon_instances\r101.txt");