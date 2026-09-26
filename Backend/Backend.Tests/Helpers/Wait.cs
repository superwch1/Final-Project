using System.Diagnostics;

namespace Backend.Tests.Helpers
{
    public static class Wait
    {
        /// <summary>
        /// Waits until the condition is true, failing the test if it takes longer than the timeout
        /// </summary>
        public static async Task UntilAsync(Func<bool> condition, int timeoutMilliseconds = 2000)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            while (!condition())
            {
                if (stopwatch.ElapsedMilliseconds > timeoutMilliseconds)
                {
                    throw new TimeoutException("The condition was not met in time.");
                }

                await Task.Delay(10);
            }
        }
    }
}
