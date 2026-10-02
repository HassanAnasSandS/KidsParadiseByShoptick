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
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Secrets.json", optional: true, reloadOnChange: true);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 200 * 1024 * 1024;
});
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 200 * 1024 * 1024;
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
builder.Services.Configure<MetaSocialOptions>(options =>
{
    builder.Configuration.GetSection(MetaSocialOptions.SectionName).Bind(options);
    var siteBase = builder.Configuration["Seo:SiteBaseUrl"]?.TrimEnd('/');
    if (!string.IsNullOrWhiteSpace(siteBase))
        options.SiteBaseUrl = siteBase;
});
builder.Services.Configure<TikTokSocialOptions>(options =>
{
    builder.Configuration.GetSection(TikTokSocialOptions.SectionName).Bind(options);
    var siteBase = builder.Configuration["Seo:SiteBaseUrl"]?.TrimEnd('/');
    if (!string.IsNullOrWhiteSpace(siteBase) && string.IsNullOrWhiteSpace(options.RedirectUri))
        options.RedirectUri = $"{siteBase}/api/admin/tiktok/oauth/callback";
    if (string.IsNullOrWhiteSpace(options.PostMode))
        options.PostMode = TikTokSocialOptions.DirectPost;
    options.PostMode = TikTokSocialOptions.NormalizePostMode(options.PostMode);
    if (string.IsNullOrWhiteSpace(options.PrivacyLevel))
        options.PrivacyLevel = "PUBLIC_TO_EVERYONE";
    // Scopes are derived at Connect time from the Admin-selected post mode.
});
builder.Services.Configure<PinterestSocialOptions>(options =>
{
    builder.Configuration.GetSection(PinterestSocialOptions.SectionName).Bind(options);
    var siteBase = builder.Configuration["Seo:SiteBaseUrl"]?.TrimEnd('/');
    if (!string.IsNullOrWhiteSpace(siteBase) && string.IsNullOrWhiteSpace(options.RedirectUri))
        options.RedirectUri = $"{siteBase}/api/admin/pinterest/oauth/callback";
    if (string.IsNullOrWhiteSpace(options.Scopes))
        options.Scopes = "boards:read,boards:write,pins:write,user_accounts:read";
    if (string.IsNullOrWhiteSpace(options.DefaultBoardName))
        options.DefaultBoardName = "Kids Paradise Toys";
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
builder.Services.AddSingleton<SocialPostQueue>();
builder.Services.AddSingleton<ISocialPostQueue>(sp => sp.GetRequiredService<SocialPostQueue>());
builder.Services.AddScoped<ISocialPostNotificationService, SignalRSocialPostNotificationService>();
builder.Services.Configure<HostOptions>(options =>
    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore);
// Token auto-refresh disabled for now (was throwing TaskCanceledException on debug stop).
// builder.Services.AddHostedService<SocialTokenMaintenanceHostedService>();
builder.Services.AddHostedService<SocialPostBackgroundService>();
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
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Never cache the SPA shell — stale index.html + new hashed assets = blank page.
        if (ctx.File.Name.Equals("index.html", StringComparison.OrdinalIgnoreCase))
        {
            ctx.Context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
            ctx.Context.Response.Headers.Pragma = "no-cache";
            ctx.Context.Response.Headers.Expires = "0";
        }
        else if (ctx.Context.Request.Path.StartsWithSegments("/assets"))
        {
            // Hashed Vite assets are immutable.
            ctx.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
        }
    },
});

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(publishedPath),
    RequestPath = "",
});

app.UseAuthentication();
app.UseAuthorization();
app.UseResponseCaching();

app.MapControllers();
app.MapHub<AdminOrderHub>("/hubs/admin-orders");

// TikTok URL ownership — serve EXACT downloaded signature file from wwwroot (not a hardcoded short code).
IResult ServeTikTokVerifyFile(HttpContext ctx, string fileName)
{
    ctx.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
    ctx.Response.Headers.Pragma = "no-cache";

    var wwwroot = Path.Combine(app.Environment.ContentRootPath, "wwwroot", fileName);
    var published = Path.Combine(
        builder.Configuration["FileStorage:BasePath"] ?? Path.Combine(app.Environment.ContentRootPath, ".."),
        fileName);
    published = Path.GetFullPath(published);

    foreach (var path in new[] { wwwroot, published })
    {
        if (System.IO.File.Exists(path))
            return Results.File(path, "text/plain; charset=utf-8");
    }

    // Fallback exact TikTok download body if files missing
    return Results.Text(
        "tiktok-developers-site-verification=sMNBLtnQopDSTvuIg1d4DGa3Hq97e9Sb",
        "text/plain; charset=utf-8");
}

