using System.Text.Json.Serialization;

namespace OphirMineralVentures.Web.Seo;

/// <summary>schema.org Organization/LocalBusiness JSON-LD, driven by siteSettings (CLAUDE.md §4a —
/// no hardcoded company details in Razor). Typed rather than an anonymous object + string-replace so
/// the "@type"/"@context" keys (not valid C# identifiers) are handled by JsonPropertyName, not string
/// hacking on the serialized output.</summary>
public class OrganizationSchema
{
    [JsonPropertyName("@context")]
    public string Context { get; init; } = "https://schema.org";

    [JsonPropertyName("@type")]
    public string Type { get; init; } = "Organization";

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("url")]
    public string? Url { get; init; }

    [JsonPropertyName("telephone")]
    public string? Telephone { get; init; }

    [JsonPropertyName("address")]
    public PostalAddressSchema? Address { get; init; }
}

public class PostalAddressSchema
{
    [JsonPropertyName("@type")]
    public string Type { get; init; } = "PostalAddress";

    [JsonPropertyName("streetAddress")]
    public string? StreetAddress { get; init; }
}
