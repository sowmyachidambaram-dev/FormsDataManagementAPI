using FluentAssertions;
using FormsDataManagementAPI.Authorization;
using FormsDataManagementAPI.DTOs;
using FormsDataManagementAPI.Models;
using FormsDataManagementAPI.Services;
using FormsDataManagementAPI.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FormsDataManagementAPI.Tests.Services;

public class FormServiceTests
{
    private readonly Mock<IFormDataRepository> _repoMock = new();
    private readonly Mock<IFormAuthorizationService> _authMock = new();
    private readonly FormService _service;

    public FormServiceTests()
    {
        _authMock.Setup(a => a.UserCanViewAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);
        _authMock.Setup(a => a.UserCanModifyAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        _service = new FormService(_repoMock.Object, _authMock.Object, NullLogger<FormService>.Instance);
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_persists_and_returns_response()
    {
        var request = new CreateFormRequest("Test subject", "desc", null, 3, true);

        _repoMock.Setup(r => r.CreateAsync(It.IsAny<FormData>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((FormData f, CancellationToken _) => f);

        var result = await _service.CreateAsync(request, "user-1");

        result.Subject.Should().Be("Test subject");
        result.Description.Should().Be("desc");
        result.Priority.Should().Be(3);
        result.Critical.Should().BeTrue();
        result.CreatedBy.Should().Be("user-1");
        _repoMock.Verify(r => r.CreateAsync(It.IsAny<FormData>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_trims_whitespace_from_subject()
    {
        var request = new CreateFormRequest("  padded  ", null, null, null, null);
        _repoMock.Setup(r => r.CreateAsync(It.IsAny<FormData>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((FormData f, CancellationToken _) => f);

        var result = await _service.CreateAsync(request, "user-1");

        result.Subject.Should().Be("padded");
    }

    // ── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_returns_null_when_not_found()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((FormData?)null);

        var result = await _service.GetByIdAsync(Guid.NewGuid(), "user-1");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_when_user_not_authorized()
    {
        var formId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(formId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(MakeForm(formId));
        _authMock.Setup(a => a.UserCanViewAsync("user-1", formId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);

        var result = await _service.GetByIdAsync(formId, "user-1");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_returns_response_when_found_and_authorized()
    {
        var formId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(formId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(MakeForm(formId));

        var result = await _service.GetByIdAsync(formId, "user-1");

        result.Should().NotBeNull();
        result!.Id.Should().Be(formId);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_returns_null_when_form_not_found()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((FormData?)null);

        var result = await _service.UpdateAsync(Guid.NewGuid(), new UpdateFormRequest("X", null, null, null, null), "user-1");

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_returns_null_when_user_not_authorized()
    {
        var formId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(formId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(MakeForm(formId));
        _authMock.Setup(a => a.UserCanModifyAsync("user-1", formId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);

        var result = await _service.UpdateAsync(formId, new UpdateFormRequest("X", null, null, null, null), "user-1");

        result.Should().BeNull();
        _repoMock.Verify(r => r.UpdateAsync(It.IsAny<FormData>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_updates_and_returns_response()
    {
        var formId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetByIdAsync(formId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(MakeForm(formId));
        _repoMock.Setup(r => r.UpdateAsync(It.IsAny<FormData>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((FormData f, CancellationToken _) => f);

        var result = await _service.UpdateAsync(formId, new UpdateFormRequest("New subject", "new desc", null, 7, false), "user-1");

        result.Should().NotBeNull();
        result!.Subject.Should().Be("New subject");
        result.Description.Should().Be("new desc");
        result.Priority.Should().Be(7);
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_returns_false_when_not_authorized()
    {
        var formId = Guid.NewGuid();
        _authMock.Setup(a => a.UserCanModifyAsync("user-1", formId, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);

        var result = await _service.DeleteAsync(formId, "user-1");

        result.Should().BeFalse();
        _repoMock.Verify(r => r.SoftDeleteAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_returns_false_when_form_not_found()
    {
        _repoMock.Setup(r => r.SoftDeleteAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(false);

        var result = await _service.DeleteAsync(Guid.NewGuid(), "user-1");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_returns_true_when_successful()
    {
        var formId = Guid.NewGuid();
        _repoMock.Setup(r => r.SoftDeleteAsync(formId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(true);

        var result = await _service.DeleteAsync(formId, "user-1");

        result.Should().BeTrue();
    }

    // ── List ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListAsync_maps_repository_results()
    {
        var formId = Guid.NewGuid();
        var pagedResult = new PagedResult<FormData>(
            new[] { MakeForm(formId) }, TotalCount: 1, Page: 1, PageSize: 20);

        _repoMock.Setup(r => r.ListAsync(It.IsAny<FormListQuery>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(pagedResult);

        var result = await _service.ListAsync(new FormListQuery(), "user-1");

        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
        result.Items[0].Id.Should().Be(formId);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static FormData MakeForm(Guid id) => new()
    {
        Id = id,
        Subject = "Original subject",
        CreatedBy = "creator",
        CreatedAt = DateTime.UtcNow
    };
}
