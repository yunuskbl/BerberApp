using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BerberApp.Api.Middleware;

/// <summary>
/// /swagger yollarını JWT + SuperAdmin rolü ile korur.
/// </summary>
public class SwaggerAuthMiddleware(RequestDelegate next, IConfiguration config)
{
    private readonly RequestDelegate _next = next;
    private readonly string _jwtKey      = config["Jwt:Key"]!;
    private readonly string _issuer      = config["Jwt:Issuer"]!;
    private readonly string _audience    = config["Jwt:Audience"]!;

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/swagger"))
        {
            await _next(context);
            return;
        }

        var principal = TryGetPrincipal(context);
        if (principal?.IsInRole("SuperAdmin") == true)
        {
            await _next(context);
            return;
        }

        // Token yoksa veya yetersiz rol → basit login formu göster
        if (context.Request.Method == "POST" && context.Request.HasFormContentType)
        {
            var token = context.Request.Form["token"].ToString();
            var p = TryValidateToken(token);
            if (p?.IsInRole("SuperAdmin") == true)
            {
                context.Response.Cookies.Append("swagger_token", token,
                    new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict });
                context.Response.Redirect("/swagger");
                return;
            }
        }

        context.Response.StatusCode = 401;
        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync(LoginHtml());
    }

    private ClaimsPrincipal? TryGetPrincipal(HttpContext context)
    {
        // 1) Authorization header
        var auth = context.Request.Headers.Authorization.ToString();
        if (auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return TryValidateToken(auth["Bearer ".Length..].Trim());

        // 2) Cookie
        if (context.Request.Cookies.TryGetValue("swagger_token", out var cookie))
            return TryValidateToken(cookie);

        return null;
    }

    private ClaimsPrincipal? TryValidateToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        try
        {
            var handler = new JwtSecurityTokenHandler();
            return handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer           = true,
                ValidateAudience         = true,
                ValidateLifetime         = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer              = _issuer,
                ValidAudience            = _audience,
                IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey))
            }, out _);
        }
        catch { return null; }
    }

    private static string LoginHtml() => """
        <!DOCTYPE html>
        <html lang="tr">
        <head>
          <meta charset="utf-8"/>
          <title>Swagger — Giriş</title>
          <style>
            *{box-sizing:border-box;margin:0;padding:0}
            body{font-family:system-ui,sans-serif;background:#0f172a;display:flex;align-items:center;justify-content:center;min-height:100vh}
            .card{background:#1e293b;border-radius:12px;padding:40px;width:360px;box-shadow:0 20px 60px rgba(0,0,0,.5)}
            h2{color:#f1f5f9;font-size:1.25rem;margin-bottom:8px}
            p{color:#94a3b8;font-size:.85rem;margin-bottom:24px}
            label{display:block;color:#cbd5e1;font-size:.8rem;margin-bottom:6px}
            textarea{width:100%;background:#0f172a;border:1px solid #334155;border-radius:8px;color:#f1f5f9;font-size:.78rem;padding:10px;resize:vertical;min-height:90px;outline:none}
            textarea:focus{border-color:#6366f1}
            button{margin-top:16px;width:100%;background:#6366f1;color:#fff;border:none;border-radius:8px;padding:12px;font-size:.9rem;cursor:pointer;font-weight:600}
            button:hover{background:#4f46e5}
          </style>
        </head>
        <body>
          <div class="card">
            <h2>Swagger API</h2>
            <p>Yalnızca SuperAdmin erişebilir.<br/>JWT token'ınızı girin.</p>
            <form method="POST">
              <label>Bearer Token</label>
              <textarea name="token" placeholder="eyJhbGci..."></textarea>
              <button type="submit">Giriş Yap</button>
            </form>
          </div>
        </body>
        </html>
        """;
}
