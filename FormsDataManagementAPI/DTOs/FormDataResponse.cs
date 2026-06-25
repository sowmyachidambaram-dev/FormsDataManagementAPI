namespace FormsDataManagementAPI.DTOs;

public record FormDataResponse(
    Guid Id,
    string Subject,
    string? Description,
    DateTime? DueDate,
    int? Priority,
    bool? Critical,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string CreatedBy
);
