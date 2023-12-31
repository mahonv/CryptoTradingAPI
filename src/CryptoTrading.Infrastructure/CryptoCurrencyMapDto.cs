﻿using Newtonsoft.Json;

namespace CryptoTrading.Infrastructure;

public class CryptoCurrencyMapDto
{
    [JsonProperty("id")] public long Id { get; set; }

    [JsonProperty("rank")] public long Rank { get; set; }

    [JsonProperty("name")] public string Name { get; set; } = null!;

    [JsonProperty("symbol")] public string Symbol { get; set; } = null!;

    [JsonProperty("slug")] public string Slug { get; set; } = null!;

    [JsonProperty("is_active")] public long IsActive { get; set; }

    [JsonProperty("first_historical_data")]
    public DateTimeOffset FirstHistoricalData { get; set; }

    [JsonProperty("last_historical_data")] public DateTimeOffset LastHistoricalData { get; set; }

    [JsonProperty("platform")] public CryptoPlatformDto? Platform { get; set; }
}

public class CryptoPlatformDto
{
    [JsonProperty("id")] public long Id { get; set; }

    [JsonProperty("name")] public string Name { get; set; } = null!;

    [JsonProperty("symbol")] public string Symbol { get; set; } = null!;

    [JsonProperty("slug")] public string Slug { get; set; } = null!;

    [JsonProperty("token_address")] public string TokenAddress { get; set; } = null!;
}