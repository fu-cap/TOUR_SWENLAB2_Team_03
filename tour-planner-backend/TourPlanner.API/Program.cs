using System.Text.Json.Serialization;
using TourPlanner.BusinessLayer.Services;
using TourPlanner.DataAccessLayer.Repositories;

var builder = WebApplication.CreateBuilder(args);

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
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Konvertiert Enums im JSON automatisch in Strings statt Zahlen
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

    
var app = builder.Build();

app.MapOpenApi();


app.UseHttpsRedirection();
app.UseAuthorization(); 
app.UseCors("AngularFrontend");
app.MapControllers();

app.Run();