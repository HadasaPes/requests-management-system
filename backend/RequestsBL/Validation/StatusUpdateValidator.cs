using RequestsBL.Dtos;
using RequestsBL.Exceptions;
using RequestsDAL.Entities;

namespace RequestsBL.Validation;

public record ValidStatusUpdate(RequestStatus NewStatus, byte[] RowVersion, string ChangedBy);

public record ValidBulkStatusUpdate(RequestStatus NewStatus, string ChangedBy, IReadOnlyList<ValidBulkItem> Items);

public record ValidBulkItem(int Id, byte[] RowVersion);

/// <summary>Validates the input of a status change (not the transition itself, see StatusTransitions).</summary>
public static class StatusUpdateValidator
{
    public const int MaxChangedByLength = 100;
    public const int MaxBulkItems = 100;

    // SQL Server rowversion is always 8 bytes.
    private const int RowVersionLength = 8;

    /// <exception cref="ValidationException">One or more fields are invalid; all errors are reported together.</exception>
    public static ValidStatusUpdate Validate(UpdateStatusRequestDto dto)
    {
        var errors = new Dictionary<string, string[]>();

        var status = ParseStatus(dto.Status, errors);
        var rowVersion = ParseRowVersion(dto.RowVersion, "rowVersion", errors);
        var changedBy = ParseChangedBy(dto.ChangedBy, errors);

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        return new ValidStatusUpdate(status!.Value, rowVersion!, changedBy!);
    }

    /// <summary>Validates the whole bulk request; if anything is invalid, no item is processed.</summary>
    /// <exception cref="ValidationException">One or more fields are invalid; all errors are reported together.</exception>
    public static ValidBulkStatusUpdate ValidateBulk(BulkStatusUpdateRequestDto dto)
    {
        var errors = new Dictionary<string, string[]>();

        var status = ParseStatus(dto.Status, errors);
        var changedBy = ParseChangedBy(dto.ChangedBy, errors);
        var items = new List<ValidBulkItem>();

        if (dto.Items is null || dto.Items.Count == 0 || dto.Items.Count > MaxBulkItems)
        {
            // Items are not checked one by one when the count is wrong, to keep the error list short.
            errors["items"] = [$"items must contain between 1 and {MaxBulkItems} items."];
        }
        else
        {
            for (var i = 0; i < dto.Items.Count; i++)
            {
                var item = dto.Items[i];
                if (item is null)
                {
                    errors[$"items[{i}]"] = ["Item is required."];
                    continue;
                }

                if (item.Id is null)
                {
                    errors[$"items[{i}].id"] = ["id is required."];
                }

                var rowVersion = ParseRowVersion(item.RowVersion, $"items[{i}].rowVersion", errors);
                if (item.Id is { } id && rowVersion is not null)
                {
                    items.Add(new ValidBulkItem(id, rowVersion));
                }
            }

            var duplicateIds = dto.Items
                .Where(i => i?.Id is not null)
                .GroupBy(i => i!.Id!.Value)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            if (duplicateIds.Count > 0)
            {
                errors["items"] = [$"Each id may appear only once. Duplicate ids: {string.Join(", ", duplicateIds)}."];
            }
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        return new ValidBulkStatusUpdate(status!.Value, changedBy!, items);
    }

    private static RequestStatus? ParseStatus(string? value, Dictionary<string, string[]> errors)
    {
        var status = EnumParsing.ParseOptional<RequestStatus>(value, "status", errors);
        if (status is null && !errors.ContainsKey("status"))
        {
            errors["status"] = ["Status is required."];
        }

        return status;
    }

    private static string? ParseChangedBy(string? value, Dictionary<string, string[]> errors)
    {
        var changedBy = value?.Trim();
        if (string.IsNullOrEmpty(changedBy))
        {
            errors["changedBy"] = ["changedBy is required."];
            return null;
        }

        if (changedBy.Length > MaxChangedByLength)
        {
            errors["changedBy"] = [$"changedBy must be at most {MaxChangedByLength} characters."];
            return null;
        }

        return changedBy;
    }

    private static byte[]? ParseRowVersion(string? value, string field, Dictionary<string, string[]> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors[field] = [$"{field} is required. Use the value returned by GET /api/requests/{{id}}."];
            return null;
        }

        var buffer = new byte[RowVersionLength];
        if (!Convert.TryFromBase64String(value.Trim(), buffer, out var written) || written != RowVersionLength)
        {
            errors[field] = [$"{field} is not valid. Use the value returned by GET /api/requests/{{id}}."];
            return null;
        }

        return buffer;
    }
}
