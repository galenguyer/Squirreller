﻿using System;
using System.Text.Json;
using NodaTime;

namespace SIBR.Storage.API.Controllers.Models
{
    public class ApiPlayerUpdate
    {
        public Guid Id { get; set; }
        public Instant FirstSeen { get; set; }
        public Instant LastSeen { get; set; }
        public Guid Hash { get; set; }
        public JsonElement Data { get; set; }
    }
}