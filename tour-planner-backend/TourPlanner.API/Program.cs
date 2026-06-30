using System.Text.Json.Serialization;
using TourPlanner.BusinessLayer.Services;
using TourPlanner.DataAccessLayer.Repositories;
using TourPlanner.DataAccessLayer;
using Microsoft.EntityFrameworkCore;
using TourPlanner.DataAccessLayer.Enums;
using Npgsql;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

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
builder.Services.AddHttpClient<ITourService, TourService>();

builder.Services.AddScoped<ILogRepository, LogRepository>();
builder.Services.AddScoped<ILogService, LogService>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();

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


builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddLog4Net("log4net.config");

    
var app = builder.Build();

app.MapOpenApi();


app.UseHttpsRedirection();
app.UseAuthorization(); 
app.UseCors("AngularFrontend");
app.MapControllers();

app.Run();