namespace RequestsBL.Validation;

internal static class EnumParsing
{
    // Names only (case-insensitive): unlike Enum.TryParse, numbers such as "1" are rejected.
    public static TEnum? ParseOptional<TEnum>(
        string? value, string field, Dictionary<string, string[]> errors, bool camelCaseNames = false)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        var names = Enum.GetNames<TEnum>();
        var match = names.FirstOrDefault(name => name.Equals(trimmed, StringComparison.OrdinalIgnoreCase));
        if (match is not null)
        {
            return Enum.Parse<TEnum>(match);
        }

        var allowed = string.Join(", ", camelCaseNames ? names.Select(ToCamelCase) : names);
        errors[field] = [$"'{trimmed}' is not a valid {field}. Allowed values: {allowed}."];
        return null;
    }

    private static string ToCamelCase(string name) => char.ToLowerInvariant(name[0]) + name[1..];
}
