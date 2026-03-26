using System.Text;
using System.Security;
using System.Security.Claims;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using UniTestSystem.Authorization;
using UniTestSystem.Application;
using UniTestSystem.Application.Interfaces;
using UniTestSystem.Application.Models;
using UniTestSystem.Configuration;
using UniTestSystem.Domain;
using UniTestSystem.Infrastructure.Persistence;
using UniTestSystem.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

var authRateLimitOptions = builder.Configuration
    .GetSection("RateLimiting:AuthPolicies")
    .Get<SecurityRateLimitingOptions>() ?? new SecurityRateLimitingOptions();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = (context, _) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            var seconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));
            context.HttpContext.Response.Headers.RetryAfter = seconds.ToString(CultureInfo.InvariantCulture);
        }

        return ValueTask.CompletedTask;
    };

    options.AddPolicy("auth-login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ResolveClientIp(context),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = authRateLimitOptions.Login.PermitLimit,
                Window = TimeSpan.FromSeconds(authRateLimitOptions.Login.WindowSeconds),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = authRateLimitOptions.Login.QueueLimit,
                AutoReplenishment = true
            }));

    options.AddPolicy("auth-forgot", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ResolveClientIp(context),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = authRateLimitOptions.Forgot.PermitLimit,
                Window = TimeSpan.FromSeconds(authRateLimitOptions.Forgot.WindowSeconds),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = authRateLimitOptions.Forgot.QueueLimit,
                AutoReplenishment = true
            }));

    options.AddPolicy("auth-reset", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: ResolveClientIp(context),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = authRateLimitOptions.Reset.PermitLimit,
                Window = TimeSpan.FromSeconds(authRateLimitOptions.Reset.WindowSeconds),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = authRateLimitOptions.Reset.QueueLimit,
                AutoReplenishment = true
            }));
});

// JWT Authentication for API calls
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = "cookie";
        options.DefaultChallengeScheme = "cookie";
    })
    .AddCookie("cookie", o =>
    {
        o.LoginPath = "/auth/login";
        o.AccessDeniedPath = "/auth/denied";
        o.SlidingExpiration = true;
        o.Cookie.HttpOnly = true;
        o.Cookie.SameSite = SameSiteMode.Lax;
        o.Cookie.Name = "unitest_auth";
        o.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async context =>
            {
                var principal = context.Principal;
                var userId = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(userId))
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync("cookie");
                    return;
                }

                var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId);
                if (user == null || !user.IsActive)
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync("cookie");
                    return;
                }

                var roleClaim = principal?.FindFirst(ClaimTypes.Role)?.Value;
                if (!string.Equals(roleClaim, user.Role.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync("cookie");
                    return;
                }

                var sid = principal?.FindFirst("sid")?.Value;
                if (string.IsNullOrWhiteSpace(sid))
                    return;

                var session = await db.UserSessions.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == sid && x.UserId == userId);
                if (session == null || session.IsRevoked || (session.ExpiresAt.HasValue && session.ExpiresAt <= DateTime.UtcNow))
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync("cookie");
                }
            }
        };
    })
    .AddJwtBearer("jwt", options =>
    {
        var jwtKey = builder.Configuration["Jwt:Key"];
        if (string.IsNullOrEmpty(jwtKey) || jwtKey.Length < 32)
        {
            throw new SecurityException("JWT Key is missing or too short (min 32 chars).");
        }

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
        
        // Configure JWT for API endpoints
        options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                if (token != null)
                {
                    context.Token = token;
                }
                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                var principal = context.Principal;
                var userId = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(userId))
                {
                    context.Fail("Invalid token principal.");
                    return;
                }

                var jti = principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value
                          ?? principal?.FindFirst("jti")?.Value;
                if (string.IsNullOrWhiteSpace(jti))
                {
                    context.Fail("Missing token identifier.");
                    return;
                }

                var blacklist = context.HttpContext.RequestServices.GetRequiredService<ITokenBlacklistService>();
                if (await blacklist.IsRevokedAsync(jti))
                {
                    context.Fail("Token has been revoked.");
                    return;
                }

                var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId);
                if (user == null || !user.IsActive)
                {
                    context.Fail("User is inactive or not found.");
                    return;
                }

                var roleClaim = principal?.FindFirst(ClaimTypes.Role)?.Value;
                if (!string.Equals(roleClaim, user.Role.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    context.Fail("User role has changed.");
                    return;
                }

                var sid = principal?.FindFirst("sid")?.Value;
                if (string.IsNullOrWhiteSpace(sid))
                {
                    return;
                }

                var session = await db.UserSessions.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == sid && x.UserId == userId);
                if (session == null || session.IsRevoked || (session.ExpiresAt.HasValue && session.ExpiresAt <= DateTime.UtcNow))
                {
                    context.Fail("Session has been revoked or expired.");
                }
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    var bothSchemes = new[] { "cookie", "jwt" };
    
    options.DefaultPolicy = new AuthorizationPolicyBuilder(bothSchemes)
        .RequireAuthenticatedUser()
        .Build();

    options.AddPolicy("RequireAdmin", policy => policy.AddAuthenticationSchemes(bothSchemes).RequireRole(Role.Admin.ToString()));
    options.AddPolicy("RequireStaff", policy => policy.AddAuthenticationSchemes(bothSchemes).RequireRole(Role.Staff.ToString()));
    options.AddPolicy("RequireLecturer", policy => policy.AddAuthenticationSchemes(bothSchemes).RequireRole(Role.Lecturer.ToString()));
    options.AddPolicy("RequireStudent", policy => policy.AddAuthenticationSchemes(bothSchemes).RequireRole(Role.Student.ToString()));
    options.AddPolicy("RequireStaffOrAdmin", policy => policy.AddAuthenticationSchemes(bothSchemes).RequireRole(Role.Admin.ToString(), Role.Staff.ToString()));
    options.AddPolicy("RequireLecturerOrAdmin", policy => policy.AddAuthenticationSchemes(bothSchemes).RequireRole(Role.Admin.ToString(), Role.Lecturer.ToString()));
    options.AddPolicy("RequireLecturerOrStaffOrAdmin", policy => policy.AddAuthenticationSchemes(bothSchemes).RequireRole(Role.Admin.ToString(), Role.Staff.ToString(), Role.Lecturer.ToString()));

    // Dynamic Permission Policies
    foreach (var permission in PermissionCodes.All)
    {
        options.AddPolicy(permission, policy => 
        {
            policy.AddAuthenticationSchemes(bothSchemes);
            policy.Requirements.Add(new PermissionRequirement(permission));
        });
    }
});

