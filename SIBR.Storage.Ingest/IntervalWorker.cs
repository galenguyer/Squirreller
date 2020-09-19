﻿using System;
using System.Threading.Tasks;
using Serilog;

namespace SIBR.Storage.Ingest
{
    public abstract class IntervalWorker : BaseWorker
    {
        public TimeSpan Interval { get; set; }
        
        private readonly ILogger _logger;

        protected abstract Task RunInterval();

        protected override async Task Run()
        {
            while (true)
            {
                try
                {
                    await RunInterval();
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Error while running worker {WorkerType}", GetType().Name);
                }

                // (arbitrary, just for tick alignment mostly)
                var epoch = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
                var waitTime = TimeSpan.FromTicks(Interval.Ticks - (DateTimeOffset.Now - epoch).Ticks % Interval.Ticks);
                if (waitTime > TimeSpan.Zero)
                    await Task.Delay(waitTime);
            }
        }

        protected IntervalWorker(ILogger logger) : base(logger)
        {
            _logger = logger;
        }
    }
}