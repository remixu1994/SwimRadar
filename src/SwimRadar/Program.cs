using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using SwimRadar.Components;
using SwimRadar.Components.Account;
using SwimRadar.Data;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMudServices();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
Console.WriteLine("Connection string: " + connectionString);
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
builder.Services.AddTransient<IVideoService, VideoService>();
var app = builder.Build();

// // Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment())
// {
//     app.UseMigrationsEndPoint();
// }
// else
// {
//     app.UseExceptionHandler("/Error", createScopeForErrors: true);
//     // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
//     // app.UseHsts();
// }

// app.UseHttpsRedirection();
app.UseMigrationsEndPoint();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.UseStaticFiles();

app.MapGet("/video/preview/{id:guid}", async Task<IResult> (Guid id, IVideoService videoService) =>
{
    SwimVideo? video = await videoService.GetVideoAsync(id);
    if (video is null ||
        video.PreviewStatus != VideoPreviewStatus.Ready ||
        string.IsNullOrWhiteSpace(video.PreviewFilePath) ||
        !File.Exists(video.PreviewFilePath))
    {
        return Results.NotFound();
    }

    return Results.File(
        video.PreviewFilePath,
        contentType: "video/mp4",
        enableRangeProcessing: true);
}).RequireAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();
