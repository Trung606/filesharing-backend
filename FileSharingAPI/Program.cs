using FileSharingAPI.Data;
using FileSharingAPI.Repositories;
using Microsoft.EntityFrameworkCore;
using FileSharingAPI.Services;

var options = new WebApplicationOptions
{
    Args = args,
    // Disable configuration reload on change to prevent inotify limit crashes on Render
    ContentRootPath = Directory.GetCurrentDirectory()
};
var builder = WebApplication.CreateBuilder(options);

// 1. ADD CORS POLICY HERE
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173", // Keep local testing alive
                "https://file-sharing-fe-coat.onrender.com" // Your live frontend URL
              )
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IFileRepository, FileRepository>();
builder.Services.AddScoped<IStorageService, CloudinaryStorageService>();
builder.Services.AddHostedService<FileCleanupService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 2. INJECT CORS MIDDLEWARE HERE (Must be before Authorization)
app.UseCors("AllowFrontend");

app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

app.Run();