using Coravel;
using Luciarr.WebApi.Models.Sonarr;
using Luciarr.WebApi.Clients;
using Luciarr.WebApi.Models.Radarr;
using Luciarr.WebApi.Models;
using Luciarr.WebApi.Workers;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using System.Reflection;
using System.Text.Json;
using Luciarr.WebApi.Middleware;
using Luciarr.Web.Data;
using Microsoft.EntityFrameworkCore;
using Luciarr.WebApi.Controllers;

namespace Luciarr.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Serilog.Log.Logger = new LoggerConfiguration()
                .WriteTo.File(Path.Combine(AppContext.BaseDirectory, "luciarr-logs.txt"), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
                .WriteTo.Console()
                .CreateBootstrapLogger();

            try
            {
                var version = Assembly.GetEntryAssembly()
                ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion
                ?.Split("+")
                .First();

                Serilog.Log.Information("Luciarr version: {Version}", version);

                var sqlliteName = "luciarr.db";
                var sqllitePath = Path.Combine(AppContext.BaseDirectory, sqlliteName);
                var sqliteConnString = $"Data Source={sqllitePath};Pooling=false";

                var builder = WebApplication.CreateBuilder(args);

                builder.Host.UseWindowsService();

                builder.Configuration.AddSqliteConfiguration(sqliteConnString);

                var logger = new LoggerConfiguration()
                    .MinimumLevel.Override("Microsoft.AspNetCore.Hosting", LogEventLevel.Warning)
                    .MinimumLevel.Override("Microsoft.AspNetCore.Mvc", LogEventLevel.Warning)
                    .MinimumLevel.Override("Microsoft.AspNetCore.Routing", LogEventLevel.Warning)
                    .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
                    .MinimumLevel.Override("Microsoft.AspNetCore.DataProtection.KeyManagement.XmlKeyManager", LogEventLevel.Warning)
                    .MinimumLevel.Override("Microsoft.AspNetCore.StaticFiles.StaticFileMiddleware", LogEventLevel.Warning)
                    .Enrich.FromLogContext()
                    .WriteTo.Console()
                    .WriteTo.SQLite(
                        sqliteDbPath: sqllitePath,
                        storeTimestampInUtc: true,
                        batchSize: 1
                    ).CreateLogger();

                builder.Services.AddSerilog(logger);
                builder.Services.AddControllers().AddJsonOptions(x => x.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);
                builder.Services.AddEndpointsApiExplorer();

                builder.Services.AddHttpClient();

                builder.Services.AddScoped<SonarrClient>();
                builder.Services.AddScoped<RadarrClient>();
                builder.Services.AddScoped<TmdbClient>();
                builder.Services.AddScoped<NewReleaseRequester>();

                builder.Services.AddScheduler();

                builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("LuciarrSettings").Bind);
                builder.Services.Configure<RadarrSettings>(builder.Configuration.GetSection("RadarrSettings").Bind);
                builder.Services.Configure<SonarrSettings>(builder.Configuration.GetSection("SonarrSettings").Bind);
                builder.Services.AddScoped(services =>
                {
                    return new SqliteDbContext(sqliteConnString);
                });
                

                builder.Services.AddSingleton<IConfigurationRoot>(builder.Configuration);
                builder.Services.AddScoped<SonarrController>();

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

                builder.Services.AddRazorPages();
                builder.Services.AddServerSideBlazor();
                builder.Services.AddBlazorBootstrap();

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

                // Configure the HTTP request pipeline.
                if (!app.Environment.IsDevelopment())
                {
                    app.UseExceptionHandler("/Error");
                }

                app.UseStaticFiles();
                app.UseRouting();
                app.MapBlazorHub();
                app.MapFallbackToPage("/_Host");

                app.Run();
            }
            catch (Exception e)
            {
                Serilog.Log.Fatal(e, "Application terminated unexpectedly");
            }
            finally
            {
                Serilog.Log.CloseAndFlush();
            }
        }
    }
}