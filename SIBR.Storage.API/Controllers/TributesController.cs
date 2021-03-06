﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using Microsoft.AspNetCore.Mvc;
using NodaTime;
using Serilog;
using SIBR.Storage.API.Models;
using SIBR.Storage.API.Utils;
using SIBR.Storage.Data;
using SIBR.Storage.Data.Models;
using SIBR.Storage.Data.Query;

namespace SIBR.Storage.API.Controllers
{
    [Route("v{version:apiVersion}")]
    [ApiController]
    [ApiVersion("1.0")]
    public class TributesController : ControllerBase
    {
        private readonly IdolsTributesStore _store;
        private readonly ILogger _logger;

        public TributesController(IdolsTributesStore store, ILogger logger)
        {
            _store = store;
            _logger = logger.ForContext<TributesController>();
        }

        [Route("tributes/updates")]
        public async Task<IActionResult> GetTributes([FromQuery] TributeQueryOptions opts)
        {
            var updates = _store.GetTributeUpdates(ToDbOpts(opts, 500));
            return await CreateResponse(opts, updates);
        }

        [Route("tributes/hourly")]
        public async Task<IActionResult> GetTributesHourly([FromQuery] TributeQueryOptions opts)
        {
            var updates = _store.GetTributeUpdatesHourly(ToDbOpts(opts, null));
            return await CreateResponse(opts, updates);
        }

        private async Task<IActionResult> CreateResponse(TributeQueryOptions opts, IAsyncEnumerable<TributesUpdateView> updates)
        {
            return opts.Format switch
            {
                TributeOutputFormat.Json => await WriteToJsonResponse(updates),
                TributeOutputFormat.Csv => await WriteToCsvResponse(updates),
                _ => throw new ArgumentException("should never happen")
            };
        }

        private async Task<IActionResult> WriteToJsonResponse(IAsyncEnumerable<TributesUpdateView> updates)
        {
            var updatesList = await updates.ToListAsync();
            return Ok(new ApiResponsePaginated<ApiTributeUpdate>
            {
                Data = updatesList.Select(u => new ApiTributeUpdate(u)),
                NextPage = updatesList.LastOrDefault()?.NextPage
            });
        }

        private static IdolsTributesStore.TributesQuery ToDbOpts(TributeQueryOptions opts, int? defaultCount)
        {
            return new IdolsTributesStore.TributesQuery
            {
                Before = opts.Before,
                After = opts.After,
                Count = opts.Count ?? defaultCount,
                Order = opts.Order,
                Players = opts.Players,
                Page = opts.Page
            };
        }

        private async Task<ActionResult> WriteToCsvResponse(IAsyncEnumerable<TributesUpdateView> updates)
        {
            Response.ContentType = "text/csv";
            
            await using var stream = new StreamWriter(Response.Body);
            await using var csv = new CsvWriter(stream, CultureInfo.InvariantCulture);

            var playerFieldOrder = new List<Guid>();
            await foreach (var update in updates)
            {
                if (playerFieldOrder.Count == 0)
                {
                    // Set up indices based on first row
                    playerFieldOrder.AddRange(update.Players);
                    
                    // Write headers
                    csv.WriteField("timestamp");
                    foreach (var player in update.Players) 
                        csv.WriteField(player.ToString());
                    await csv.NextRecordAsync();
                }
                
                // Write each row
                var peanutsByPlayer = update.Players
                    .Zip(update.Peanuts)
                    .ToDictionary(k => k.First, v => v.Second);
                
                csv.WriteField(update.Timestamp);
                foreach (var player in playerFieldOrder)
                {
                    if (peanutsByPlayer.TryGetValue(player, out var peanuts))
                        csv.WriteField(peanuts);
                    else
                        csv.WriteField(null);
                }
                    
                await csv.NextRecordAsync();
            }

            return new EmptyResult();
        }
        
        public class TributeQueryOptions: IUpdateQuery
        {
            public Instant? Before { get; set; }
            public Instant? After { get; set; }
            public SortOrder Order { get; set; }
            public PageToken Page { get; set; }
            [Range(1, 1000)] public int? Count { get; set; }
            
            [ModelBinder(BinderType = typeof(CommaSeparatedBinder))]
            public Guid[] Players { get; set; } = null;
            public TributeOutputFormat Format { get; set; } = TributeOutputFormat.Json;
        }

        public enum TributeOutputFormat
        {
            Json,
            Csv
        }
    }
}