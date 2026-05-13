namespace EventsParser.Parsing;

public sealed class MuziekladderSelectors
{
    public string RootScope { get; set; } = "main";

    public string GigRow { get; set; } = "div.row.gig-row.gig";

    public string DetailContainer { get; set; } = ".detail_container";

    public string GigAnchorHrefContains { get; set; } = "/gig/";

    public string GigPageRoot { get; set; } = "main.event_full";

    public string Headline { get; set; } = ".headline";

    public string DateInfo { get; set; } = ".date-info";

    public string LocationInfo { get; set; } = ".location-info";

    public string LocationBlock { get; set; } = "div.location";

    public string DescriptionWithItemProp { get; set; } = "p.description[itemprop='description']";

    public string EventLink { get; set; } = "a.event_link";

    public string CityName { get; set; } = ".city_name";

    public string CountryName { get; set; } = ".country_name";

    public string NextPageLink { get; set; } = "a.next-page[href]";

    public static MuziekladderSelectors Default { get; } = new();
}
