namespace EventsParser;

public sealed class EventDto
{
    public string EventLink { get; set; } = "";

    public string Title { get; set; } = "";

    public string Date { get; set; } = "";

    public string? StartTime { get; set; }

    public string? DoorsTime { get; set; }

    public string Description { get; set; } = "";

    public LocationDto Location { get; set; } = new();

    public List<string> Genres { get; set; } = [];

    public string? ImageUrl { get; set; }

    public List<TicketDto> Tickets { get; set; } = [];

    public string? OriginalEventLink { get; set; }

    public List<string> Lineup { get; set; } = [];
}
