using RequestsBL.Dtos;
using RequestsBL.Exceptions;
using RequestsDAL.Entities;

namespace RequestsBL.Validation;

public record ValidStatusUpdate(RequestStatus NewStatus, byte[] RowVersion, string ChangedBy);

/// <summary>Validates the input of a status change (not the transition itself, see StatusTransitions).</summary>
public static class StatusUpdateValidator
{
    public const int MaxChangedByLength = 100;

    // SQL Server rowversion is always 8 bytes.
    private const int RowVersionLength = 8;

    /// <exception cref="ValidationException">One or more fields are invalid; all errors are reported together.</exception>
    public static ValidStatusUpdate Validate(UpdateStatusRequestDto dto)
    {
        var errors = new Dictionary<string, string[]>();

        var status = EnumParsing.ParseOptional<RequestStatus>(dto.Status, "status", errors);
        if (status is null && !errors.ContainsKey("status"))
        {
            errors["status"] = ["Status is required."];
        }

        var rowVersion = ParseRowVersion(dto.RowVersion, errors);

        var changedBy = dto.ChangedBy?.Trim();
        if (string.IsNullOrEmpty(changedBy))
        {
            errors["changedBy"] = ["changedBy is required."];
        }
        else if (changedBy.Length > MaxChangedByLength)
        {
            errors["changedBy"] = [$"changedBy must be at most {MaxChangedByLength} characters."];
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        return new ValidStatusUpdate(status!.Value, rowVersion!, changedBy!);
    }

    private static byte[]? ParseRowVersion(string? value, Dictionary<string, string[]> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors["rowVersion"] = ["rowVersion is required. Use the value returned by GET /api/requests/{id}."];
            return null;
        }

        var buffer = new byte[RowVersionLength];
        if (!Convert.TryFromBase64String(value.Trim(), buffer, out var written) || written != RowVersionLength)
        {
            errors["rowVersion"] = ["rowVersion is not valid. Use the value returned by GET /api/requests/{id}."];
            return null;
        }

        return buffer;
    }
}
