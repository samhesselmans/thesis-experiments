// See https://aka.ms/new-console-template for more information
using MathNet.Numerics.Distributions;
using System.Diagnostics;

var gamma = new Gamma(2.0, 1.5);
var random = new Random();
var toBenchGamma = () => { gamma.CumulativeDistribution(0.7); };
var toBenchGammaCreation = () => { var gamma = new Gamma(2.0, 1.5); gamma.CumulativeDistribution(0.7); };
var toBenchGammaPDF = () => { gamma.Density(0.7); };
var toBenchRandomNumber = () =>{ random.Next(100); };



var list = new List<Gamma>(100 * 100 * 150);

for(int i=0; i< 100 * 100 * 150; i++)
{
    list.Add(new Gamma(2.0, 1.5));
}

//Benchmark(toBenchGamma,"Gamma cummulative distribution");
//Benchmark(toBenchGammaPDF, "Gamma PDF ");
//Benchmark(toBenchRandomNumber, "Random number bench");
//Benchmark(toBenchGammaCreation, "Gamma creation");

Benchmark(()=>{ FunctionToTest2(); }, "Value tuple",1000000000);
Benchmark(() => { bool x; double y; FunctionToTest(out x,out y); }, "Out params", 1000000000);

void FunctionToTest(out bool x, out double y)
{
    x = true; y = 10;
}

(bool,double) FunctionToTest2()
{
    return (true, 10);
}

void Benchmark(Action toBenchmark,string label, int numTries = 10000000)
{
    Console.WriteLine($"Starting benchmarking {label}");
    var watch = new Stopwatch();
    watch.Start();
    for(int i = 0; i < numTries; i++)
    {
        toBenchmark();
    }
    watch.Stop();
    Console.WriteLine($"Finished benchmarking {label} in {watch.ElapsedMilliseconds} ms");
}