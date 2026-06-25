namespace FormsDataManagementAPI.DTOs;

public record CreateFormRequest(
    string Subject,
    string? Description,
    DateTime? DueDate,
    int? Priority,
    bool? Critical
);
