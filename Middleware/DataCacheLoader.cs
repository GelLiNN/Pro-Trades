using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Sociosearch.NET.Middleware
{
    public abstract class BackgroundTaskRunner : IHostedService
    {
        private Task _executingTask;
        private CancellationTokenSource _cts;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            //Create a linked token so we can trigger cancellation outside of this token's cancellation
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            //Store the task we're executing
            _executingTask = ExecuteAsync(_cts.Token);

            //If the task is completed then return it, otherwise it's running
            return _executingTask.IsCompleted ? _executingTask : Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            //Stop called without start
            if (_executingTask == null) { return; }

            //Signal cancellation to the executing method
            _cts.Cancel();

            //Wait until the task completes or the stop token triggers
            await Task.WhenAny(_executingTask, Task.Delay(-1, cancellationToken));

            //Throw if cancellation triggered
            cancellationToken.ThrowIfCancellationRequested();
        }

        //Derived classes should override this and execute a long running method until cancellation is requested
        protected abstract Task ExecuteAsync(CancellationToken cancellationToken);
    }

    public class DataCacheLoader : BackgroundTaskRunner
    {
        private readonly DataCache _cache;

        public DataCacheLoader(DataCache cache) { _cache = cache; }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var cacheLoadTask1 = _cache.LoadCacheAsync("iex-companies"); //Investors Exchange IEX.cs
                var cacheLoadTask2 = _cache.LoadCacheAsync("yh-companies"); //Yahoo Finance YF.cs
                //var cacheLoadTask3 = _cache.LoadCacheAsync("cbcorporate-products");
                await Task.WhenAll(cacheLoadTask1, cacheLoadTask2/*, cacheLoadTask3*/);

                //Set this Task to cancel now that it is complete
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }
    }
}
