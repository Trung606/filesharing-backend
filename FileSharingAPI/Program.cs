using FileSharingAPI.Data;
using FileSharingAPI.Repositories;
using Microsoft.EntityFrameworkCore;
using FileSharingAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. ADD CORS POLICY HERE
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVueFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173") // Hieu's Vue dev server
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
builder.Services.AddScoped<IStorageService, LocalStorageService>();
builder.Services.AddHostedService<FileCleanupService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 2. INJECT CORS MIDDLEWARE HERE (Must be before Authorization)
app.UseCors("AllowVueFrontend");

app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

app.Run();