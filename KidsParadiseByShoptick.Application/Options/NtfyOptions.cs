namespace KidsParadiseByShoptick.Application.Options;

public class NtfyOptions
{
    public const string SectionName = "Ntfy";

    public bool Enabled { get; set; } = true;

    public string TopicUrl { get; set; } = "https://ntfy.sh/KidsParadise";
}
