using CleanSolution.Domain;
using CleanSolution.Domain.Repositories;
using CleanSolution.Domain.Utilities;
using CleanSolution.Infrastructure.Data;
using CleanSolution.Infrastructure.Identity;
using CleanSolution.Infrastructure.Repositories;
using CleanSolution.Infrastructure.Utilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace CleanSolution.Infrastructure.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static void AddDependencyInjection(this IServiceCollection service)
        {
            service.AddScoped<IApplicationUnitOfWork, ApplicationUnitOfWork>();
            service.AddScoped<IApplicationSettingRepository, ApplicationSettingRepository>();
            service.AddSingleton<IEmailUtility, EmailUtility>();
           
        }
        public static void AddDbContext(this IServiceCollection service,string connectionString, Assembly migrationAssembly)
        {
            service.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString,
                (x) => x.MigrationsAssembly(migrationAssembly)));

        }
        //External Login (google / facebook)
        public static void AddExternalLogin(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAuthentication()
                .AddGoogle(options =>
                {
                    options.ClientId =
                        configuration["Authentication:Google:ClientId"]!;

                    options.ClientSecret =
                        configuration["Authentication:Google:ClientSecret"]!;

                    options.Events.OnRemoteFailure = context =>
                    {
                        var logger = context.HttpContext.RequestServices
                            .GetRequiredService<ILoggerFactory>()
                            .CreateLogger("GoogleLogin");

                        logger.LogError(context.Failure, "Google login failed.");

                        context.Response.Redirect("/Account/Login?error=external");
                        context.HandleResponse();
                        return Task.CompletedTask;
                    };
                });
        }
        // Identity Configuration
        public static void AddIdentity(this IServiceCollection service)
        {
            service
                .AddIdentity<ApplicationUser, ApplicationRole>(options =>
                {
                    options.SignIn.RequireConfirmedEmail = true;
                })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddUserManager<ApplicationUserManager>()
                .AddRoleManager<ApplicationRoleManager>()
                .AddSignInManager<ApplicationSignInManager>()
                .AddDefaultTokenProviders();

            service.Configure<IdentityOptions>(options =>
            {
                // Password settings
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequiredLength = 6;
                options.Password.RequiredUniqueChars = 0;

                // Lockout settings
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                // User settings
                options.User.AllowedUserNameCharacters =
                    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
                options.User.RequireUniqueEmail = true;
            });
        }
    }
}
