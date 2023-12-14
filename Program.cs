using System.Diagnostics;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using PT.Middleware;
using PT.Services;
using LogLevel = NLog.LogLevel;

namespace PT
{
    public class Program
    {
        // For deploying to different environments read from appsettings.secrets.json
        public static string Env;

        // Application config can be read from anywhere in the app
        public static IConfiguration Config { get; set; }

        public static string ExecutingPath { get; set; } =
            Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName)
            ?? Environment.CurrentDirectory;

        public static void Main(string[] args)
        {
            // Read app config into object from both json files
            Config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .AddJsonFile("appsettings.secrets.json", optional: false, reloadOnChange: false)
                .AddEnvironmentVariables()
                .Build();
            Env = Config.GetValue<string>("DeploymentEnvironment");

            // Initialize logger and test
            LogHelper.InitializeLogger(Config);
            LogHelper.Log(LogLevel.Warn, "You have just begun to initialize Pro-Trades...");

            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            var connectionString =
                Config.GetValue<string>("PostgresConnectionString")
                ?? throw new InvalidOperationException(
                    "Connection string 'PostgresConnectionString' not found in config."
                );

            // Add Db
            builder
                .Services
                .AddDbContext<PTContext>(options => options.UseNpgsql(connectionString));
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            // Add UserService to DI
            //builder.Services.AddScoped<IUserService, UserService>();

            // Use RequestManager
            builder.Services.AddSingleton<RequestManager>();

            // Use other .NET MVC modules
            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages();

            // Use SendGrid Email
            builder.Services.AddTransient<IEmailSender, EmailSender>();

            // Use DataCache
            builder.Services.AddSingleton<DataCache>();
            builder.Services.AddSingleton<IHostedService, DataCacheLoader>(); //Load caches in background thread on startup

            // Setup the generated swagger JSON for swagger documentation
            builder
                .Services
                .AddSwaggerGen(swagger =>
                {
                    swagger.SwaggerDoc(
                        "v1",
                        new OpenApiInfo { Title = "Pro-Trades API", Version = "v1" }
                    );
                });

            // Add CORS (https://learn.microsoft.com/en-us/aspnet/core/security/cors?view=aspnetcore-8.0)
            builder
                .Services
                .AddCors(options =>
                {
                    options.AddPolicy(
                        name: Constants.PT_CORS,
                        policy =>
                        {
                            policy
                                .WithOrigins("http://localhost:7778")
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                        }
                    );
                });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios,
                // see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            // Must be placed after `UseRouting` but before `UseAuthorization`
            app.UseCors(Constants.PT_CORS);

            app.UseAuthentication();
            app.UseAuthorization();

            // custom jwt auth middleware
            //app.UseMiddleware<JwtMiddleware>();

            app.MapControllers();
            app.MapRazorPages();

            app.MapFallbackToFile("index.html");

            // Enable middleware to serve generated Swagger as a JSON endpoint
            app.UseSwagger();
            app.UseSwaggerUI(swagger =>
            {
                swagger.SwaggerEndpoint("/swagger/v1/swagger.json", "Pro-Trades API V1");
            });
            app.UseDeveloperExceptionPage();

            app.Run();
        }
    }
}
