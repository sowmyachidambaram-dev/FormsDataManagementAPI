using FormsDataManagementAPI.DTOs;
using FormsDataManagementAPI.Models;

namespace FormsDataManagementAPI.Repositories;

public interface IFormDataRepository
{
    Task<FormData?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<FormData>> ListAsync(FormListQuery query, CancellationToken cancellationToken = default);
    Task<FormData> CreateAsync(FormData form, CancellationToken cancellationToken = default);
    Task<FormData> UpdateAsync(FormData form, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(Guid id, string deletedBy, CancellationToken cancellationToken = default);
}
