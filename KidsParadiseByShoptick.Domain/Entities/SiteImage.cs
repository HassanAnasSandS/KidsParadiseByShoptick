namespace KidsParadiseByShoptick.Domain.Entities;

public class SiteImage : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public string? ImagePath { get; set; }
    public string? Title { get; set; }
    public string? Subtitle { get; set; }
    public string? CtaText { get; set; }
    public string? LinkUrl { get; set; }
    public string? TitleColor { get; set; }
    public string? SubtitleColor { get; set; }
    public string? CtaColor { get; set; }
    public int SortOrder { get; set; }
}
