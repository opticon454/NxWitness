﻿using Newtonsoft.Json;
using System.Diagnostics;
using Serilog;

namespace CreateMatrix;

// https://updates.vmsproxy.com/{product}/releases.json
// https://updates.vmsproxy.com/default/releases.json
// https://updates.vmsproxy.com/metavms/releases.json
// https://updates.vmsproxy.com/digitalwatchdog/releases.json
public class ReleasesJsonSchema
{
    private static readonly JsonSerializerSettings Settings = new()
    {
        Formatting = Formatting.Indented,
        StringEscapeHandling = StringEscapeHandling.EscapeNonAscii,
        NullValueHandling = NullValueHandling.Ignore,
        MissingMemberHandling = MissingMemberHandling.Ignore,
        ObjectCreationHandling = ObjectCreationHandling.Replace
    };

    public class Release
    {
        [JsonProperty("product")] public string Product { get; set; } = "";

        [JsonProperty("version")] public string Version { get; set; } = "";

        [JsonProperty("protocol_version")] public int ProtocolVersion { get; set; }

        [JsonProperty("publication_type")] public string PublicationType { get; set; } = "";

        [JsonProperty("release_date")] public long ReleaseDate { get; set; }

        [JsonProperty("release_delivery_days")] public int ReleaseDeliveryDays { get; set; }

        public bool IsReleased()
        {
            // TODO: Follow up with Nx on correctness of logic
            // Similar to logic used in releases_info.cpp : canReceiveUnpublishedBuild()
            // release_date is a null or a quoted string in JSON
            // release_delivery_days is a null or integer in JSON
            // JSON to int conversion will default to 0 for null, so testing for >= will always be true?
            return ReleaseDate > 0 && ReleaseDeliveryDays >= 0;
        }
    }

    [JsonProperty("packages_urls")] public List<string> PackagesUrls { get; set; } = new();

    [JsonProperty("releases")] public List<Release> Releases { get; set; } = new();

    private static ReleasesJsonSchema FromJson(string jsonString)
    {
        var jsonSchema = JsonConvert.DeserializeObject<ReleasesJsonSchema>(jsonString, Settings);
        ArgumentNullException.ThrowIfNull(jsonSchema);
        return jsonSchema;
    }

    internal static List<Release> GetReleases(HttpClient httpClient, string productName)
    {
        // Load releases JSON
        // https://updates.vmsproxy.com/{product}/releases.json
        Uri releasesUri = new($"https://updates.vmsproxy.com/{productName}/releases.json");
        Log.Logger.Information("Getting release information from {Uri}", releasesUri);
        var jsonString = httpClient.GetStringAsync(releasesUri).Result;
        var releasesSchema = FromJson(jsonString);
        ArgumentNullException.ThrowIfNull(releasesSchema);
        ArgumentNullException.ThrowIfNull(releasesSchema.Releases);
        Debug.Assert(releasesSchema.Releases.Count > 0);

        // Return releases
        return releasesSchema.Releases;
    }
}