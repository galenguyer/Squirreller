using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using MongoDb.Bson.NodaTime;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NodaTime;
using Serilog;

namespace SIBR.Storage.Data
{
    public class Database
    {
        private readonly string _connectionString;
        private readonly ILogger _logger;

        public static void Init()
        {
            NodaTimeSerializers.Register();
        }

        public Database(IServiceProvider services, string connectionString)
        {
            _logger = services.GetRequiredService<ILogger>().ForContext<Database>();
            _connectionString = connectionString;
        }

        public async Task<MongoClient> Obtain()
        {
            var connection = new MongoClient(_connectionString);
            return connection;
        }

        //public async Task RefreshMaterializedViews(NpgsqlConnection conn, params string[] matViews)
        //{
        //    foreach (var matView in matViews)
        //    {
        //        var sw = new Stopwatch();
        //        sw.Start();
        //        await conn.ExecuteAsync($"refresh materialized view concurrently {matView}");
        //        sw.Stop();

        //        _logger.Information("Refreshed materialized view {ViewName} in {Duration}", matView, sw.Elapsed);
        //    }
        //}
    }
}