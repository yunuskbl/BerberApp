using BerberApp.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace BerberApp.Api.Authorization;

/// <summary>
/// Requires specific plan type(s) to access endpoint
/// Usage: [RequirePlan(PlanType.Standard, PlanType.Full)]
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequirePlanAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly PlanType[] _requiredPlans;

    public RequirePlanAttribute(params PlanType[] requiredPlans)
    {
        _requiredPlans = requiredPlans;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        // Must be authenticated
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // Get user's plan from claims
        var planClaim = user.FindFirst("plan_type");
        if (planClaim == null)
        {
            context.Result = new ForbidResult();
            return;
        }

        // Yeni isimlerle parse et; eski token'lar için geriye dönük uyumluluk
        PlanType userPlan;
        if (!Enum.TryParse<PlanType>(planClaim.Value, out userPlan))
        {
            userPlan = planClaim.Value switch
            {
                "Basic"    => PlanType.Baslangic,
                "Standard" => PlanType.Profesyonel,
                "Full"     => PlanType.Premium,
                _          => (PlanType)0
            };
            if (userPlan == 0) { context.Result = new ForbidResult(); return; }
        }

        // Check if user's plan is in required plans
        if (!_requiredPlans.Contains(userPlan))
        {
            context.Result = new ObjectResult(new
            {
                success = false,
                message = "Bu özellik sizin paket seviyesinde mevcut değildir."
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        await Task.CompletedTask;
    }
}