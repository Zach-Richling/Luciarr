using Serilog;
using System.Text.Json;
using Serilog.Events;
using Luciarr.WebApi.Models;
using Luciarr.WebApi.Middleware;
using Microsoft.OpenApi.Models;
using Luciarr.WebApi.Clients;
using Luciarr.WebApi.Models.Radarr;
using Coravel;
using Luciarr.WebApi.Workers;
using System.Reflection;
using Luciarr.WebApi.Models.Sonarr;

namespace Luciarr.WebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(Path.Combine(AppContext.BaseDirectory, "luciarr-logs.txt"), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
                .WriteTo.Console()
                .MinimumLevel.Override("Microsoft.AspNetCore.Hosting", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore.Mvc", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.AspNetCore.Routing", LogEventLevel.Warning)
                .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
                .CreateLogger();

            try
            {
                var version = Assembly.GetEntryAssembly()
                ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion
                ?.Split("+")
                .First();

                Log.Information("Luciarr version: {Version}", version);

                var builder = WebApplication.CreateBuilder(args);

                builder.Host.UseWindowsService();

                builder.Services.AddSerilog();
                builder.Services.AddControllers().AddJsonOptions(x => x.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);
                builder.Services.AddEndpointsApiExplorer();

                builder.Services.AddHttpClient();

                builder.Services.AddSingleton<SonarrClient>();
                builder.Services.AddSingleton<RadarrClient>();
                builder.Services.AddSingleton<TmdbClient>();
                builder.Services.AddSingleton<NewReleaseRequester>();

                builder.Services.AddScheduler();

                builder.Services.Configure<AppSettings>(builder.Configuration.Bind);
                builder.Services.Configure<RadarrSettings>(builder.Configuration.GetSection("RadarrSettings").Bind);
                builder.Services.Configure<SonarrSettings>(builder.Configuration.GetSection("SonarrSettings").Bind);

                builder.Services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Luciarr - Plex", Version = "v1" });
                    c.AddSecurityDefinition("basic", new OpenApiSecurityScheme
                    {
                        Name = "Authorization",
                        Type = SecuritySchemeType.Http,
                        Scheme = "basic",
                        In = ParameterLocation.Header,
                        Description = "Basic Authorization header using the Bearer scheme."
                    });

                    c.AddSecurityRequirement(new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "basic"
                            }
                            },
                            Array.Empty<string>()
                        }
                    });
                });

                var app = builder.Build();

                app.Services.UseScheduler(x => 
                {
                    x.Schedule<NewReleaseRequester>().Weekly().Sunday().RunOnceAtStart();
                });

                app.UseSwagger();
                app.UseSwaggerUI();

                app.UseAuthorization();

                app.MapControllers();

                app.UseSerilogRequestLogging();

                app.UseMiddleware<BasicAuthMiddleware>();

                app.Run();
            } 
            catch (Exception e)
            {
                Log.Fatal(e, "Application terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
