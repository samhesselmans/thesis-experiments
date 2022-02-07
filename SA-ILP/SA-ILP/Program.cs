// See https://aka.ms/new-console-template for more information
using SA_ILP;
using System.Diagnostics;
using Gurobi;
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
//solver.SolveInstance(@"C:\Users\samca\Documents\GitHub\thesis-experiments\solomon_instances\rc101.txt", numIterations: 80000000);
//await solver.SolveInstanceAsync(@"..\..\..\..\..\solomon_instances\rc103.txt",numThreads:4, numIterations: 60000000);


await SolveAllAsync(@"..\..\..\..\..\solomon_instances", Path.Join(@"..\..\..\..\..\solutions\solomon_instances",DateTime.Now.ToString("dd-MM-yy_HH-mm-ss")),numThreads:1);


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
            (bool failed, List<RouteStore> ilpSol, double ilpVal, double ilpTime, double lsTime,double lsVal) = await solver.SolveInstanceAsync(file, numThreads: numThreads, numIterations: numIterations);
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

//GRBEnv env = new GRBEnv();
//GRBModel model = new GRBModel(env);

//string[] Categories =
//    new string[] { "calories", "protein", "fat", "sodium" };
//int nCategories = Categories.Length;
//double[] minNutrition = new double[] { 1800, 91, 0, 0 };
//double[] maxNutrition = new double[] { 2200, GRB.INFINITY, 65, 1779 };

//// Set of foods
//string[] Foods =
//    new string[] { "hamburger", "chicken", "hot dog", "fries",
//              "macaroni", "pizza", "salad", "milk", "ice cream" };
//int nFoods = Foods.Length;
//double[] cost =
//    new double[] { 2.49, 2.89, 1.50, 1.89, 2.09, 1.99, 2.49, 0.89,
//              1.59 };

//// Nutrition values for the foods
//double[,] nutritionValues = new double[,] {
//          { 410, 24, 26, 730 },   // hamburger
//          { 420, 32, 10, 1190 },  // chicken
//          { 560, 20, 32, 1800 },  // hot dog
//          { 380, 4, 19, 270 },    // fries
//          { 320, 12, 10, 930 },   // macaroni
//          { 320, 15, 12, 820 },   // pizza
//          { 320, 31, 12, 1230 },  // salad
//          { 100, 8, 2.5, 125 },   // milk
//          { 330, 8, 10, 180 }     // ice cream
//          };

//// Model
//GRBEnv env = new GRBEnv();
//GRBModel model = new GRBModel(env);

//model.ModelName = "diet";

//// Create decision variables for the nutrition information,
//// which we limit via bounds
//GRBVar[] nutrition = new GRBVar[nCategories];
//for (int i = 0; i < nCategories; ++i)
//{
//    nutrition[i] =
//        model.AddVar(minNutrition[i], maxNutrition[i], 0, GRB.CONTINUOUS,
//                     Categories[i]);
//}

//// Create decision variables for the foods to buy
////
//// Note: For each decision variable we add the objective coefficient
////       with the creation of the variable.
//GRBVar[] buy = new GRBVar[nFoods];
//for (int j = 0; j < nFoods; ++j)
//{
//    buy[j] =
//        model.AddVar(0, GRB.INFINITY, cost[j], GRB.CONTINUOUS, Foods[j]);
//}

//// The objective is to minimize the costs
////
//// Note: The objective coefficients are set during the creation of
////       the decision variables above.
//model.ModelSense = GRB.MINIMIZE;

//// Nutrition constraints
//for (int i = 0; i < nCategories; ++i)
//{
//    GRBLinExpr ntot = 0.0;
//    for (int j = 0; j < nFoods; ++j)
//        ntot.AddTerm(nutritionValues[j, i], buy[j]);
//    model.AddConstr(ntot == nutrition[i], Categories[i]);
//}

//// Solve
//model.Optimize();
//PrintSolution(model, buy, nutrition);

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

static void PrintSolution(GRBModel model, GRBVar[] buy,
                                  GRBVar[] nutrition)
{
    if (model.Status == GRB.Status.OPTIMAL)
    {
        Console.WriteLine("\nCost: " + model.ObjVal);
        Console.WriteLine("\nBuy:");
        for (int j = 0; j < buy.Length; ++j)
        {
            if (buy[j].X > 0.0001)
            {
                Console.WriteLine(buy[j].VarName + " " + buy[j].X);
            }
        }
        Console.WriteLine("\nNutrition:");
        for (int i = 0; i < nutrition.Length; ++i)
        {
            Console.WriteLine(nutrition[i].VarName + " " + nutrition[i].X);
        }
    }
    else
    {
        Console.WriteLine("No solution");
    }
}
