using System.Text;
using KidsParadiseByShoptick.Application;
using KidsParadiseByShoptick.Application.Interfaces;
using KidsParadiseByShoptick.Application.Options;
using KidsParadiseByShoptick.APIs.Hubs;
using KidsParadiseByShoptick.APIs.Middleware;
using KidsParadiseByShoptick.APIs.Services;
using KidsParadiseByShoptick.Infrastructure;
using KidsParadiseByShoptick.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Secrets.json", optional: true, reloadOnChange: true);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
builder.Services.AddMemoryCache();
builder.Services.Configure<SeoOptions>(builder.Configuration.GetSection(SeoOptions.SectionName));
builder.Services.Configure<GoogleOAuthOptions>(options =>
{
    builder.Configuration.GetSection(GoogleOAuthOptions.SectionName).Bind(options);
    var siteBase = builder.Configuration["Seo:SiteBaseUrl"]?.TrimEnd('/');
    if (!string.IsNullOrWhiteSpace(siteBase))
        options.RedirectUri = $"{siteBase}/api/admin/youtube/oauth/callback";
});
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    // Only forward client IP and HTTPS scheme — NOT host (prevents www↔apex rewrite bugs).
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});
builder.Services.AddResponseCaching();
builder.Services.AddOpenApi();
builder.Services.AddSignalR();
builder.Services.AddScoped<IOrderNotificationService, SignalROrderNotificationService>();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
});

builder.Services.Configure<Microsoft.AspNetCore.Authorization.AuthorizationOptions>(options =>
{
    options.AddPolicy("Admin", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim(System.Security.Claims.ClaimTypes.Name);
    });
});

// Map any authenticated admin JWT user to Admin role
builder.Services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
{
    var original = options.Events?.OnTokenValidated;
    options.Events ??= new JwtBearerEvents();
    options.Events.OnTokenValidated = async context =>
    {
        if (original is not null) await original(context);
        if (context.Principal?.Identity?.IsAuthenticated == true)
        {
            var identity = (System.Security.Claims.ClaimsIdentity)context.Principal.Identity;
            if (!identity.HasClaim(c => c.Type == System.Security.Claims.ClaimTypes.Role))
                identity.AddClaim(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Admin"));
        }
    };
});

var app = builder.Build();

var publishedPath = builder.Configuration["FileStorage:BasePath"]
    ?? Path.Combine(Directory.GetCurrentDirectory(), "..", "KidsParadiseByShoptick.Published");
publishedPath = Path.GetFullPath(publishedPath);
Directory.CreateDirectory(publishedPath);

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

// 1) Trust proxy/IIS forwarded headers (HTTPS/host behind reverse proxy)
app.UseForwardedHeaders();

// 2) Force canonical domain: http→https, wrong host→canonical (301)
app.UseMiddleware<CanonicalHostMiddleware>();

if (!app.Environment.IsDevelopment())
    app.UseHsts();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(publishedPath),
    RequestPath = ""
});

app.UseAuthentication();
app.UseAuthorization();
app.UseResponseCaching();

app.MapControllers();
app.MapHub<AdminOrderHub>("/hubs/admin-orders");
app.MapFallbackToFile("index.html");

await DbSeeder.SeedAsync(app.Services);

app.Run();
