using FormsDataManagementAPI.DTOs;

namespace FormsDataManagementAPI.Services;

public interface IFormService
{
    Task<FormDataResponse> CreateAsync(CreateFormRequest request, string userId, CancellationToken cancellationToken = default);
    Task<FormDataResponse?> GetByIdAsync(Guid id, string userId, CancellationToken cancellationToken = default);
    Task<PagedResult<FormDataResponse>> ListAsync(FormListQuery query, string userId, CancellationToken cancellationToken = default);
    Task<FormDataResponse?> UpdateAsync(Guid id, UpdateFormRequest request, string userId, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, string userId, CancellationToken cancellationToken = default);
}
