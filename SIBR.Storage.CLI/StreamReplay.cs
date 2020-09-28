﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Serilog;
using SIBR.Storage.Data;
using SIBR.Storage.Data.Models;
using SIBR.Storage.Data.Utils;

namespace SIBR.Storage.CLI
{
    public class StreamReplay
    {
        private readonly UpdateStore _updateStore;
        private readonly Database _db;
        private readonly ILogger _logger;

        public StreamReplay(UpdateStore updateStore, Database db, ILogger logger)
        {
            _updateStore = updateStore;
            _db = db;
            _logger = logger.ForContext<StreamReplay>();
        }

        public async Task Run()
        {
            using var hasher = new SibrHasher();
            var updates = _updateStore.ExportAllUpdatesRaw(UpdateType.Stream)
                .SelectMany(streamUpdate =>
                {
                    var obj = JObject.Parse(streamUpdate.Data.GetRawText());
                    var extracted = TgbUtils.ExtractUpdatesFromStreamRoot(streamUpdate.SourceId, streamUpdate.Timestamp, obj, hasher);
                    return extracted.EntityUpdates.ToAsyncEnumerable();
                });
            
            var sw = new Stopwatch();

            await using var conn = await _db.Obtain();
            await foreach (var chunk in updates.Buffer(2500))
            {
                sw.Restart();
                await using var tx = await conn.BeginTransactionAsync();
                var saved = await _updateStore.SaveUpdates(conn, chunk.ToList(), false);
                await tx.CommitAsync();
                sw.Stop();

                var timestamp = chunk.Min(u => u.Timestamp);
                _logger.Information("@ {Timestamp}: Saved {NewUpdateCount}/{UpdateCount} updates (took {Duration})",
                    timestamp, saved, chunk.Count, sw.Elapsed);
            }
        }
    }
}