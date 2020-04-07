using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Sociosearch.NET
{
    public class Program
    {
        //For deploying to different environments read from appsettings.secrets.json
        public static string Environment;
        public static IConfiguration Config { get; set; }

        //Main entry point for starting the server
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            Config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .AddJsonFile("appsettings.secrets.json", optional: false, reloadOnChange: false)
                .AddEnvironmentVariables()
                .Build();
            Environment = Config.GetValue<string>("DeploymentEnvironment");

            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webHostBuilder =>
                {
                    webHostBuilder.UseConfiguration(Config);
                    webHostBuilder.UseStartup<Startup>();
                });
        }
    }
}
