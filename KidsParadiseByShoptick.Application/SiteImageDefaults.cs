namespace KidsParadiseByShoptick.Application;

public record SiteImageDefinition(string Key, string Label, string Group, string DefaultUrl, int SortOrder);

public static class SiteImageDefaults
{
    public static readonly IReadOnlyList<SiteImageDefinition> All =
    [
        new("hero_slide_1", "Hero Slide 1", "Hero Slider", "/hero/slide-1.jpg", 1),
        new("hero_slide_2", "Hero Slide 2", "Hero Slider", "/hero/slide-2.jpg", 2),
        new("hero_slide_3", "Hero Slide 3", "Hero Slider", "/hero/slide-3.jpg", 3),
        new("hero_slide_4", "Hero Slide 4", "Hero Slider", "/hero/slide-4.jpg", 4),
        new("banner_new_arrivals", "New Arrivals Banner", "Home Banners", "/hero/slide-1.jpg", 5),
        new("banner_perfect_gifts", "Perfect Gifts Banner", "Home Banners", "/hero/slide-3.jpg", 6),
        new("shop_header", "Shop Page Header", "Pages", "/hero/slide-1.jpg", 7),
    ];

    public static string GetDefaultUrl(string key)
        => All.FirstOrDefault(x => x.Key == key)?.DefaultUrl ?? string.Empty;

    public static SiteImageDefinition? GetDefinition(string key)
        => All.FirstOrDefault(x => x.Key == key);
}
