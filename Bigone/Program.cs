using Azure.Storage.Blobs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Register SQL DB Service
var sqlConnectionString = builder.Configuration.GetConnectionString("SqlServerConnection"); // Fix the name
builder.Services.AddSingleton(new SqlDbService(sqlConnectionString));

// Register Blob Service Client
string blobConnectionString = builder.Configuration.GetConnectionString("AzureStorage:ConnectionString"); // Fix the name
builder.Services.AddSingleton(new BlobServiceClient(blobConnectionString));

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

app.UseAuthorization();

app.MapRazorPages();

app.Run();
