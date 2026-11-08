namespace RequestsBL.Dtos;

public class UpdateStatusRequestDto
{
    public string? Status { get; set; }

    public string? RowVersion { get; set; }

    public string? ChangedBy { get; set; }
}
