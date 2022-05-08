namespace Sight.Benchmarks
{
    public static class Program
    {
        public static void Main()
        {
            BenchmarkRunner.Run(typeof(Program).Assembly);
        }
    }
}
