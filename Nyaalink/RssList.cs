namespace Nyaalink;

using System.Xml.Serialization;
using System.Collections.Generic;

[XmlRoot(ElementName = "link", Namespace = "http://www.w3.org/2005/Atom")]
public class Link
{
    [XmlAttribute(AttributeName = "href")] public string Href { get; set; }
    [XmlAttribute(AttributeName = "rel")] public string Rel { get; set; }
    [XmlAttribute(AttributeName = "type")] public string Type { get; set; }
}

[XmlRoot(ElementName = "guid")]
public class Guid
{
    [XmlAttribute(AttributeName = "isPermaLink")]
    public string IsPermaLink { get; set; }

    [XmlText] public string Text { get; set; }
}

[XmlRoot(ElementName = "item")]
public class Item
{
    [XmlElement(ElementName = "title")] public string Title { get; set; }
    [XmlElement(ElementName = "link")] public string Link { get; set; }
    [XmlElement(ElementName = "guid")] public Guid Guid { get; set; }
    [XmlElement(ElementName = "pubDate")] public string PubDate { get; set; }

    [XmlElement(ElementName = "seeders", Namespace = "https://nyaa.si/xmlns/nyaa")]
    public string Seeders { get; set; }

    [XmlElement(ElementName = "leechers", Namespace = "https://nyaa.si/xmlns/nyaa")]
    public string Leechers { get; set; }

    [XmlElement(ElementName = "downloads", Namespace = "https://nyaa.si/xmlns/nyaa")]
    public string Downloads { get; set; }

    [XmlElement(ElementName = "infoHash", Namespace = "https://nyaa.si/xmlns/nyaa")]
    public string InfoHash { get; set; }

    [XmlElement(ElementName = "categoryId", Namespace = "https://nyaa.si/xmlns/nyaa")]
    public string CategoryId { get; set; }

    [XmlElement(ElementName = "category", Namespace = "https://nyaa.si/xmlns/nyaa")]
    public string Category { get; set; }

    [XmlElement(ElementName = "size", Namespace = "https://nyaa.si/xmlns/nyaa")]
    public string Size { get; set; }

    [XmlElement(ElementName = "comments", Namespace = "https://nyaa.si/xmlns/nyaa")]
    public string Comments { get; set; }

    [XmlElement(ElementName = "trusted", Namespace = "https://nyaa.si/xmlns/nyaa")]
    public string Trusted { get; set; }

    [XmlElement(ElementName = "remake", Namespace = "https://nyaa.si/xmlns/nyaa")]
    public string Remake { get; set; }

    [XmlElement(ElementName = "description")]
    public string Description { get; set; }

    public ulong? ParseId()
    {
        var lastSlash = Guid.Text.LastIndexOf('/');
        return ulong.TryParse(Guid.Text[(lastSlash + 1)..], out var parsed)
            ? parsed
            : null;
    }
}

[XmlRoot(ElementName = "channel")]
public class FeedChannel
{
    [XmlElement(ElementName = "title")] public string Title { get; set; }

    [XmlElement(ElementName = "description")]
    public string Description { get; set; }

    [XmlElement(ElementName = "link")] public List<string> Link { get; set; }
    [XmlElement(ElementName = "item")] public List<Item> Item { get; set; }
}

[XmlRoot(ElementName = "rss")]
public class Rss
{
    [XmlElement(ElementName = "channel")] public FeedChannel FeedChannel { get; set; }

    [XmlAttribute(AttributeName = "atom", Namespace = "http://www.w3.org/2000/xmlns/")]
    public string Atom { get; set; }

    [XmlAttribute(AttributeName = "nyaa", Namespace = "http://www.w3.org/2000/xmlns/")]
    public string Nyaa { get; set; }

    [XmlAttribute(AttributeName = "version")]
    public string Version { get; set; }
}