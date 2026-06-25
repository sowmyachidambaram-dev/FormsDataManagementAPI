namespace FormsDataManagementAPI.Authorization;

public interface IFormAuthorizationService
{
    Task<bool> UserCanViewAsync(string userId, Guid formId, CancellationToken cancellationToken = default);
    Task<bool> UserCanModifyAsync(string userId, Guid formId, CancellationToken cancellationToken = default);
}