app.MapGet("/tiktoksMNBLtnQopDSTvuIg1d4DGa3Hq97e9Sb.txt", (HttpContext ctx) =>
    ServeTikTokVerifyFile(ctx, "tiktoksMNBLtnQopDSTvuIg1d4DGa3Hq97e9Sb.txt"));
app.MapGet("/tiktoksMNBLtnQopDSTvulg1d4DGa3Hq97e9Sb.txt", (HttpContext ctx) =>
    ServeTikTokVerifyFile(ctx, "tiktoksMNBLtnQopDSTvulg1d4DGa3Hq97e9Sb.txt"));
app.MapGet("/tiktok-developers-site-verification.txt", (HttpContext ctx) =>
    ServeTikTokVerifyFile(ctx, "tiktok-developers-site-verification.txt"));

// Crawler-friendly product HTML: inject title/description/JSON-LD into SPA shell before JS runs.
app.MapGet("/product/{id:int}", async (
    int id,
    IToyService toyService,
    IOptions<SeoOptions> seoOptions,
    IWebHostEnvironment env,
    CancellationToken cancellationToken) =>
{
    var indexPath = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "index.html");
    if (!System.IO.File.Exists(indexPath))
        return Results.NotFound();

    var html = await System.IO.File.ReadAllTextAsync(indexPath, cancellationToken);
    var toy = await toyService.GetByIdAsync(id, cancellationToken);
    if (toy is null)
        return Results.Content(html, "text/html; charset=utf-8");

    var seo = seoOptions.Value;
    var root = seo.SiteBaseUrl.TrimEnd('/');
    var image = toy.ImageUrls.FirstOrDefault();
    if (!string.IsNullOrWhiteSpace(image)
        && !image.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        && !image.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
    {
        image = root + (image.StartsWith('/') ? image : "/" + image);
    }

    html = KidsParadiseByShoptick.Application.Helpers.ProductSeoHelper.InjectIntoIndexHtml(
        html, toy, seo, image ?? seo.DefaultOgImageUrl);

    return Results.Content(html, "text/html; charset=utf-8");
});

// Crawler-friendly static pages: correct title/description/canonical per route
// (otherwise every SPA route serves the home-page meta and Google under-indexes them).
foreach (var (route, pageSeo) in KidsParadiseByShoptick.Application.Helpers.PageSeoHelper.StaticPages)
{
    app.MapGet(route, async (
        HttpContext http,
        IOptions<SeoOptions> seoOptions,
        IWebHostEnvironment env,
        CancellationToken cancellationToken) =>
    {
        var indexPath = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "index.html");
        if (!System.IO.File.Exists(indexPath))
            return Results.NotFound();

        var html = await System.IO.File.ReadAllTextAsync(indexPath, cancellationToken);
        html = KidsParadiseByShoptick.Application.Helpers.PageSeoHelper.InjectIntoIndexHtml(
            html, pageSeo, seoOptions.Value);
        http.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
        http.Response.Headers.Pragma = "no-cache";
        return Results.Content(html, "text/html; charset=utf-8");
    });
}

// Crawler-friendly category pages with the category name in title/description.
app.MapGet("/category/{id:int}", async (
    int id,
    ICategoryService categoryService,
    IOptions<SeoOptions> seoOptions,
    IWebHostEnvironment env,
    CancellationToken cancellationToken) =>
{
    var indexPath = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "index.html");
    if (!System.IO.File.Exists(indexPath))
        return Results.NotFound();

    var html = await System.IO.File.ReadAllTextAsync(indexPath, cancellationToken);
    var category = await categoryService.GetByIdAsync(id, cancellationToken);
    if (category is not null)
    {
        var pageSeo = KidsParadiseByShoptick.Application.Helpers.PageSeoHelper.ForCategory(id, category.Name);
        html = KidsParadiseByShoptick.Application.Helpers.PageSeoHelper.InjectIntoIndexHtml(
            html, pageSeo, seoOptions.Value);
    }
    return Results.Content(html, "text/html; charset=utf-8");
});

app.MapFallbackToFile("index.html");

await DbSeeder.SeedAsync(app.Services);

app.Run();
