using System.Globalization;
using System.Text;

namespace EventsParser.Parsing;

internal static class AgendaPaginaUri
{
    public static Uri WithPagina(Uri agendaUri, int paginaIndex)
    {
        var builder = new UriBuilder(agendaUri);
        var pairs = ParseQuery(builder.Query);
        pairs.RemoveAll(static kv => kv.Key.Equals("pagina", StringComparison.OrdinalIgnoreCase));
        pairs.Add(new KeyValuePair<string, string>(
            "pagina",
            paginaIndex.ToString(CultureInfo.InvariantCulture)));
        builder.Query = FormatQuery(pairs);
        return builder.Uri;
    }

    private static List<KeyValuePair<string, string>> ParseQuery(string? query)
    {
        var list = new List<KeyValuePair<string, string>>();
        if (string.IsNullOrEmpty(query))
            return list;
        var trimmed = query.StartsWith('?') ? query[1..] : query;
        if (trimmed.Length == 0)
            return list;
        foreach (var segment in trimmed.Split('&'))
        {
            if (segment.Length == 0)
                continue;
            var idx = segment.IndexOf('=');
            string key, value;
            if (idx < 0)
            {
                key = Uri.UnescapeDataString(segment);
                value = "";
            }
            else
            {
                key = Uri.UnescapeDataString(segment[..idx]);
                value = Uri.UnescapeDataString(segment[(idx + 1)..]);
            }

            list.Add(new KeyValuePair<string, string>(key, value));
        }

        return list;
    }

    private static string FormatQuery(List<KeyValuePair<string, string>> pairs)
    {
        if (pairs.Count == 0)
            return "";
        var sb = new StringBuilder();
        for (var i = 0; i < pairs.Count; i++)
        {
            if (i > 0)
                sb.Append('&');
            sb.Append(Uri.EscapeDataString(pairs[i].Key))
                .Append('=')
                .Append(Uri.EscapeDataString(pairs[i].Value));
        }

        return sb.ToString();
    }
}
