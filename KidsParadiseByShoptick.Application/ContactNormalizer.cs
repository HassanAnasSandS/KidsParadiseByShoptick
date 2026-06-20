namespace KidsParadiseByShoptick.Application;

public static class ContactNormalizer
{
    public static string NormalizeWhatsapp(string input)
    {
        var digits = new string(input.Where(char.IsDigit).ToArray());
        return string.IsNullOrEmpty(digits) ? input.Trim() : digits;
    }
}
