namespace FormsDataManagementAPI.DTOs;

public record FormListQuery(
    int Page = 1,
    int PageSize = 20,
    string? SubjectFilter = null,
    bool? CriticalFilter = null,
    int? PriorityFilter = null
);
