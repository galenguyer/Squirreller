﻿using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using SIBR.Storage.Data.Models;

namespace SIBR.Storage.Ingest
{
    public class IngestWorkers
    {
        public static IEnumerable<BaseWorker> CreateWorkers(IServiceProvider services, Guid sourceId) => new BaseWorker[]
        {
            new MiscEndpointWorker(services, Duration.FromMinutes(1), sourceId, new[]
            {
                (UpdateType.Idols, "https://www.blaseball.com/api/getIdols"),
                (UpdateType.Tributes, "https://www.blaseball.com/api/getTribute"),
                (UpdateType.GlobalEvents, "https://www.blaseball.com/database/globalEvents"),
                (UpdateType.Sim, "https://www.blaseball.com/database/simulationData"),
            }, new []{"idols_versions", "tributes_versions"}),
            new MiscEndpointWorker(services, Duration.FromMinutes(10),  sourceId, new[]
            {
                (UpdateType.OffseasonSetup, "https://www.blaseball.com/database/offseasonSetup"),
            }), 
            new SiteUpdateWorker(services, sourceId),
            new StreamDataWorker(services, sourceId), 
            new TeamPlayerDataWorker(services, sourceId),
            new GameEndpointWorker(services, sourceId), 
        };
    }
}