using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Database;
using Database.Model;
using Tomlyn;
using Tomlyn.Model;
using Serilog.Context;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;

namespace API;

public class Startup
{
    public IConfiguration Configuration { get; }
    private bool _openApiEnabled;

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public class JwtConfig
    {
        public string Secret { get; set; }
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public int ExpiryMinutes { get; set; } = 60;
    }

    public class OAuthConfig
    {
        public string Id { get; set; }
        public string Secret { get; set; }
        public string ApiEndpoint { get; set; } = "https://discord.com/api/v10";
    }

    public void ConfigureServices(IServiceCollection services)
    {
        try
        {
            using (LogContext.PushProperty("SourceContext", "API.Configuration"))
            {
                // Parse TOML config
                var toml = Toml.Parse(File.ReadAllText("./config.toml")).ToModel();
                var jwtSection = toml["jwt"] as TomlTable;
                var oauthSection = toml["oauth"] as TomlTable;
                var openApiSection = toml.ContainsKey("openapi") ? toml["openapi"] as TomlTable : null;
                _openApiEnabled = openApiSection != null && openApiSection.ContainsKey("enabled") && (bool)openApiSection["enabled"];

                // Null checks for config sections and required values
                if (jwtSection == null)
                    throw new Exception("Missing [jwt] section in config.toml");
                if (oauthSection == null)
                    throw new Exception("Missing [oauth] section in config.toml");
                if (jwtSection["secret"] == null)
                    throw new Exception("Missing 'secret' in [jwt] section of config.toml");
                if (jwtSection["issuer"] == null)
                    throw new Exception("Missing 'issuer' in [jwt] section of config.toml");
                if (jwtSection["audience"] == null)
                    throw new Exception("Missing 'audience' in [jwt] section of config.toml");
                if (oauthSection["id"] == null)
                    throw new Exception("Missing 'id' in [oauth] section of config.toml");
                if (oauthSection["secret"] == null)
                    throw new Exception("Missing 'secret' in [oauth] section of config.toml");

                var jwtConfig = new JwtConfig
                {
                    Secret = jwtSection["secret"] as string,
                    Issuer = jwtSection["issuer"] as string,
                    Audience = jwtSection["audience"] as string,
                    ExpiryMinutes = jwtSection.ContainsKey("expiryMinutes") ? Convert.ToInt32(jwtSection["expiryMinutes"]) : 60
                };
                var oauthConfig = new OAuthConfig
                {
                    Id = oauthSection["id"] as string,
                    Secret = oauthSection["secret"] as string,
                    ApiEndpoint = oauthSection.ContainsKey("apiEndpoint") ? oauthSection["apiEndpoint"] as string : "https://discord.com/api/v10"
                };
                // Register config objects
                services.AddSingleton(jwtConfig);
                services.AddSingleton(oauthConfig);
                // Add controllers
                services.AddControllers();
                services.AddScoped<Storage>();
                // Register QuotaContext for DI
                services.AddDbContext<QuotaContext>(options =>
                {
                    options.UseSqlite("Data Source=database.db");
                    options.UseLoggerFactory(
                        LoggerFactory.Create(builder =>
                        {
                            builder.ClearProviders();
                            builder.SetMinimumLevel(LogLevel.Debug);
                            builder.AddSerilog(Common.Log.Logger);
                        })
                    );
                });
                // Add JwtService using config
                services.AddSingleton(sp => new JwtService(
                    jwtConfig.Secret,
                    jwtConfig.Issuer,
                    jwtConfig.Audience,
                    jwtConfig.ExpiryMinutes
                ));
                // Add Discord OAuth client using config
                services.AddSingleton(sp => new Common.OAuth.Client(
                    oauthConfig.Id,
                    oauthConfig.Secret,
                    oauthConfig.ApiEndpoint
                ));
                // Add JWT Bearer authentication
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Bearer";
                    options.DefaultChallengeScheme = "Bearer";
                })
                .AddJwtBearer("Bearer", options =>
                {
                    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtConfig.Issuer,
                        ValidAudience = jwtConfig.Audience,
                        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtConfig.Secret)),
                        ClockSkew = System.TimeSpan.FromSeconds(30)
                    };
                });
                // Add Swagger/OpenAPI if enabled
                if (_openApiEnabled)
                {
                    services.AddEndpointsApiExplorer();
                    services.AddSwaggerGen(options =>
                    {
                        options.SwaggerDoc("v1", new OpenApiInfo
                        {
                            Title = "Randware Quota API",
                            Version = "v1",
                            Description = "Beautiful, interactive documentation for the Randware Quota API."
                        });

                        // Add JWT Bearer
                        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                        {
                            Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
                            Name = "Authorization",
                            In = ParameterLocation.Header,
                            Type = SecuritySchemeType.Http,
                            Scheme = "bearer",
                            BearerFormat = "JWT"
                        });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });

    // XML comments if present
    var xmlFile = $"API.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});
                }

                Log.Logger.Information("API services configured successfully");
            }
        }
        catch (Exception ex)
        {
            using (LogContext.PushProperty("SourceContext", "API.Configuration"))
            {
                Log.Logger.Error(ex, "Error configuring API services");
                throw;
            }
        }
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        try
        {
            using (LogContext.PushProperty("SourceContext", "API.Startup"))
            {
                if (env.IsDevelopment())
                {
                    app.UseDeveloperExceptionPage();
                }

                // Ensure database is created
                using (var scope = app.ApplicationServices.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<QuotaContext>();
                    db.Database.EnsureCreated();
                    Log.Logger.Information("Database initialized successfully");
                }

                // Configure ASP.NET Core logging
                app.UseSerilogRequestLogging(opts =>
                {
                    opts.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
                    {
                        using (LogContext.PushProperty("SourceContext", "API.Request"))
                        {
                            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                            diagnosticContext.Set("RemoteIP", httpContext.Connection.RemoteIpAddress);
                        }
                    };
                    opts.MessageTemplate = "[{RequestMethod}] {RequestPath} responded {StatusCode} in {Elapsed:0.0000}ms";
                    opts.GetLevel = (httpContext, elapsed, ex) =>
                    {
                        if (ex != null) return LogEventLevel.Error;
                        if (httpContext.Response.StatusCode > 499) return LogEventLevel.Error;
                        if (httpContext.Response.StatusCode > 399) return LogEventLevel.Warning;
                        return LogEventLevel.Information;
                    };
                });

                if (_openApiEnabled)
                {
                    app.UseSwagger();
                    app.UseSwaggerUI(options =>
                    {
                        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Randware Quota API v1");
                        options.DocumentTitle = "Randware Quota API Documentation";
                    });
                    Log.Logger.Information("Swagger UI enabled at /swagger");
                }

                app.UseRouting();
                app.UseAuthentication(); 
                app.UseAuthorization();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                });

                Log.Logger.Information("API startup complete");
            }
        }
        catch (Exception ex)
        {
            using (LogContext.PushProperty("SourceContext", "API.Startup"))
            {
                Log.Logger.Error(ex, "Error during API startup");
                throw;
            }
        }
    }
}

