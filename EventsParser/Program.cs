using EventsParser;
using EventsParser.Parsing;

const string DefaultOut = "events.json";

var argsList = args.ToList();
var scheduleMode = TakeFlag(ref argsList, "--schedule");
var runOnStart = TakeFlag(ref argsList, "--run-on-start");
var maxGigs = TakeOptionalPositiveInt(ref argsList, "--max-gigs");

var site = Environment.GetEnvironmentVariable("SITE") ?? "muziekladder";
ISiteProfile profile = site.ToLowerInvariant() switch
{
    "muziekladder" => new MuziekladderSiteProfile(),
    _ => throw new ArgumentException($"Unknown site '{site}'. Supported values: muziekladder.")
};

var agendaUrl = argsList.Count > 0 ? argsList[0] : profile.DefaultAgendaUrl;
var outputPath = argsList.Count > 1 ? argsList[1] : DefaultOut;
var delayMs = int.TryParse(Environment.GetEnvironmentVariable("DELAY_MS"), out var d) ? d : 100;

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
http.DefaultRequestHeaders.TryAddWithoutValidation(
    "User-Agent",
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

var progress = new Progress<string>(Console.WriteLine);
var scraper = new SiteScraper(http, profile);

try
{
    if (scheduleMode)
    {
        await Scheduler.RunLoopAsync(
            runOnStart,
            ct => ParseRun.RunOnceAsync(scraper, agendaUrl, delayMs, maxGigs, outputPath, progress, ct),
            cts.Token);
    }
    else
    {
        await ParseRun.RunOnceAsync(scraper, agendaUrl, delayMs, maxGigs, outputPath, progress, cts.Token);
    }
}
catch (OperationCanceledException)
{
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex);
    Environment.ExitCode = 1;
}

static bool TakeFlag(ref List<string> list, string flag)
{
    var i = list.IndexOf(flag);
    if (i < 0)
        return false;
    list.RemoveAt(i);
    return true;
}

static int TakeOptionalPositiveInt(ref List<string> list, string flag)
{
    var i = list.IndexOf(flag);
    if (i < 0 || i + 1 >= list.Count)
        return 0;
    if (!int.TryParse(list[i + 1], out var v) || v <= 0)
        return 0;
    list.RemoveAt(i + 1);
    list.RemoveAt(i);
    return v;
}
