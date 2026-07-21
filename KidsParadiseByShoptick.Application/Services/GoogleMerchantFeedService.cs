using System.Globalization;
using System.Text;
using System.Xml;
using KidsParadiseByShoptick.Application.Interfaces;
using KidsParadiseByShoptick.Application.Options;
using KidsParadiseByShoptick.Domain.Entities;
using KidsParadiseByShoptick.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace KidsParadiseByShoptick.Application.Services;

public class GoogleMerchantFeedService : IGoogleMerchantFeedService
{
    private const string GoogleNs = "http://base.google.com/ns/1.0";

    private readonly IToyRepository _toys;
    private readonly IFileStorageService _fileStorage;
    private readonly SeoOptions _seo;

    public GoogleMerchantFeedService(
        IUnitOfWork unitOfWork,
        IFileStorageService fileStorage,
        IOptions<SeoOptions> seoOptions)
    {
        _toys = unitOfWork.Toys;
        _fileStorage = fileStorage;
        _seo = seoOptions.Value;
    }

    public async Task<string> GenerateFeedXmlAsync(string baseUrl, CancellationToken cancellationToken = default)
    {
        var root = baseUrl.TrimEnd('/');
        var toys = await _toys.GetAllAvailableWithDetailsAsync(cancellationToken);

        var settings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = new UTF8Encoding(false),
            Async = false,
        };

        using var ms = new MemoryStream();
        using (var writer = XmlWriter.Create(ms, settings))
        {
            writer.WriteStartDocument();
            writer.WriteStartElement("rss");
            writer.WriteAttributeString("version", "2.0");
            writer.WriteAttributeString("xmlns", "g", null, GoogleNs);

            writer.WriteStartElement("channel");
            writer.WriteElementString("title", _seo.SiteName);
            writer.WriteElementString("link", root + "/");
            writer.WriteElementString("description", _seo.DefaultDescription);

            foreach (var toy in toys)
                WriteItem(writer, toy, root);

            writer.WriteEndElement(); // channel
            writer.WriteEndElement(); // rss
            writer.WriteEndDocument();
        }

        return Encoding.UTF8.GetString(ms.ToArray());
    }

    void WriteItem(XmlWriter writer, Toy toy, string root)
    {
        var imageUrls = ToySocialCaptionBuilder.BuildAbsoluteImageUrls(toy, root, _fileStorage.GetPublicUrl);
        if (imageUrls.Count == 0)
            return;

        var title = Truncate(Sanitize(toy.Name), 150);
        var description = Truncate(BuildDescription(toy), 5000);
        var link = $"{root}/product/{toy.Id}";
        var brand = string.IsNullOrWhiteSpace(_seo.MerchantBrand) ? "Kids Paradise" : _seo.MerchantBrand.Trim();
        var condition = string.IsNullOrWhiteSpace(_seo.MerchantCondition) ? "used" : _seo.MerchantCondition.Trim();

        writer.WriteStartElement("item");
        WriteG(writer, "id", toy.Id.ToString(CultureInfo.InvariantCulture));
        WriteG(writer, "title", title);
        WriteG(writer, "description", description);
        WriteG(writer, "link", link);
        WriteG(writer, "image_link", imageUrls[0]);
        foreach (var extra in imageUrls.Skip(1).Take(10))
            WriteG(writer, "additional_image_link", extra);

        WriteG(writer, "availability", toy.IsSold ? "out_of_stock" : "in_stock");
        WriteG(writer, "condition", condition);
        WriteG(writer, "brand", brand);
        WriteG(writer, "identifier_exists", "false");
        WriteG(writer, "price", FormatMoney(toy.Price));

        if (toy.SalePrice is > 0 && toy.SalePrice < toy.Price)
            WriteG(writer, "sale_price", FormatMoney(toy.SalePrice.Value));

        if (!string.IsNullOrWhiteSpace(toy.Category?.Name))
            WriteG(writer, "product_type", Sanitize(toy.Category.Name));

        writer.WriteStartElement("g", "shipping", GoogleNs);
        WriteG(writer, "country", string.IsNullOrWhiteSpace(_seo.Region) ? "PK" : _seo.Region);
        WriteG(writer, "service", "Standard");
        WriteG(writer, "price", FormatMoney(_seo.MerchantDefaultShippingPkr));
        writer.WriteEndElement();

        writer.WriteEndElement(); // item
    }

    static void WriteG(XmlWriter writer, string localName, string value)
    {
        writer.WriteStartElement("g", localName, GoogleNs);
        writer.WriteString(value);
        writer.WriteEndElement();
    }

    static string BuildDescription(Toy toy)
    {
        var category = string.IsNullOrWhiteSpace(toy.Category?.Name) ? "Kids toys" : toy.Category.Name.Trim();
        return $"{toy.Name.Trim()}. Excellent working condition. Category: {category}. Unique kids toy from Kids Paradise by Shoptick. Karachi & Pakistan delivery.";
    }

    static string FormatMoney(decimal amount)
        => string.Create(CultureInfo.InvariantCulture, $"{amount:0.00} PKR");

    static string Sanitize(string value)
        => value.Replace('\r', ' ').Replace('\n', ' ').Trim();

    static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..max];
}
