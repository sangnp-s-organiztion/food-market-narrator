using MongoDB.Bson;
using MongoDB.Driver;
using food_market_narrator_api.Models;
using System.Globalization;

namespace food_market_narrator_api.Repositories;

public class AnalyticsRepository
{
    private readonly IMongoDatabase _db;

    public AnalyticsRepository(IMongoDatabase mongoDatabase)
    {
        _db = mongoDatabase;
    }

    // ─── KPI: Total Users (sessions) ──────────────────────────────────────────
    public async Task<long> GetTotalSessionCountAsync()
    {
        return await _db.GetCollection<BsonDocument>("UserSessions").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
    }

    // ─── KPI: Avg Listening Time (valid plays only: duration >= 5) ─────────────
    public async Task<double> GetAverageListeningTimeAsync()
    {
        var pipeline = new[]
        {
            new BsonDocument("$match",
                new BsonDocument("duration",
                    new BsonDocument("$gte", 5))),
            new BsonDocument("$group",
                new BsonDocument
                {
                    { "_id", BsonNull.Value },
                    { "avgDuration", new BsonDocument("$avg", "$duration") }
                })
        };

        var result = await _db.GetCollection<BsonDocument>("AudioLogs")
            .Aggregate<BsonDocument>(pipeline)
            .FirstOrDefaultAsync();

        return result != null ? result["avgDuration"].ToDouble() : 0;
    }

    // ─── KPI: Total POI Plays (valid plays only: duration >= 5) ────────────────
    public async Task<long> GetTotalPlayCountAsync()
    {
        var filter = Builders<BsonDocument>.Filter.Gte("duration", 5);
        return await _db.GetCollection<BsonDocument>("AudioLogs").CountDocumentsAsync(filter);
    }

    // ─── Heatmap: GeoJSON points from LocationLogs (hours window or all-time) ─
    public async Task<List<GeoJsonPoint>> GetHeatmapPointsAsync(int? hours = 24)
    {
        var pipeline = new List<BsonDocument>();

        if (hours.HasValue)
        {
            var since = DateTime.UtcNow.AddHours(-hours.Value);
            pipeline.Add(
                new BsonDocument("$match",
                    new BsonDocument("timestamp",
                        new BsonDocument("$gte", since))));
        }

        pipeline.AddRange(new[]
        {
            new BsonDocument("$project",
                new BsonDocument
                {
                    { "lng", new BsonDocument("$arrayElemAt", new BsonArray { "$location.coordinates", 0 }) },
                    { "lat", new BsonDocument("$arrayElemAt", new BsonArray { "$location.coordinates", 1 }) }
                }),
            new BsonDocument("$match",
                new BsonDocument("$expr",
                    new BsonDocument("$and", new BsonArray
                    {
                        new BsonDocument("$ne", new BsonArray { "$lng", BsonNull.Value }),
                        new BsonDocument("$ne", new BsonArray { "$lat", BsonNull.Value })
                    })))
        });

        var points = await _db.GetCollection<BsonDocument>("LocationLogs")
            .Aggregate<BsonDocument>(pipeline)
            .ToListAsync();

        return points
            .Where(p => p.Contains("lng") && p.Contains("lat"))
            .Select(p => new GeoJsonPoint
            {
                Longitude = p["lng"].ToDouble(),
                Latitude = p["lat"].ToDouble()
            })
            .ToList();
    }

    // ─── Avg Listening Time per Audio (group by audio_id, valid plays only) ────
    public async Task<List<AudioStats>> GetAudioStatsAsync()
    {
        var pipeline = new[]
        {
            new BsonDocument("$match",
                new BsonDocument("duration",
                    new BsonDocument("$gte", 5))),
            new BsonDocument("$group",
                new BsonDocument
                {
                    { "_id", "$audio_id" },
                    { "playCount", new BsonDocument("$sum", 1) },
                    { "avgDuration", new BsonDocument("$avg", "$duration") }
                }),
            new BsonDocument("$sort",
                new BsonDocument("playCount", -1))
        };

        var results = await _db.GetCollection<BsonDocument>("AudioLogs")
            .Aggregate<BsonDocument>(pipeline)
            .ToListAsync();

        return results.Select(r => new AudioStats
        {
            AudioId = r["_id"].ToInt32(),
            PlayCount = r["playCount"].ToInt32(),
            AverageDurationSeconds = Math.Round(r["avgDuration"].ToDouble(), 2)
        }).ToList();
    }

