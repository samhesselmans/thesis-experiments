// See https://aka.ms/new-console-template for more information
using MathNet.Numerics.Distributions;
using System.Diagnostics;

var gamma = new Gamma(2.0, 1.5);
var random = new Random();
var toBenchGamma = () => { gamma.CumulativeDistribution(0.7); };
var toBenchGammaPDF = () => { gamma.Density(0.7); };
var toBenchRandomNumber = () =>{ random.Next(100); };
Benchmark(toBenchGamma,"Gamma cummulative distribution");
Benchmark(toBenchGammaPDF, "Gamma PDF ");
Benchmark(toBenchRandomNumber, "Random number bench");
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