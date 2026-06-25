namespace FormsDataManagementAPI.DTOs;

public record UpdateFormRequest(
    string Subject,
    string? Description,
    DateTime? DueDate,
    int? Priority,
    bool? Critical
);