    // ─── Top Restaurants by Plays (group by restaurant_id, valid plays only) ──
    public async Task<List<RestaurantStats>> GetRestaurantStatsAsync()
    {
        var pipeline = new[]
        {
            new BsonDocument("$match",
                new BsonDocument("duration",
                    new BsonDocument("$gte", 5))),
            new BsonDocument("$group",
                new BsonDocument
                {
                    { "_id", "$restaurant_id" },
                    { "playCount", new BsonDocument("$sum", 1) },
                    { "avgDuration", new BsonDocument("$avg", "$duration") }
                }),
            new BsonDocument("$sort",
                new BsonDocument("playCount", -1))
        };

        var results = await _db.GetCollection<BsonDocument>("AudioLogs")
            .Aggregate<BsonDocument>(pipeline)
            .ToListAsync();

        return results.Select(r => new RestaurantStats
        {
            RestaurantId = r["_id"].ToString(),
            PlayCount = r["playCount"].ToInt32(),
            AverageDurationSeconds = Math.Round(r["avgDuration"].ToDouble(), 2)
        }).ToList();
    }

    // ─── Movement Paths: ordered coordinates per session (last N sessions) ───
    public async Task<List<SessionPath>> GetMovementPathsAsync(int? limit = 100)
    {
        // Distinct session_ids ordered by most recent activity.
        // IMPORTANT: group first, then limit by sessions (not by raw log rows).
        var sessionPipeline = new List<BsonDocument>
        {
            new("$addFields",
                new BsonDocument("event_time",
                    new BsonDocument("$ifNull", new BsonArray { "$timestamp", "$created_at" }))),
            new("$match",
                new BsonDocument
                {
                    { "session_id", new BsonDocument("$ne", BsonNull.Value) },
                    { "event_time", new BsonDocument("$ne", BsonNull.Value) }
                }),
            new("$group",
                new BsonDocument
                {
                    { "_id", "$session_id" },
                    { "last_time", new BsonDocument("$max", "$event_time") }
                }),
            new("$sort",
                new BsonDocument("last_time", -1))
        };

        if (limit.HasValue && limit.Value > 0)
        {
            sessionPipeline.Add(new BsonDocument("$limit", limit.Value));
        }

        var sessionDocs = await _db.GetCollection<BsonDocument>("LocationLogs")
            .Aggregate<BsonDocument>(sessionPipeline)
            .ToListAsync();

        if (!sessionDocs.Any())
            return [];

        var sessionIds = new BsonArray(sessionDocs.Select(s => s["_id"]).ToList());

        // Fetch all points for those sessions, ordered by timestamp
        var pointsPipeline = new[]
        {
            new BsonDocument("$match",
                new BsonDocument("session_id",
                    new BsonDocument("$in", sessionIds))),
            new BsonDocument("$sort",
                new BsonDocument { { "session_id", 1 }, { "timestamp", 1 } }),
            new BsonDocument("$project",
                new BsonDocument
                {
                    { "session_id", 1 },
                    {
                        "lng",
                        new BsonDocument("$ifNull", new BsonArray
                        {
                            new BsonDocument("$arrayElemAt", new BsonArray { "$location.coordinates", 0 }),
                            "$lng"
                        })
                    },
                    {
                        "lat",
                        new BsonDocument("$ifNull", new BsonArray
                        {
                            new BsonDocument("$arrayElemAt", new BsonArray { "$location.coordinates", 1 }),
                            "$lat"
                        })
                    },
                    {
                        "timestamp",
                        new BsonDocument("$ifNull", new BsonArray { "$timestamp", "$created_at" })
                    }
                })
        };

        var pointDocs = await _db.GetCollection<BsonDocument>("LocationLogs")
            .Aggregate<BsonDocument>(pointsPipeline)
            .ToListAsync();

        // Group by session_id
        var grouped = pointDocs
            .Where(p =>
                !string.IsNullOrWhiteSpace(GetStringValue(p, "session_id"))
                && TryGetDoubleValue(p, "lng", out _)
                && TryGetDoubleValue(p, "lat", out _)
                && p.Contains("timestamp")
                && !p["timestamp"].IsBsonNull)
            .GroupBy(p => p["session_id"].ToString());

        return grouped.Select(g => new SessionPath
        {
            SessionId = g.Key,
            Points = g
                .OrderBy(p => GetDateTimeValue(p, "timestamp", "created_at"))
                .Select(p => new GeoJsonPointWithTimestamp
                {
                    Longitude = GetDoubleValue(p, "lng"),
                    Latitude = GetDoubleValue(p, "lat"),
                    Timestamp = GetDateTimeValue(p, "timestamp", "created_at")
                })
                .ToList()
        }).ToList();
    }

