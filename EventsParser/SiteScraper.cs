using EventsParser.Parsing;

namespace EventsParser;

public sealed class SiteScraper(HttpClient http, ISiteProfile siteProfile)
{
    private const int MaxAgendaPages = 500;

    private readonly Dictionary<string, string?> _venueWebsiteByListingUrl = new(StringComparer.OrdinalIgnoreCase);

    private readonly IAgendaParser _index = siteProfile.CreateAgendaParser();
    private readonly IEventParser _events = siteProfile.CreateEventParser();

    public async Task<List<EventDto>> FetchEventsAsync(
        string agendaUrl,
        int delayBetweenGigsMs = 300,
        int maxGigs = 0,
        IProgress<string>? log = null,
        CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(agendaUrl, UriKind.Absolute, out var agendaUri))
            throw new ArgumentException("Agenda URL must be absolute.", nameof(agendaUrl));

        var rows = await CollectAllAgendaRowsAsync(agendaUri, delayBetweenGigsMs, log, cancellationToken).ConfigureAwait(false);
        log?.Report($"Found agenda rows (all pages, deduped): {rows.Count}.");

        var total = rows.Count;
        if (maxGigs > 0 && rows.Count > maxGigs)
        {
            rows = rows.Take(maxGigs).ToList();
            log?.Report($"Applying maxGigs={maxGigs}: processing {rows.Count} of {total} rows.");
        }

        var result = new List<EventDto>();
        for (var i = 0; i < rows.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var current = i + 1;
            var gigUrl = rows[i].Url;
            log?.Report($"[{current}/{rows.Count}] Parsing {gigUrl}");
            if (!Uri.TryCreate(gigUrl, UriKind.Absolute, out var gigUri))
            {
                log?.Report($"[{current}/{rows.Count}] Invalid event URL: {gigUrl}");
                continue;
            }

            try
            {
                using var greq = new HttpRequestMessage(HttpMethod.Get, gigUri);
                using var gres = await http.SendAsync(greq, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                if (!gres.IsSuccessStatusCode)
                {
                    log?.Report($"[{current}/{rows.Count}] Skipped (HTTP {(int)gres.StatusCode}): {gigUri}");
                    continue;
                }

                var gigHtml = await gres.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                var dto = _events.Parse(gigHtml, gigUri);
                if (dto is null)
                {
                    log?.Report($"[{current}/{rows.Count}] Parse returned null: {gigUri}");
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(rows[i].Title))
                    dto.Title = rows[i].Title;

                var thumb = rows[i].ThumbnailUrl;
                if (!string.IsNullOrWhiteSpace(thumb) && string.IsNullOrWhiteSpace(dto.ImageUrl))
                    dto.ImageUrl = thumb;

                await TryFillVenueWebsiteAsync(dto, current, rows.Count, log, cancellationToken).ConfigureAwait(false);

                dto.Location.MuziekladderVenueUrl = null;

                result.Add(dto);
                log?.Report($"[{current}/{rows.Count}] Parsed successfully.");
            }
            catch (Exception ex)
            {
                log?.Report($"[{current}/{rows.Count}] Parse error for {gigUri}: {ex.Message}");
            }

            if (i < rows.Count - 1 && delayBetweenGigsMs > 0)
                await Task.Delay(delayBetweenGigsMs, cancellationToken).ConfigureAwait(false);
        }

        return result;
    }

    private async Task<List<AgendaListRow>> CollectAllAgendaRowsAsync(
        Uri agendaUri,
        int delayBetweenRequestsMs,
        IProgress<string>? log,
        CancellationToken cancellationToken)
    {
        var rows = new List<AgendaListRow>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var pagina = 0; pagina < MaxAgendaPages; pagina++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var pageUri = AgendaPaginaUri.WithPagina(agendaUri, pagina);
            log?.Report($"Fetching agenda pagina={pagina}: {pageUri}");

            using var req = new HttpRequestMessage(HttpMethod.Get, pageUri);
            using var res = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            res.EnsureSuccessStatusCode();
            var html = await res.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            var pageRows = _index.ExtractRows(html, pageUri, out var detailContainers).ToList();
            if (pageRows.Count == 0)
            {
                log?.Report($"Pagina={pagina}: no gig rows, stopping pagination.");
                break;
            }

            var added = 0;
            foreach (var row in pageRows)
            {
                if (seen.Add(row.Url))
                {
                    rows.Add(row);
                    added++;
                }
            }

            log?.Report($"Pagina={pagina}: rows={pageRows.Count}, new unique={added}, detail blocks={detailContainers}.");
            if (added == 0)
            {
                log?.Report($"Pagina={pagina}: no new gigs (duplicates only), stopping pagination.");
                break;
            }

            if (delayBetweenRequestsMs > 0 && pagina < MaxAgendaPages - 1)
                await Task.Delay(delayBetweenRequestsMs, cancellationToken).ConfigureAwait(false);
        }

        return rows;
    }

    private async Task TryFillVenueWebsiteAsync(
        EventDto dto,
        int current,
        int total,
        IProgress<string>? log,
        CancellationToken cancellationToken)
    {
        var venueUrl = dto.Location.MuziekladderVenueUrl;
        if (string.IsNullOrWhiteSpace(venueUrl))
            return;

        if (!Uri.TryCreate(venueUrl, UriKind.Absolute, out var venueUri))
            return;

        var cacheKey = venueUri.GetLeftPart(UriPartial.Path);
        if (_venueWebsiteByListingUrl.TryGetValue(cacheKey, out var memo))
        {
            dto.Location.VenueWebsite = memo;
            return;
        }

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, venueUri);
            using var res = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            if (!res.IsSuccessStatusCode)
            {
                log?.Report($"[{current}/{total}] Venue page HTTP {(int)res.StatusCode}: {venueUri}");
                _venueWebsiteByListingUrl[cacheKey] = null;
                return;
            }

            var venueHtml = await res.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var website = VenueWebsiteExtractor.TryExtract(venueHtml);
            dto.Location.VenueWebsite = website;
            _venueWebsiteByListingUrl[cacheKey] = website;
        }
        catch (Exception ex)
        {
            log?.Report($"[{current}/{total}] Venue fetch error ({venueUri}): {ex.Message}");
            _venueWebsiteByListingUrl[cacheKey] = null;
        }
    }
}
