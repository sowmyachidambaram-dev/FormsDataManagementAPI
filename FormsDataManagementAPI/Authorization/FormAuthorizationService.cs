namespace FormsDataManagementAPI.Authorization;

public class FormAuthorizationService : IFormAuthorizationService
{
    public Task<bool> UserCanViewAsync(string userId, Guid formId, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<bool> UserCanModifyAsync(string userId, Guid formId, CancellationToken cancellationToken = default)
        => Task.FromResult(true);
}
