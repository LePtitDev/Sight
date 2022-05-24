using Sight.Logging.Loggers;

namespace Sight.Logging.Sample
{
    public static class Program
    {
        public static void Main()
        {
            var logger = new ConsoleLogger();
            logger.LogInformation("Hello World!");
            logger.LogWarning("This logger support warnings");
            logger.LogError("...and errors!");

            try
            {
                throw new InvalidOperationException("An unhandled exception");
            }
            catch (Exception ex)
            {
                logger.LogError(ex);
            }

            logger.LogDebug("Exit sample...");
        }
    }
}
