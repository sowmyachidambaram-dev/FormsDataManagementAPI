using FluentAssertions;
using FormsDataManagementAPI.Data;
using FormsDataManagementAPI.DTOs;
using FormsDataManagementAPI.Models;
using FormsDataManagementAPI.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FormsDataManagementAPI.Tests.Repositories;

public class FormDataRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly FormDataRepository _repository;

    public FormDataRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _repository = new FormDataRepository(_context);
    }

    public void Dispose() => _context.Dispose();

    // ── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_returns_form_when_exists()
    {
        var form = await SeedFormAsync("Find me");

        var result = await _repository.GetByIdAsync(form.Id);

        result.Should().NotBeNull();
        result!.Subject.Should().Be("Find me");
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_when_not_found()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_for_soft_deleted_form()
    {
        var form = await SeedFormAsync("Deleted", isDeleted: true);

        var result = await _repository.GetByIdAsync(form.Id);

        result.Should().BeNull();
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_persists_form()
    {
        var form = BuildForm("New form");

        var created = await _repository.CreateAsync(form);

        var stored = await _context.Forms.FindAsync(created.Id);
        stored.Should().NotBeNull();
        stored!.Subject.Should().Be("New form");
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_persists_changes()
    {
        var form = await SeedFormAsync("Before");
        form.Subject = "After";
        form.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(form);

        var stored = await _context.Forms.AsNoTracking().FirstAsync(f => f.Id == form.Id);
        stored.Subject.Should().Be("After");
        stored.UpdatedAt.Should().NotBeNull();
    }

    // ── SoftDelete ────────────────────────────────────────────────────────────

    [Fact]
    public async Task SoftDeleteAsync_marks_form_as_deleted()
    {
        var form = await SeedFormAsync("To be deleted");

        var result = await _repository.SoftDeleteAsync(form.Id, "user-1");

        result.Should().BeTrue();
        var stored = await _context.Forms.FindAsync(form.Id);
        stored!.IsDeleted.Should().BeTrue();
        stored.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task SoftDeleteAsync_returns_false_when_not_found()
    {
        var result = await _repository.SoftDeleteAsync(Guid.NewGuid(), "user-1");
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SoftDeleteAsync_returns_false_when_already_deleted()
    {
        var form = await SeedFormAsync("Already gone", isDeleted: true);

        var result = await _repository.SoftDeleteAsync(form.Id, "user-1");

        result.Should().BeFalse();
    }

    // ── List ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListAsync_excludes_soft_deleted()
    {
        await SeedFormAsync("Active");
        await SeedFormAsync("Deleted", isDeleted: true);

        var result = await _repository.ListAsync(new FormListQuery());

        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task ListAsync_filters_by_subject()
    {
        await SeedFormAsync("Alpha task");
        await SeedFormAsync("Beta task");

        var result = await _repository.ListAsync(new FormListQuery(SubjectFilter: "Alpha"));

        result.Items.Should().HaveCount(1);
        result.Items[0].Subject.Should().Be("Alpha task");
    }

    [Fact]
    public async Task ListAsync_paginates_correctly()
    {
        for (var i = 0; i < 5; i++)
            await SeedFormAsync($"Form {i}");

        var page1 = await _repository.ListAsync(new FormListQuery(Page: 1, PageSize: 2));
        var page2 = await _repository.ListAsync(new FormListQuery(Page: 2, PageSize: 2));

        page1.Items.Should().HaveCount(2);
        page2.Items.Should().HaveCount(2);
        page1.TotalCount.Should().Be(5);
        page1.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task ListAsync_filters_by_critical()
    {
        await SeedFormAsync("Critical form", critical: true);
        await SeedFormAsync("Normal form", critical: false);

        var result = await _repository.ListAsync(new FormListQuery(CriticalFilter: true));

        result.Items.Should().HaveCount(1);
        result.Items[0].Subject.Should().Be("Critical form");
    }

    [Fact]
    public async Task ListAsync_filters_by_priority()
    {
        await SeedFormAsync("High priority", priority: 9);
        await SeedFormAsync("Low priority", priority: 2);

        var result = await _repository.ListAsync(new FormListQuery(PriorityFilter: 9));

        result.Items.Should().HaveCount(1);
        result.Items[0].Subject.Should().Be("High priority");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<FormData> SeedFormAsync(string subject, bool isDeleted = false, bool? critical = null, int? priority = null)
    {
        var form = BuildForm(subject, isDeleted, critical, priority);
        _context.Forms.Add(form);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        return form;
    }

    private static FormData BuildForm(string subject, bool isDeleted = false, bool? critical = null, int? priority = null) => new()
    {
        Id = Guid.NewGuid(),
        Subject = subject,
        CreatedBy = "seeder",
        CreatedAt = DateTime.UtcNow,
        IsDeleted = isDeleted,
        Critical = critical,
        Priority = priority
    };
}
