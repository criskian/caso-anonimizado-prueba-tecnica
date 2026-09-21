using Microsoft.AspNetCore.Mvc;
using Seguimiento.Api;
using Seguimiento.Api.Json;
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

builder.Services
    .AddControllers()
    .AddJsonOptions(opciones => opciones.JsonSerializerOptions.Converters.Add(new FechaConDesfaseConverter()))
    .ConfigureApiBehaviorOptions(opciones =>
    {
        // Los 400 que genera ASP.NET (JSON mal formado, campos obligatorios) salen con el
        // mismo formato que los de ValidacionException, para que la interfaz los trate igual.
        opciones.InvalidModelStateResponseFactory = contexto =>
        {
            // Las claves de ASP.NET vienen como «VersionEsperada» o «$.fechaContacto»; se
            // normalizan a camelCase («versionEsperada», «fechaContacto»), igual que las del servicio.
            var errores = contexto.ModelState
                .Where(e => e.Value is { Errors.Count: > 0 })
                .GroupBy(e => ClaveEnCamelCase(e.Key))
                .ToDictionary(g => g.Key, g => g.SelectMany(e => e.Value!.Errors).Select(x => x.ErrorMessage).ToArray());

            var detalle = new ValidationProblemDetails(errores)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Datos inválidos",
                Detail = "Uno o más datos no son válidos.",
            };
            detalle.Extensions["codigo"] = "VALIDACION";
            return new BadRequestObjectResult(detalle) { ContentTypes = { "application/problem+json" } };
        };
    });
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

static string ClaveEnCamelCase(string clave)
{
    var limpia = clave.StartsWith("$.", StringComparison.Ordinal) ? clave[2..] : clave;
    return limpia.Length == 0 ? limpia : char.ToLowerInvariant(limpia[0]) + limpia[1..];
}
