
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SmartAPI.Services;

public partial class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var firebaseProjectId = builder.Configuration["Firebase:ProjectId"];

        if (string.IsNullOrWhiteSpace(firebaseProjectId))
        {
            throw new InvalidOperationException(
                "Firebase:ProjectId is not configured. Set it in appsettings.json or with: " +
                "dotnet user-secrets set \"Firebase:ProjectId\" \"<your-firebase-project-id>\"");
        }

        var firebaseIssuer = $"https://securetoken.google.com/{firebaseProjectId}";

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = firebaseIssuer;
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = firebaseIssuer,
                    ValidateAudience = true,
                    ValidAudience = firebaseProjectId,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        builder.Services.AddAuthorization();

        builder.Services.AddControllers();

        builder.Services.AddSingleton<FarmStore>();
        builder.Services.AddSingleton<DataHub>();

        builder.Services.AddHostedService<TelemetrySimulator>();
        builder.Services.AddHostedService<PowerSimulator>();

        builder.Services.AddEndpointsApiExplorer();

        // Swagger configuration
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        Console.WriteLine($"Environment: {app.Environment.EnvironmentName}");
        Console.WriteLine("Swagger UI: http://localhost:5109/swagger");

        // Swagger middleware
        app.UseSwagger();

        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint(
                "/swagger/v1/swagger.json",
                "SmartX API v1");

            options.RoutePrefix = "swagger";
        });

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
