using Employee_Survey.Application;
using Employee_Survey.Domain;
using Employee_Survey.Infrastructure;
using Employee_Survey.Infrastructure.Persistence;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;

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
        o.Cookie.Name = "emp_survey_auth";
    })
    .AddJwtBearer("jwt", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "default_secret_key_1234567890123456"))
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

builder.Services.AddAuthorization();

// Upload limit
builder.Services.Configure<FormOptions>(opt => { opt.MultipartBodyLengthLimit = 50L * 1024 * 1024; });

// Repos (Switched to EF Core)
builder.Services.AddScoped<IRepository<User>, EfRepository<User>>();
builder.Services.AddScoped<IRepository<Team>, EfRepository<Team>>();
builder.Services.AddScoped<IRepository<Question>, EfRepository<Question>>();
builder.Services.AddScoped<IRepository<Test>, EfRepository<Test>>();
builder.Services.AddScoped<IRepository<Assignment>, EfRepository<Assignment>>();
builder.Services.AddScoped<IRepository<Session>, EfRepository<Session>>();
builder.Services.AddScoped<IRepository<Feedback>, EfRepository<Feedback>>();
builder.Services.AddScoped<IRepository<RolePermissionMapping>, EfRepository<RolePermissionMapping>>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
builder.Services.AddScoped<IRepository<Department>, EfRepository<Department>>();
builder.Services.AddScoped<IRepository<Option>, EfRepository<Option>>();
builder.Services.AddScoped<IRepository<UserAnswer>, EfRepository<UserAnswer>>();
builder.Services.AddScoped<IRepository<Result>, EfRepository<Result>>();

// Services
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IQuestionService, QuestionService>();
builder.Services.AddScoped<IQuestionExcelService, QuestionExcelService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<TestService>();
builder.Services.AddScoped<AssignmentService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<PasswordResetService>();

builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<IAuditReaderService, AuditReaderService>();
builder.Services.AddScoped<ITestGenerationService, TestGenerationService>();

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

// ======= NEW: Middleware ghi audit cơ bản cho các request đã đăng nhập =======
app.Use(async (ctx, next) =>
{
    await next();

    try
    {
        var audit = ctx.RequestServices.GetRequiredService<IAuditService>();
        var uid = ctx.User?.Identity?.IsAuthenticated == true
            ? ctx.User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "(unknown)"
            : "(anon)";

        // Chỉ ghi các action thay đổi (POST/PUT/DELETE) hoặc các URL nhạy cảm
        if (ctx.Request.Method is "POST" or "PUT" or "DELETE"
            || ctx.Request.Path.StartsWithSegments("/settings")
            || ctx.Request.Path.StartsWithSegments("/roles"))
        {
            var entity = ctx.Request.Path.ToString();
            var action = $"{ctx.Request.Method} {entity}";
            await audit.LogAsync(uid, action, entityId: entity, before: null, after: null);
        }
    }
    catch { /* nuốt lỗi audit để không chặn flow */ }
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
