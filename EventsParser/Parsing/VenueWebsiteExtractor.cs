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
            if (!strong.TextContent.Trim().Equals("Website", StringComparison.OrdinalIgnoreCase))
                continue;

            var tr = strong.Closest("tr");
            if (tr is null)
                continue;

            foreach (var a in tr.QuerySelectorAll("a[href]"))
            {
                var href = a.GetAttribute("href")?.Trim();
                if (string.IsNullOrEmpty(href))
                    continue;
                if (href.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                    href.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    return href;
            }
        }

        return null;
    }
}
