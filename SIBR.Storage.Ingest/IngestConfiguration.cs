﻿using System;
using System.Collections.Generic;
using SIBR.Storage.Data.Models;

namespace SIBR.Storage.Ingest
{
    public class IngestConfiguration
    {
        public Guid SourceId { get; set; }
        public IntervalWorkerConfiguration FutureGamesWorker { get; set; }
        public IntervalWorkerConfiguration GameEndpointWorker { get; set; }
        public List<MiscEndpointWorkerConfiguration> MiscEndpointWorkers { get; set; }
        public IntervalWorkerConfiguration SiteUpdateWorker { get; set; }
        public IntervalWorkerConfiguration StatsheetsWorker { get; set; }
        public IntervalWorkerConfiguration TeamPlayerWorker { get; set; }
        public ElectionResultsConfiguration ElectionResultsWorker { get; set; }
    }

    public class IntervalWorkerConfiguration
    {
        public TimeSpan Interval { get; set; }
        public TimeSpan Offset { get; set; }
    }

    public class ElectionResultsConfiguration: IntervalWorkerConfiguration
    {
        public TimeSpan ThrottleInterval { get; set; }
    }
    
    public class MiscEndpointWorkerConfiguration: IntervalWorkerConfiguration
    {
        public List<IngestEndpoint> Endpoints { get; set; }
        public List<string> MaterializedViews { get; set; }
    }

    public class IngestEndpoint
    {
        public string Url { get; set; }
        public UpdateType Type { get; set; }
    }
}