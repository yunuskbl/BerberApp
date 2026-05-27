using BerberApp.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BerberApp.Api.Middleware;

public class BranchContextMiddleware
{
    private readonly RequestDelegate _next;

    public BranchContextMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, IAppDbContext db)
    {
        var branchIdHeader = context.Request.Headers["X-Branch-Id"].FirstOrDefault();

        if (!string.IsNullOrEmpty(branchIdHeader) &&
            Guid.TryParse(branchIdHeader, out var branchId) &&
            context.User.Identity?.IsAuthenticated == true)
        {
            var tenantIdClaim = context.User.FindFirst("tenant_id")?.Value;
            if (Guid.TryParse(tenantIdClaim, out var parentTenantId))
            {
                var isValid = await db.Tenants.AnyAsync(t =>
                    t.Id == branchId &&
                    t.ParentTenantId == parentTenantId &&
                    !t.IsDeleted);

                if (isValid)
                {
                    var identity = (ClaimsIdentity)context.User.Identity!;
                    var existing = identity.FindFirst("tenant_id");
                    if (existing != null) identity.RemoveClaim(existing);
                    identity.AddClaim(new Claim("tenant_id", branchId.ToString()));
                }
            }
        }

        await _next(context);
    }
}
