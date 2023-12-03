using System.Security.Claims;
using Google.Apis.Auth.OAuth2;
using FirebaseAdmin;
using Projekt_I.Services;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using MatBlazor;
using Blazorise;
using Blazorise.Bootstrap;
using Blazorise.Icons.FontAwesome;

var builder = WebApplication.CreateBuilder(args);

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .Build();

var filePath = configuration["GOOGLE_APPLICATION_CREDENTIALS"];
Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", filePath);
// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder
    .Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
    })
    .AddCookie("Cookies")
    .AddGoogle(googleOptions =>
    {
        googleOptions.ClientId = configuration["Google:Id"];
        googleOptions.ClientSecret = configuration["Google:Secret"];
        googleOptions.CallbackPath = "/home";
        googleOptions.Scope.Add("profile");
        googleOptions.Events.OnCreatingTicket = context =>
        {
            string picuri = context.User.GetProperty("picture").GetString();

            context.Identity.AddClaim(new Claim("picture", picuri));

            return Task.CompletedTask;
        };
    });

builder.Services.AddMatBlazor();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<HttpContextAccessor>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<FirestoreDb>(provider =>
{
    return FirestoreDb.Create("projekti-56b36");
});
builder.Services.AddScoped<UserContextService>();
builder.Services.AddScoped<FirestoreService>();
builder.Services.AddMatToaster(config =>
{
    config.Position = MatToastPosition.TopRight;
    config.PreventDuplicates = true;
    config.NewestOnTop = true;
    config.ShowCloseButton = true;
    config.MaximumOpacity = 95;
    config.VisibleStateDuration = 3000;
});
builder.Services
    .AddBlazorise(options =>
    {
        options.Immediate = true;
    })
    .AddBootstrapProviders()
    .AddFontAwesomeIcons();

var credentials = GoogleCredential.FromFile(filePath);
var firebaseApp = FirebaseApp.Create(new AppOptions
{
    Credential = credentials,
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseCookiePolicy();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
