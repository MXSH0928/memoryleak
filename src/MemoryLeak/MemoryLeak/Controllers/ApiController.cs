namespace MemoryLeak.Controllers
{
    using System;
    using System.Buffers;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.FileProviders;

    /// <summary>
    /// The API controller.
    /// </summary>
    [Route("api")]
    [ApiController]
    public class ApiController : ControllerBase
    {
        /// <summary>
        /// The http client.
        /// </summary>
        private static readonly HttpClient HttpClient = new HttpClient();

        /// <summary>
        /// The static strings.
        /// </summary>
        private static readonly ConcurrentBag<string> StaticStrings = new ConcurrentBag<string>();

        /// <summary>
        /// The temp path.
        /// </summary>
        private static readonly string TempPath = Path.GetTempPath();

        /// <summary>
        /// The array pool.
        /// </summary>
        private static readonly ArrayPool<byte> ArrayPool = ArrayPool<byte>.Create();

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiController"/> class.
        /// </summary>
        public ApiController()
        {
            Interlocked.Increment(ref DiagnosticsController.Requests);
        }

        /// <summary>
        /// The get static string.
        /// </summary>
        /// <returns>
        /// The <see cref="ActionResult"/>.
        /// </returns>
        [HttpGet("staticstring")]
        public ActionResult<string> GetStaticString()
        {
            var bigString = new string('x', 10 * 1024);
            StaticStrings.Add(bigString);
            return bigString;
        }

        /// <summary>
        /// The get big string.
        /// </summary>
        /// <returns>
        /// The <see cref="ActionResult"/>.
        /// </returns>
        [HttpGet("bigstring")]
        public ActionResult<string> GetBigString()
        {
            return new string('x', 10 * 1024);
        }

        /// <summary>
        /// The get LOH.
        /// </summary>
        /// <param name="size">
        /// The size.
        /// </param>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        [HttpGet("loh/{size=85000}")]
        public int GetLOH(int size)
        {
            return new byte[size].Length;
        }

        /// <summary>
        /// The get file provider.
        /// </summary>
        [HttpGet("fileprovider")]
        public void GetFileProvider()
        {
            var fp = new PhysicalFileProvider(TempPath);
            fp.Watch("*.*");
        }

        /// <summary>
        /// The get http client 1.
        /// </summary>
        /// <param name="url">
        /// The url.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        [HttpGet("httpclient1")]
        public async Task<int> GetHttpClient1(string url)
        {
            using (var httpClient = new HttpClient())
            {
                var result = await httpClient.GetAsync(url);
                return (int)result.StatusCode;
            }
        }

        /// <summary>
        /// The get http client 2.
        /// </summary>
        /// <param name="url">
        /// The url.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        [HttpGet("httpclient2")]
        public async Task<int> GetHttpClient2(string url)
        {
            var result = await HttpClient.GetAsync(url);
            return (int)result.StatusCode;
        }

        /// <summary>
        /// The get array.
        /// </summary>
        /// <param name="size">
        /// The size.
        /// </param>
        /// <returns>
        /// The <see cref="byte[T]"/>.
        /// </returns>
        [HttpGet("array/{size}")]
        public byte[] GetArray(int size)
        {
            var array = new byte[size];

            var random = new Random();
            random.NextBytes(array);

            return array;
        }

        /// <summary>
        /// The get pooled array.
        /// </summary>
        /// <param name="size">
        /// The size.
        /// </param>
        /// <returns>
        /// The <see cref="byte[]"/>.
        /// </returns>
        [HttpGet("pooledarray/{size}")]
        public byte[] GetPooledArray(int size)
        {
            var pooledArray = new PooledArray(size);

            var random = new Random();
            random.NextBytes(pooledArray.Array);

            HttpContext.Response.RegisterForDispose(pooledArray);

            return pooledArray.Array;
        }

        /// <summary>
        /// The pooled array.
        /// </summary>
        private class PooledArray : IDisposable
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="PooledArray"/> class.
            /// </summary>
            /// <param name="size">
            /// The size.
            /// </param>
            public PooledArray(int size)
            {
                this.Array = ArrayPool.Rent(size);
            }

            /// <summary>
            /// Gets the array.
            /// </summary>
            public byte[] Array { get; private set; }

            /// <summary>
            /// The dispose.
            /// </summary>
            public void Dispose()
            {
                ArrayPool.Return(this.Array);
            }
        }
    }
}
