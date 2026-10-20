namespace Sight.Benchmarks;

public static class Program
{
    public static void Main(string[] args)
    {
        // Without arguments every benchmark runs; arguments are BenchmarkDotNet options (e.g. --filter *Graph*)
        if (args.Length == 0)
            BenchmarkRunner.Run(typeof(Program).Assembly);
        else
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
