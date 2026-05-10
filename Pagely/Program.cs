using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NotionClone.BLL.Services;
using NotionClone.DAL;
using NotionClone.DAL.Repositories;
using NotionClone.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy  = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "NotionClone API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT Bearer token in the format: Bearer {token}"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var jwtSecret = builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT secret is missing.");
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPageRepository, PageRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPageService, PageService>();
builder.Services.AddScoped<ITaskService, TaskService>();

var app = builder.Build();
var frontendPath = Path.Combine(app.Environment.ContentRootPath, "frontend");
var frontendProvider = new PhysicalFileProvider(frontendPath);

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseDefaultFiles(new DefaultFilesOptions
{
    FileProvider = frontendProvider,
    RequestPath = ""
});
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = frontendProvider,
    RequestPath = ""
});
app.UseAuthentication();
app.UseAuthorization();

await SeedDatabaseAsync(app);

app.MapControllers();
app.MapGet("/", () => Results.Redirect("/index.html"));

app.Run();

static async Task SeedDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();

    var testEmail = "test@test.com";
    var existingUser = await dbContext.Users.FirstOrDefaultAsync(user => user.Email == testEmail);
    if (existingUser is not null)
    {
        return;
    }

    var userId = Guid.NewGuid();
    dbContext.Users.Add(new NotionClone.DAL.Entities.AppUser
    {
        Id = userId,
        Email = testEmail,
        Name = "Test User",
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test1234!"),
        CreatedAt = DateTime.UtcNow
    });

    var now = DateTime.UtcNow;
    dbContext.Pages.AddRange(
        new NotionClone.DAL.Entities.Page
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = "Dashboard",
            Content = string.Empty,
            Icon = "\uD83D\uDCCA",
            Status = "Todo",
            Order = 0,
            CreatedAt = now,
            UpdatedAt = now
        },
        new NotionClone.DAL.Entities.Page
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = "Calendar",
            Content = string.Empty,
            Icon = "\uD83D\uDDD3",
            Status = "Todo",
            Order = 1,
            CreatedAt = now,
            UpdatedAt = now
        });

    await dbContext.SaveChangesAsync();
}