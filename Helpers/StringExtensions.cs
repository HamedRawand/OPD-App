namespace OPDClinic.Helpers;

public static class StringExtensions
{
    /// <summary>Returns null if the string is null or whitespace-only; otherwise returns the original string.</summary>
    public static string? NullIfEmpty(this string? s) => string.IsNullOrWhiteSpace(s) ? null : s;
}
