using System.Text.Json.Serialization;
using TourPlanner.BusinessLayer.Services;
using TourPlanner.DataAccessLayer.Repositories;
using TourPlanner.DataAccessLayer;
using Microsoft.EntityFrameworkCore;
using TourPlanner.DataAccessLayer.Enums;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Register DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.MapEnum<TransportType>();
var dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContext<TourPlannerDbContext>(options =>
    options.UseNpgsql(dataSource));

//Services registrieren
builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddScoped<ITourRepository, TourRepository>();
builder.Services.AddScoped<ITourService, TourService>();

//Cors für Angular-Frontend erlauben
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "http://localhost")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Konvertiert Enums im JSON automatisch in Strings statt Zahlen
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.KebabCaseLower)
        );
    });

    
var app = builder.Build();

app.MapOpenApi();


app.UseHttpsRedirection();
app.UseAuthorization(); 
app.UseCors("AngularFrontend");
app.MapControllers();

app.Run();