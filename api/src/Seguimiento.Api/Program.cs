using Seguimiento.Api;
using Seguimiento.Datos;
using Seguimiento.Servicios;

var builder = WebApplication.CreateBuilder(args);

// La cadena de conexión nunca se guarda en el repositorio: llega por user-secrets o por la
// variable de entorno ConnectionStrings__Seguimiento (ver README).
var cadenaConexion = builder.Configuration.GetConnectionString("Seguimiento");
if (string.IsNullOrWhiteSpace(cadenaConexion))
{
    throw new InvalidOperationException(
        "Falta la cadena de conexión 'ConnectionStrings:Seguimiento'. Configúrala con dotnet user-secrets " +
        "o con la variable de entorno ConnectionStrings__Seguimiento (ver README).");
}

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ManejadorExcepciones>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddServicios();
builder.Services.AddDatos(cadenaConexion);

var app = builder.Build();

app.UseExceptionHandler();
// Las respuestas de error sin cuerpo (por ejemplo, 404 de una ruta inexistente) también salen como ProblemDetails.
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
