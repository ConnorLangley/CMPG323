using Azure.Storage.Blobs;
using Bigone.Services; // Ensure this matches your folder structure
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Load configuration
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSingleton<PdfWatermarkService>();
builder.Services.AddSession(); // Add session support

// Register Blob Service Client
string blobConnectionString = builder.Configuration["AzureStorage:ConnectionString"];
if (string.IsNullOrEmpty(blobConnectionString))
{
    throw new ArgumentNullException(nameof(blobConnectionString), "Blob connection string is null or empty. Check your appsettings.json.");
}
builder.Services.AddSingleton(x => new BlobServiceClient(blobConnectionString));

// Get SQL Server connection string from configuration
var sqlConnectionString = builder.Configuration.GetConnectionString("SqlServerConnection");
builder.Services.AddSingleton(new SqlDbService(sqlConnectionString, builder.Configuration, new BlobServiceClient(blobConnectionString))); // Pass the BlobServiceClient

// Configure authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Index"; // Set the login path to your Index page
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();