    // ─── Recent Activity: AudioLogs sorted by timestamp DESC ──────────────────
    public async Task<(List<ActivityRecord> Items, long TotalCount)> GetRecentActivityAsync(int page = 1, int pageSize = 10)
    {
        var safePage = Math.Max(page, 1);
        var safePageSize = Math.Max(pageSize, 1);
        var skip = (safePage - 1) * safePageSize;

        var pipeline = new[]
        {
            new BsonDocument("$addFields",
                new BsonDocument("event_time",
                    new BsonDocument("$ifNull", new BsonArray
                    {
                        "$timestamp",
                        new BsonDocument("$ifNull", new BsonArray
                        {
                            "$end_time",
                            new BsonDocument("$ifNull", new BsonArray
                            {
                                "$start_time",
                                "$created_at"
                            })
                        })
                    }))),
            new BsonDocument("$match",
                new BsonDocument
                {
                    { "duration", new BsonDocument("$gte", 5) },
                    { "event_time", new BsonDocument("$ne", BsonNull.Value) }
                }),
            new BsonDocument("$sort",
                new BsonDocument("event_time", -1)),
            new BsonDocument("$facet",
                new BsonDocument
                {
                    {
                        "items", new BsonArray
                        {
                            new BsonDocument("$skip", skip),
                            new BsonDocument("$limit", safePageSize),
                            new BsonDocument("$project",
                                new BsonDocument
                                {
                                    { "_id", 0 },
                                    { "audio_id", 1 },
                                    { "restaurant_id", 1 },
                                    { "duration", 1 },
                                    { "timestamp", "$event_time" }
                                })
                        }
                    },
                    {
                        "totalCount", new BsonArray
                        {
                            new BsonDocument("$count", "count")
                        }
                    }
                })
        };

        var facetResult = await _db.GetCollection<BsonDocument>("AudioLogs")
            .Aggregate<BsonDocument>(pipeline)
            .FirstOrDefaultAsync();

        if (facetResult == null)
            return ([], 0);

        var itemsArray = facetResult.GetValue("items", new BsonArray()).AsBsonArray;
        var totalCountArray = facetResult.GetValue("totalCount", new BsonArray()).AsBsonArray;
        var totalCount = totalCountArray.Count > 0
            ? GetIntValue(totalCountArray[0].AsBsonDocument, "count")
            : 0;

        var items = itemsArray.Select(x => x.AsBsonDocument).Select(r => new ActivityRecord
        {
            AudioId = GetIntValue(r, "audio_id"),
            RestaurantId = GetStringValue(r, "restaurant_id"),
            Duration = GetIntValue(r, "duration"),
            Timestamp = GetDateTimeValue(r, "timestamp", "end_time", "start_time", "created_at")
        }).ToList();

        return (items, totalCount);
    }

    private static int GetIntValue(BsonDocument doc, string key, int defaultValue = 0)
    {
        if (!doc.TryGetValue(key, out var value) || value.IsBsonNull)
            return defaultValue;

        return value.BsonType switch
        {
            BsonType.Int32 => value.AsInt32,
            BsonType.Int64 => (int)value.AsInt64,
            BsonType.Double => (int)value.AsDouble,
            BsonType.Decimal128 => (int)value.AsDecimal,
            BsonType.String when int.TryParse(value.AsString, out var parsed) => parsed,
            _ => defaultValue
        };
    }

