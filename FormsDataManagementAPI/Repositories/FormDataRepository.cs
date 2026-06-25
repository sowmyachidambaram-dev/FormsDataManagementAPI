using FormsDataManagementAPI.Data;
using FormsDataManagementAPI.DTOs;
using FormsDataManagementAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace FormsDataManagementAPI.Repositories;

public class FormDataRepository : IFormDataRepository
{
    private readonly ApplicationDbContext _context;

    public FormDataRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FormData?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Forms
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted, cancellationToken);
    }

    public async Task<PagedResult<FormData>> ListAsync(FormListQuery query, CancellationToken cancellationToken = default)
    {
        var q = _context.Forms
            .AsNoTracking()
            .Where(f => !f.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.SubjectFilter))
            q = q.Where(f => f.Subject.Contains(query.SubjectFilter));

        if (query.CriticalFilter.HasValue)
            q = q.Where(f => f.Critical == query.CriticalFilter.Value);

        if (query.PriorityFilter.HasValue)
            q = q.Where(f => f.Priority == query.PriorityFilter.Value);

        var totalCount = await q.CountAsync(cancellationToken);

        var items = await q
            .OrderByDescending(f => f.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<FormData>(items, totalCount, query.Page, query.PageSize);
    }

    public async Task<FormData> CreateAsync(FormData form, CancellationToken cancellationToken = default)
    {
        await _context.Forms.AddAsync(form,cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return form;
    }

    public async Task<FormData> UpdateAsync(FormData form, CancellationToken cancellationToken = default)
    {
        // EF Core uses RowVersion as concurrency token in WHERE clause
        _context.Forms.Update(form);
        await _context.SaveChangesAsync(cancellationToken);
        return form;
    }

    public async Task<bool> SoftDeleteAsync(Guid id, string deletedBy, CancellationToken cancellationToken = default)
    {
        var form = await _context.Forms
            .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted, cancellationToken);

        if (form is null) return false;

        form.IsDeleted = true;
        form.DeletedAt = DateTime.UtcNow;
        form.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