builder.Services.AddScoped<IAuthorizationHandler, PermissionHandler>();

// Upload limit
builder.Services.Configure<FormOptions>(opt => { opt.MultipartBodyLengthLimit = 50L * 1024 * 1024; });

// Options
builder.Services.Configure<AppOptions>(builder.Configuration.GetSection("App"));
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); app.UseSwaggerUI();
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// ======= Middleware ghi audit cho các request đã đăng nhập =======
app.Use(async (ctx, next) =>
{
    await next();

    try
    {
        var audit = ctx.RequestServices.GetRequiredService<IAuditService>();
        var uid = ctx.User?.Identity?.IsAuthenticated == true
            ? ctx.User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "(unknown)"
            : "(anon)";

        if (ctx.Request.Method is "POST" or "PUT" or "DELETE"
            || ctx.Request.Path.StartsWithSegments("/settings")
            || ctx.Request.Path.StartsWithSegments("/roles"))
        {
            var entity = ctx.Request.Path.ToString();
            var action = $"{ctx.Request.Method} {entity}";
            await audit.LogAsync(uid, action, entityName: "(request)", entityId: entity, before: null, after: null);
        }
    }
    catch { /* nuốt lỗi audit */ }
});

// Root
app.MapGet("/", ctx =>
{
    if (ctx.User?.Identity?.IsAuthenticated == true)
    {
        var role = ctx.User.FindFirst(ClaimTypes.Role)?.Value;
        var target = role switch
        {
            nameof(Role.Admin) => "/Admin/Dashboard",
            nameof(Role.Staff) => "/Staff/Dashboard",
            nameof(Role.Lecturer) => "/Lecturer/Dashboard",
            nameof(Role.Student) => "/MyTests",
            _ => "/Home/Index"
        };

        ctx.Response.Redirect(target);
        return Task.CompletedTask;
    }

    ctx.Response.Redirect("/auth/login");
    return Task.CompletedTask;
});

// Routes
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapControllers();

// Folders & Seed
Directory.CreateDirectory(Path.Combine(app.Environment.WebRootPath ?? "wwwroot", "uploads", "logo"));
using (var scope = app.Services.CreateScope())
{
    await Seeder.RunAsync(scope.ServiceProvider);

    // Ensure default permissions exist
    var permSvc = scope.ServiceProvider.GetRequiredService<IPermissionService>();
    await permSvc.EnsureDefaultAsync();
}

app.Run();

static string ResolveClientIp(HttpContext context)
{
    return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
