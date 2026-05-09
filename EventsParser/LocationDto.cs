using System.Text.Json.Serialization;

namespace EventsParser;

public sealed class LocationDto
{
    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public string City { get; set; } = "";

    public string Country { get; set; } = "";

    public string Address { get; set; } = "";

    public string VenueName { get; set; } = "";

    public string? VenueWebsite { get; set; }

    [JsonIgnore]
    public string? MuziekladderVenueUrl { get; set; }
}
