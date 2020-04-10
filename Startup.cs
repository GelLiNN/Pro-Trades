using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Sociosearch.NET.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sociosearch.NET.Middleware;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace Sociosearch.NET
{
    public class Startup
    {
        //This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //Use Sqlite3 with Database Context
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(
                    Program.Config.GetConnectionString("DefaultConnection")));

            //Use Microsoft Identity for Sociosearch logins
            services
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
                .AddEntityFrameworkStores<ApplicationDbContext>();

            //Use SendGrid Email
            services.AddTransient<IEmailSender, EmailSender>();

            //Use RequestManager
            //services.AddSingleton<RequestManager>();

            //Use DataCache
            //services.AddSingleton<DataCache>();
            //services.AddSingleton<IHostedService, DataCacheLoader>(); //Load caches in background thread on startup

            //Use other .NET MVC modules
            services.AddControllersWithViews();
            services.AddRazorPages();
        }

        //This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                //The default HSTS value is 30 days, See https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRazorPages();
            });
        }
    }
}
