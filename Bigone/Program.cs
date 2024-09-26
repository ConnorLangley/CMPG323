using Azure.Storage.Blobs;
using Bigone.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSingleton<PdfWatermarkService>();
builder.Services.AddSession(); // Add this line

// Register Blob Service Client
string blobConnectionString = builder.Configuration["AzureStorage:ConnectionString"];
if (string.IsNullOrEmpty(blobConnectionString))
{
    throw new ArgumentNullException(nameof(blobConnectionString), "Blob connection string is null or empty. Check your appsettings.json.");
}
builder.Services.AddSingleton(x => new BlobServiceClient(blobConnectionString));

var connectionString = builder.Configuration.GetConnectionString("SqlServerConnection");
builder.Services.AddSingleton(new SqlDbService(connectionString));

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
