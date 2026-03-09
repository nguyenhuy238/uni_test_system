using System.Text;
using System.Security;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using UniTestSystem.Application;
using UniTestSystem.Application.Interfaces;
using UniTestSystem.Application.Models;
using UniTestSystem.Domain;
using UniTestSystem.Infrastructure.Persistence;
using UniTestSystem.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DB
builder.Services.AddHttpContextAccessor();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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

// Repos (Switched to EF Core)
builder.Services.AddScoped<IRepository<Student>, EfRepository<Student>>();
builder.Services.AddScoped<IRepository<Lecturer>, EfRepository<Lecturer>>();
builder.Services.AddScoped<IRepository<User>, EfRepository<User>>();
builder.Services.AddScoped<IRepository<StudentClass>, EfRepository<StudentClass>>();
builder.Services.AddScoped<IRepository<Question>, EfRepository<Question>>();
builder.Services.AddScoped<IRepository<Test>, EfRepository<Test>>();
builder.Services.AddScoped<IRepository<Assessment>, EfRepository<Assessment>>();
builder.Services.AddScoped<IRepository<Session>, EfRepository<Session>>();
builder.Services.AddScoped<IRepository<Feedback>, EfRepository<Feedback>>();
builder.Services.AddScoped<IRepository<RolePermissionMapping>, EfRepository<RolePermissionMapping>>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
builder.Services.AddScoped<IRepository<Faculty>, EfRepository<Faculty>>();
builder.Services.AddScoped<IRepository<Option>, EfRepository<Option>>();
builder.Services.AddScoped<IRepository<UserAnswer>, EfRepository<UserAnswer>>();
builder.Services.AddScoped<IRepository<Result>, EfRepository<Result>>();

// NEW academic repos
builder.Services.AddScoped<IRepository<Course>, EfRepository<Course>>();
builder.Services.AddScoped<IRepository<Enrollment>, EfRepository<Enrollment>>();
builder.Services.AddScoped<IRepository<ExamSchedule>, EfRepository<ExamSchedule>>();
builder.Services.AddScoped<IRepository<Transcript>, EfRepository<Transcript>>();

// Services
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IQuestionService, QuestionService>();
builder.Services.AddScoped<IQuestionExcelService, QuestionExcelService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<TestService>();
builder.Services.AddScoped<AssessmentService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<PasswordResetService>();

builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<IAuditReaderService, AuditReaderService>();
builder.Services.AddScoped<ITestGenerationService, TestGenerationService>();
builder.Services.AddScoped<IAcademicService, AcademicService>();
builder.Services.AddScoped<IBulkImportService, BulkImportService>();

// Options
builder.Services.Configure<AppOptions>(builder.Configuration.GetSection("App"));
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection("Email"));

builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
builder.Services.AddSingleton<INotificationService, NotificationService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
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
