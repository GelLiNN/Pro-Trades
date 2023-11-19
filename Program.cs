using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using PT.Data;
using PT.Middleware;
using PT.Models;


namespace PT
{
    public class Program
    {
        // For deploying to different environments read from appsettings.secrets.json
        public static string Environment;
        public static IConfiguration Config { get; set; }

        public static void Main(string[] args)
        {
            Config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .AddJsonFile("appsettings.secrets.json", optional: false, reloadOnChange: false)
                .AddEnvironmentVariables()
                .Build();
            Environment = Config.GetValue<string>("DeploymentEnvironment");

            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));

            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            // Use Microsoft Identity for PT logins
            builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddEntityFrameworkStores<ApplicationDbContext>();

            builder.Services.AddIdentityServer()
                .AddApiAuthorization<ApplicationUser, ApplicationDbContext>();

            builder.Services.AddAuthentication()
                .AddIdentityServerJwt();

            // OLD
            /*builder.Services
                .AddDefaultIdentity<IdentityUser>(options =>
                {
                    options.SignIn.RequireConfirmedAccount = true;
                    options.Password.RequireDigit = true;
                    options.Password.RequiredLength = 5;
                    options.Password.RequiredUniqueChars = 0;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequireNonAlphanumeric = false;
                })
                .AddEntityFrameworkStores<ApplicationDbContext>();*/

            // Use other .NET MVC modules
            builder.Services.AddControllersWithViews();
            builder.Services.AddRazorPages();

            // Stuff from old Startup.cs
            /*builder.Services.AddRazorPages().AddRazorPagesOptions(options =>
            {
                options.Conventions.AddPageRoute("/Home", "");
            });*/

            // Use SendGrid Email
            builder.Services.AddTransient<IEmailSender, EmailSender>();

            // Use RequestManager
            builder.Services.AddSingleton<RequestManager>();

            // Use DataCache
            builder.Services.AddSingleton<DataCache>();
            builder.Services.AddSingleton<IHostedService, DataCacheLoader>(); //Load caches in background thread on startup

            // Setup the generated swagger JSON for swagger documentation
            builder.Services.AddSwaggerGen(swagger =>
            {
                swagger.SwaggerDoc("v1", new OpenApiInfo { Title = "Pro-Trades API", Version = "v1" });
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

            app.UseAuthentication();
            app.UseIdentityServer();
            app.UseAuthorization();
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