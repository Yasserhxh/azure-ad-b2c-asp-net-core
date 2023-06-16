using Microsoft.Azure.WebJobs;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using BacsAndTags;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.Extensions.Configuration;

[assembly: FunctionsStartup(typeof(Program))]
namespace BacsAndTags;
public class Program : FunctionsStartup
{
    public IConfiguration Configuration { get; }
    public Program()
    {
        var builder = new ConfigurationBuilder()
            .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();
        Configuration = builder.Build();
    }
    public override void Configure(IFunctionsHostBuilder builder)
    {
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApi(options =>
                {
                    Configuration.Bind("AzureAdB2C", options);

                    options.TokenValidationParameters.NameClaimType = "name";
                },
                options => { Configuration.Bind("AzureAdB2C", options); });

    }
}