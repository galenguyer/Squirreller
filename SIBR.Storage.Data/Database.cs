﻿using System;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
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
            NpgsqlConnection.GlobalTypeMapper.UseJsonNet().UseNodaTime();
            DefaultTypeMap.MatchNamesWithUnderscores = true;
            SqlMapper.AddTypeHandler(new PassthroughTypeHandler<Instant>(NpgsqlDbType.TimestampTz));
            SqlMapper.AddTypeHandler(new JsonTypeHandler<JToken>());
            SqlMapper.AddTypeHandler(new JsonTypeHandler<JValue>());
            SqlMapper.AddTypeHandler(new JsonTypeHandler<JArray>());
            SqlMapper.AddTypeHandler(new JsonTypeHandler<JObject>());
            SqlMapper.AddTypeHandler(new JsonTypeHandler<JObject>());
            SqlMapper.AddTypeHandler(new STJsonTypeHandler());
        }

        public Database(IServiceProvider services, string connectionString)
        {
            _logger = services.GetRequiredService<ILogger>().ForContext<Database>();
            _connectionString = new NpgsqlConnectionStringBuilder(connectionString)
                {
                    Enlist = false,
                    NoResetOnClose = true,
                    WriteBufferSize = 1024*64
                }
                .ConnectionString;
        }

        public async Task<NpgsqlConnection> Obtain()
        {
            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }
        
        public async Task RefreshMaterializedViews(NpgsqlConnection conn, params string[] matViews)
        {
            foreach (var matView in matViews)
            {
                var sw = new Stopwatch();
                sw.Start();
                await conn.ExecuteAsync($"refresh materialized view concurrently {matView}");
                sw.Stop();

                _logger.Information("Refreshed materialized view {ViewName} in {Duration}", matView, sw.Elapsed);
            }
        }
        
        public async Task RunMigrations(bool repair)
        {
            await using var connection = await Obtain();
            var evolve = new Evolve.Evolve(connection, msg => _logger.Information("Evolve: {Message}", msg))
            {
                Locations = new[] {Path.Join(Path.GetDirectoryName(typeof(Database).Assembly.Location), "Schema")}, 
                IsEraseDisabled = true,
                SqlMigrationPrefix = "v",
                SqlRepeatableMigrationPrefix = "r",
                CommandTimeout = 9999999 // some of these are really long!
            };
            
            if (repair)
                evolve.Repair();

            // Evolve isn't async >.>
            await Task.Run(() => evolve.Migrate());
        }

        private class JsonTypeHandler<T> : SqlMapper.TypeHandler<T> where T: JToken
        {
            public override void SetValue(IDbDataParameter parameter, T value)
            {
                var p = (NpgsqlParameter) parameter;
                
                p.Value = value;
                p.NpgsqlDbType = NpgsqlDbType.Jsonb;
            }

            public override T Parse(object value)
            {
                return JsonConvert.DeserializeObject<T>((string) value);
            }
        }
        
        private class STJsonTypeHandler : SqlMapper.TypeHandler<JsonElement>
        {
            public override void SetValue(IDbDataParameter parameter, JsonElement value)
            {
                var p = (NpgsqlParameter) parameter;
                
                p.Value = value.GetRawText();
                p.NpgsqlDbType = NpgsqlDbType.Jsonb;
            }

            public override JsonElement Parse(object value)
            {
                using var doc = JsonDocument.Parse((string) value);
                return doc.RootElement.Clone();
            }
        }
        
        private class PassthroughTypeHandler<T>: SqlMapper.TypeHandler<T>
        {
            private readonly NpgsqlDbType _type;

            public PassthroughTypeHandler(NpgsqlDbType type)
            {
                _type = type;
            }

            public override void SetValue(IDbDataParameter parameter, T value)
            {
                parameter.Value = value;
                if (parameter is NpgsqlParameter p)
                    p.NpgsqlDbType = _type;
            }

            public override T Parse(object value) => (T) value;
        }
    }
}