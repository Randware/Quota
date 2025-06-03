using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Database;
using Tomlyn;
using Tomlyn.Model;

namespace API;

public class Startup
{
    public IConfiguration Configuration { get; }

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
        // Parse TOML config
        var toml = Toml.Parse(File.ReadAllText("config.toml")).ToModel();
        var jwtSection = toml["jwt"] as TomlTable;
        var oauthSection = toml["oauth"] as TomlTable;

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
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseRouting();
        app.UseAuthorization();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}

