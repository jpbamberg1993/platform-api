using Microsoft.EntityFrameworkCore;
using PlatformService.Data;
using PlatformService.SyncDataServices.Http;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
if (builder.Environment.IsProduction())
{
    Console.WriteLine("Using postgresql.");
    builder.Services.AddDbContext<AppDbContext>(opt =>
        opt.UseNpgsql(builder.Configuration.GetConnectionString("PlatformsConnectionString")));
}
else
{
    Console.WriteLine("Using in memory database.");
    builder.Services
        .AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase("InMem"));
}

// DI
builder.Services.AddScoped<IPlatformRepo, PlatformRepo>();
builder.Services.AddHttpClient<ICommandDataClient, CommandDataClient>();

builder.Services.AddControllers();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

Console.WriteLine($"--> CommandService Endpoint {app.Configuration["CommandService"]}");

// app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

PrepDb.PrepPopulation(app, builder.Environment.IsProduction());

app.Run();
