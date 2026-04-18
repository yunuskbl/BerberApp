using BerberApp.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BerberApp.Api.Middleware;

public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IAppDbContext dbContext)
    {
        // JWT'den tenant_id claim'ini oku
        var tenantClaim = context.User.Claims
            .FirstOrDefault(x => x.Type == "tenant_id");

        if (tenantClaim is not null && Guid.TryParse(tenantClaim.Value, out var tenantId))
        {
            var tenant = await dbContext.Tenants
                .FirstOrDefaultAsync(x => x.Id == tenantId && x.IsActive);

            if (tenant is not null)
                context.Items["TenantId"] = tenantId;
        }

        await _next(context);
    }
}