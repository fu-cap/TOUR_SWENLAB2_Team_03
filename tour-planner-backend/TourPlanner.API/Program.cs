var builder = WebApplication.CreateBuilder(args);

//Services registrieren
builder.Services.AddControllers();

builder.Services.AddOpenApi();

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

    
var app = builder.Build();

app.MapOpenApi();


app.UseHttpsRedirection();
app.UseAuthorization(); 
app.UseCors("AngularFrontend");
app.MapControllers();

app.Run();