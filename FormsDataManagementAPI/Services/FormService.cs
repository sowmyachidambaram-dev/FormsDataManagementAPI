using FormsDataManagementAPI.Authorization;
using FormsDataManagementAPI.DTOs;
using FormsDataManagementAPI.Models;
using FormsDataManagementAPI.Repositories;

namespace FormsDataManagementAPI.Services;

public class FormService : IFormService
{
    private readonly IFormDataRepository _repository;
    private readonly IFormAuthorizationService _authorizationService;
    private readonly ILogger<FormService> _logger;

    public FormService(
        IFormDataRepository repository,
        IFormAuthorizationService authorizationService,
        ILogger<FormService> logger)
    {
        _repository = repository;
        _authorizationService = authorizationService;
        _logger = logger;
    }

    public async Task<FormDataResponse> CreateAsync(CreateFormRequest request, string userId, CancellationToken cancellationToken = default)
    {
        var form = new FormData
        {
            Id = Guid.NewGuid(),
            Subject = request.Subject.Trim(),
            Description = request.Description?.Trim(),
            DueDate = request.DueDate,
            Priority = request.Priority,
            Critical = request.Critical,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId
        };

        var created = await _repository.CreateAsync(form, cancellationToken);
        _logger.LogInformation("Form {FormId} created by {UserId}", created.Id, userId);
        return MapToResponse(created);
    }

    public async Task<FormDataResponse?> GetByIdAsync(Guid id, string userId, CancellationToken cancellationToken = default)
    {
        var form = await _repository.GetByIdAsync(id, cancellationToken);
        if (form is null) return null;

        // Treat auth denial as not-found to avoid information disclosure
        if (!await _authorizationService.UserCanViewAsync(userId, id, cancellationToken))
        {
            _logger.LogWarning("User {UserId} attempted to view form {FormId} without permission", userId, id);
            return null;
        }

        return MapToResponse(form);
    }

    public async Task<PagedResult<FormDataResponse>> ListAsync(FormListQuery query, string userId, CancellationToken cancellationToken = default)
    {
        var result = await _repository.ListAsync(query, cancellationToken);
        var mappedItems = result.Items.Select(MapToResponse).ToList();
        return new PagedResult<FormDataResponse>(mappedItems, result.TotalCount, result.Page, result.PageSize);
    }

    public async Task<FormDataResponse?> UpdateAsync(Guid id, UpdateFormRequest request, string userId, CancellationToken cancellationToken = default)
    {
        var form = await _repository.GetByIdAsync(id, cancellationToken);
        if (form is null) return null;

        if (!await _authorizationService.UserCanModifyAsync(userId, id, cancellationToken))
        {
            _logger.LogWarning("User {UserId} attempted to modify form {FormId} without permission", userId, id);
            return null;
        }

        form.Subject = request.Subject.Trim();
        form.Description = request.Description?.Trim();
        form.DueDate = request.DueDate;
        form.Priority = request.Priority;
        form.Critical = request.Critical;
        form.UpdatedAt = DateTime.UtcNow;

        var updated = await _repository.UpdateAsync(form, cancellationToken);
        _logger.LogInformation("Form {FormId} updated by {UserId}", id, userId);
        return MapToResponse(updated);
    }

    public async Task<bool> DeleteAsync(Guid id, string userId, CancellationToken cancellationToken = default)
    {
        if (!await _authorizationService.UserCanModifyAsync(userId, id, cancellationToken))
        {
            _logger.LogWarning("User {UserId} attempted to delete form {FormId} without permission", userId, id);
            return false;
        }

        var deleted = await _repository.SoftDeleteAsync(id, userId, cancellationToken);
        if (deleted)
            _logger.LogInformation("Form {FormId} soft-deleted by {UserId}", id, userId);
        return deleted;
    }

    private static FormDataResponse MapToResponse(FormData form) => new(
        form.Id,
        form.Subject,
        form.Description,
        form.DueDate,
        form.Priority,
        form.Critical,
        form.CreatedAt,
        form.UpdatedAt,
        form.CreatedBy
    );
}
