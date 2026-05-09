using AngleSharp.Html.Parser;

namespace EventsParser.Parsing;

internal static class VenueWebsiteExtractor
{
    public static string? TryExtract(string html)
    {
        var parser = new HtmlParser();
        var doc = parser.ParseDocument(html);
        foreach (var strong in doc.QuerySelectorAll("td strong"))
        {
            if (!strong.TextContent.Contains("Website", StringComparison.OrdinalIgnoreCase))
                continue;
            var td = strong.Closest("td");
            if (td is null)
                continue;
            var a = td.QuerySelector("a[href^='http']");
            var href = a?.GetAttribute("href");
            return string.IsNullOrWhiteSpace(href) ? null : href.Trim();
        }

        return null;
    }
}