    private static double GetDoubleValue(BsonDocument doc, string key, double defaultValue = 0)
    {
        if (!doc.TryGetValue(key, out var value) || value.IsBsonNull)
            return defaultValue;

        return value.BsonType switch
        {
            BsonType.Double => value.AsDouble,
            BsonType.Int32 => value.AsInt32,
            BsonType.Int64 => value.AsInt64,
            BsonType.Decimal128 => (double)value.AsDecimal,
            BsonType.String when double.TryParse(value.AsString, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => defaultValue
        };
    }

    private static bool TryGetDoubleValue(BsonDocument doc, string key, out double value)
    {
        if (!doc.TryGetValue(key, out var bsonValue) || bsonValue.IsBsonNull)
        {
            value = default;
            return false;
        }

        switch (bsonValue.BsonType)
        {
            case BsonType.Double:
                value = bsonValue.AsDouble;
                return true;
            case BsonType.Int32:
                value = bsonValue.AsInt32;
                return true;
            case BsonType.Int64:
                value = bsonValue.AsInt64;
                return true;
            case BsonType.Decimal128:
                value = (double)bsonValue.AsDecimal;
                return true;
            case BsonType.String:
                return double.TryParse(bsonValue.AsString, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
            default:
                value = default;
                return false;
        }
    }

    private static string GetStringValue(BsonDocument doc, string key, string defaultValue = "")
    {
        if (!doc.TryGetValue(key, out var value) || value.IsBsonNull)
            return defaultValue;

        return value.ToString();
    }

    private static DateTime GetDateTimeValue(BsonDocument doc, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!doc.TryGetValue(key, out var value) || value.IsBsonNull)
                continue;

            switch (value.BsonType)
            {
                case BsonType.DateTime:
                    return value.ToUniversalTime();
                case BsonType.String:
                    if (DateTime.TryParse(value.AsString, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
                        return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
                    break;
            }
        }

        return DateTime.UtcNow;
    }

    // ─── Internal models ──────────────────────────────────────────────────────
    public class GeoJsonPoint
    {
        public double Longitude { get; set; }
        public double Latitude { get; set; }
    }

    public class GeoJsonPointWithTimestamp : GeoJsonPoint
    {
        public DateTime Timestamp { get; set; }
    }

    public class AudioStats
    {
        public int AudioId { get; set; }
        public int PlayCount { get; set; }
        public double AverageDurationSeconds { get; set; }
    }

    public class RestaurantStats
    {
        public string RestaurantId { get; set; } = string.Empty;
        public int PlayCount { get; set; }
        public double AverageDurationSeconds { get; set; }
    }

    public class SessionPath
    {
        public string SessionId { get; set; } = string.Empty;
        public List<GeoJsonPointWithTimestamp> Points { get; set; } = [];
    }

    public class ActivityRecord
    {
        public int AudioId { get; set; }
        public string RestaurantId { get; set; } = string.Empty;
        public int Duration { get; set; }
        public DateTime Timestamp { get; set; }
    }

    // ─── Daily Listen Counts (timeseries) ─────────────────────────────────────
    public async Task<List<DailyListenCount>> GetDailyListenCountsAsync(int days = 14)
    {
        var since = DateTime.UtcNow.AddDays(-days);

        var pipeline = new[]
        {
            new BsonDocument("$addFields",
                new BsonDocument("event_time_raw",
                    new BsonDocument("$ifNull", new BsonArray
                    {
                        "$start_time",
                        new BsonDocument("$ifNull", new BsonArray
                        {
                            "$timestamp",
                            "$created_at"
                        })
                    }))),
            new BsonDocument("$addFields",
                new BsonDocument("event_time",
                    new BsonDocument("$convert", new BsonDocument
                    {
                        { "input", "$event_time_raw" },
                        { "to", "date" },
                        { "onError", BsonNull.Value },
                        { "onNull", BsonNull.Value }
                    }))),
            new BsonDocument("$match",
                new BsonDocument
                {
                    { "event_time", new BsonDocument("$ne", BsonNull.Value) },
                    { "event_time", new BsonDocument("$gte", since) },
                    { "duration", new BsonDocument("$gte", 5) }
                }),
            new BsonDocument("$group",
                new BsonDocument
                {
                    { "_id", new BsonDocument("$dateToString",
                        new BsonDocument { { "format", "%Y-%m-%d" }, { "date", "$event_time" } }) },
                    { "count", new BsonDocument("$sum", 1) }
                }),
            new BsonDocument("$sort",
                new BsonDocument("_id", 1))
        };

        var results = await _db.GetCollection<BsonDocument>("AudioLogs")
            .Aggregate<BsonDocument>(pipeline)
            .ToListAsync();

        return results.Select(r => new DailyListenCount
        {
            Date = r["_id"].ToString(),
            Count = r["count"].ToInt32()
        }).ToList();
    }

    public class DailyListenCount
    {
        public string Date { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
