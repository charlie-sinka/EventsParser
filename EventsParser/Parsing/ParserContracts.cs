namespace EventsParser.Parsing;

public readonly record struct AgendaListRow(string Url, string Title, string? ThumbnailUrl = null);

public interface IAgendaParser
{
    IReadOnlyList<AgendaListRow> ExtractRows(string html, Uri pageUri, out int detailContainers);

    Uri? TryGetNextAgendaPageUri(string html, Uri pageUri);
}

public interface IEventParser
{
    EventDto? Parse(string html, Uri eventPageUri);
}

public interface ISiteProfile
{
    string SiteKey { get; }
    string DefaultAgendaUrl { get; }
    IAgendaParser CreateAgendaParser();
    IEventParser CreateEventParser();
}
