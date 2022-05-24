// See https://aka.ms/new-console-template for more information
using MathNet.Numerics.Distributions;
using System.Diagnostics;
using System;

namespace MyApp // Note: actual namespace depends on the project name.
{
    internal class Program
    {
        static List<int> testList = new List<int>(100000000);
        static void Main(string[] args)
        {
            var gamma = new Gamma(2.0, 1.5);
            var random = new Random();
            var toBenchGamma = () => { gamma.CumulativeDistribution(0.7); };
            var toBenchGammaCreation = () => { var gamma = new Gamma(2.0, 1.5); gamma.CumulativeDistribution(0.7); };
            var toBenchGammaPDF = () => { gamma.Density(0.7); };
            var toBenchRandomNumber = () => { random.Next(100); };

            
            for (int i = 0; i < testList.Capacity; i++)
                testList.Add(random.Next());



            //var list = new List<Gamma>(100 * 100 * 150);

            //for(int i=0; i< 100 * 100 * 150; i++)
            //{
            //    list.Add(new Gamma(2.0, 1.5));
            //}

            //Benchmark(toBenchGamma,"Gamma cummulative distribution");
            //Benchmark(toBenchGammaPDF, "Gamma PDF ");
            //Benchmark(toBenchRandomNumber, "Random number bench");
            //Benchmark(toBenchGammaCreation, "Gamma creation");

            //Benchmark(() => { testList.Add(random.Next()); }, "1", 1000000);

            //Benchmark(() => { testList.Add(random.Next()); testList.Add(random.Next()); testList.Add(random.Next()); }, "3", 1000000);
            //Benchmark(() => { testList.Add(random.Next()); testList.Add(random.Next()); testList.Add(random.Next()); testList.Add(random.Next()); testList.Add(random.Next()); testList.Add(random.Next()); }, "6", 1000000);

            Benchmark(() => { Console.WriteLine(testHeap()); }, "heap", 1);
            Benchmark(() => { Console.WriteLine(testStack(testList)); }, "stack", 1);
            Benchmark(() => { Console.WriteLine(testHeap()); }, "heap", 1);
            Benchmark(() => { Console.WriteLine(testStack(testList)); }, "stack", 1);
            //Benchmark(() => { FunctionToTest2(); }, "Value tuple", 1799051249);
            //Benchmark(() => { bool x; double y; FunctionToTest(out x, out y); }, "Out params", 1799051249);
            //Benchmark(() => { TestFunction1(140, 150, 10);}, "llcalc", 1799051249); 
            //Benchmark(() => { TestFunction1(140, 150, 10); bool x; double y; FunctionToTest(out x, out y); FunctionToTest2(); }, "All", 1799051249);


        }
        static void FunctionToTest(out bool x, out double y)
        {
            x = true; y = 10;
        }

        (bool, double) FunctionToTest2()
        {
            return (true, 10);
        }


        static long testHeap()
        {
            long total = 0;
            for (int i = 0; i < testList.Count; i++)
                total += testList[i];
            return total;
        }

        static long testStack(List<int> lijst)
        {
            long total = 0;
            for (int i = 0; i < lijst.Count; i++)
                total += lijst[i];
            return total;
        }

        static int TestFunction1(double weight, double max_capacity, int numLoadLevels)
        {
            return (int)((Math.Max(0, weight - 0.000001) / max_capacity) * numLoadLevels);
        }

        //double TestFunction2()
        //{
        //    return 12;
        //}

        static void Benchmark(Action toBenchmark, string label, long numTries = 10000000)
        {
            Console.WriteLine($"Starting benchmarking {label}");
            var watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < numTries; i++)
            {
                toBenchmark();
            }
            watch.Stop();
            Console.WriteLine($"Finished benchmarking {label} in {watch.ElapsedMilliseconds} ms");
        }
    }
}



