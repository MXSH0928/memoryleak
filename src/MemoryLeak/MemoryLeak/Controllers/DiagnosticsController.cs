namespace MemoryLeak.Controllers
{
    using System;
    using System.Diagnostics;
    using System.Threading;

    using Microsoft.AspNetCore.Mvc;

    /// <summary>
    /// The diagnostics controller.
    /// </summary>
    [Route("api")]
    [ApiController]
    public class DiagnosticsController : ControllerBase
    {
        /// <summary>
        /// The _process.
        /// </summary>
        private static readonly Process Process = Process.GetCurrentProcess();

        /// <summary>
        /// The refresh rate.
        /// </summary>
        private static readonly double RefreshRate = TimeSpan.FromSeconds(1).TotalMilliseconds;

        /// <summary>
        /// The requests.
        /// </summary>
        public static long Requests = 0;

        /// <summary>
        /// The old CPU time.
        /// </summary>
        private static TimeSpan oldCpuTime = TimeSpan.Zero;

        /// <summary>
        /// The last monitor time.
        /// </summary>
        private static DateTime lastMonitorTime = DateTime.UtcNow;

        /// <summary>
        /// The last RPS time.
        /// </summary>
        private static DateTime lastRpsTime = DateTime.UtcNow;

        /// <summary>
        /// The CPU
        /// </summary>
        private static double cpu = 0;

        /// <summary>
        /// The RPS.
        /// </summary>
        private static double rps = 0;
        
        /// <summary>
        /// The get collect.
        /// </summary>
        /// <returns>
        /// The <see cref="ActionResult"/>.
        /// </returns>
        [HttpGet("collect")]
        public ActionResult GetCollect()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            return this.Ok();
        }

        /// <summary>
        /// The get diagnostics.
        /// </summary>
        /// <returns>
        /// The <see cref="ActionResult"/>.
        /// </returns>
        [HttpGet("diagnostics")]
        public ActionResult GetDiagnostics()
        {
            var now = DateTime.UtcNow;
            Process.Refresh();

            var cpuElapsedTime = now.Subtract(lastMonitorTime).TotalMilliseconds;

            if (cpuElapsedTime > RefreshRate)
            {
                var newCpuTime = Process.TotalProcessorTime;
                var elapsedCpu = (newCpuTime - oldCpuTime).TotalMilliseconds;
                cpu = elapsedCpu * 100 / Environment.ProcessorCount / cpuElapsedTime;

                lastMonitorTime = now;
                oldCpuTime = newCpuTime;
            }

            var rpsElapsedTime = now.Subtract(lastRpsTime).TotalMilliseconds;
            if (rpsElapsedTime > RefreshRate)
            {
                rps = requests * 1000 / rpsElapsedTime;
                Interlocked.Exchange(ref requests, 0);
                lastRpsTime = now;
            }

            var diagnostics = new
            {
                PID = Process.Id,

                // The memory occupied by objects.
                Allocated = GC.GetTotalMemory(false),

                // The working set includes both shared and private data. The shared data includes the pages that contain all the 
                // instructions that the process executes, including instructions in the process modules and the system libraries.
                WorkingSet = Process.WorkingSet64,

                // The value returned by this property represents the current size of memory used by the process, in bytes, that 
                // cannot be shared with other processes.
                PrivateBytes = Process.PrivateMemorySize64,

                // The number of generation 0 collections
                Gen0 = GC.CollectionCount(0),

                // The number of generation 1 collections
                Gen1 = GC.CollectionCount(1),

                // The number of generation 2 collections
                Gen2 = GC.CollectionCount(2),

                CPU = cpu,

                RPS = rps
            };

            return new ObjectResult(diagnostics);
        }
    }
}
