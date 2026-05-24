using BerberApp.Application.Common.Interfaces;
using BerberApp.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BerberApp.Api.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
public class ExpensesController : BaseApiController
{
    private readonly IAppDbContext _context;

    public ExpensesController(IMediator mediator, IAppDbContext context) : base(mediator)
    {
        _context = context;
    }

    // ── Gider Listesi ─────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> GetExpenses(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken ct)
    {
        var query = _context.Expenses
            .Where(e => e.TenantId == TenantId && !e.IsDeleted);

        if (startDate.HasValue)
            query = query.Where(e => e.Date >= startDate.Value);
        if (endDate.HasValue)
            query = query.Where(e => e.Date < endDate.Value.AddDays(1));

        var expenses = await query
            .OrderByDescending(e => e.Date)
            .Select(e => new
            {
                e.Id,
                Date        = e.Date.ToString("yyyy-MM-dd"),
                e.Amount,
                e.Currency,
                e.Category,
                e.Description,
                e.Note,
                e.CreatedAt,
            })
            .ToListAsync(ct);

        return Success(expenses);
    }

    // ── Gider Ekle ────────────────────────────────────────────────────────────

    [HttpPost]
    public async Task<IActionResult> CreateExpense(
        [FromBody] CreateExpenseRequest request,
        CancellationToken ct)
    {
        if (request.Amount <= 0)
            return BadRequest(new { success = false, message = "Tutar 0'dan büyük olmalıdır." });

        if (!DateTime.TryParse(request.Date, out var parsedDate))
            return BadRequest(new { success = false, message = "Geçersiz tarih formatı." });

        var expense = new Expense
        {
            TenantId    = TenantId,
            Date        = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc),
            Amount      = request.Amount,
            Currency    = string.IsNullOrWhiteSpace(request.Currency) ? "TRY" : request.Currency.ToUpper(),
            Category    = request.Category ?? string.Empty,
            Description = request.Description,
            Note        = request.Note,
        };

        _context.Expenses.Add(expense);
        await _context.SaveChangesAsync(ct);

        return Created(new
        {
            expense.Id,
            Date        = expense.Date.ToString("yyyy-MM-dd"),
            expense.Amount,
            expense.Currency,
            expense.Category,
            expense.Description,
            expense.Note,
        });
    }

    // ── Gider Güncelle ────────────────────────────────────────────────────────

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateExpense(
        Guid id,
        [FromBody] CreateExpenseRequest request,
        CancellationToken ct)
    {
        var expense = await _context.Expenses
            .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == TenantId && !e.IsDeleted, ct);

        if (expense is null)
            return NotFound(new { success = false, message = "Gider bulunamadı." });

        if (!DateTime.TryParse(request.Date, out var parsedDate))
            return BadRequest(new { success = false, message = "Geçersiz tarih formatı." });

        expense.Date        = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
        expense.Amount      = request.Amount;
        expense.Currency    = string.IsNullOrWhiteSpace(request.Currency) ? "TRY" : request.Currency.ToUpper();
        expense.Category    = request.Category ?? string.Empty;
        expense.Description = request.Description;
        expense.Note        = request.Note;

        await _context.SaveChangesAsync(ct);
        return Success(new { expense.Id });
    }

    // ── Gider Sil ─────────────────────────────────────────────────────────────

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteExpense(Guid id, CancellationToken ct)
    {
        var expense = await _context.Expenses
            .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == TenantId && !e.IsDeleted, ct);

        if (expense is null)
            return NotFound(new { success = false, message = "Gider bulunamadı." });

        _context.Expenses.Remove(expense);
        await _context.SaveChangesAsync(ct);
        return NoContent();
    }

    // ── Request records ───────────────────────────────────────────────────────

    public record CreateExpenseRequest(
        string Date,
        decimal Amount,
        string? Currency,
        string? Category,
        string? Description,
        string? Note
    );
}
