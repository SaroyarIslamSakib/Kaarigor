using CleanSolution.Application.Features.Settings.Commands;
using CleanSolution.Domain.Settings;
using CleanSolution.Infrastructure.Data;
using CleanSolution.Infrastructure.Extensions;
using CleanSolution.Web.Data;
using Cortex.Mediator.DependencyInjection;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Reflection;

#region Bootstrap Logger Configuration
Log.Logger = new LoggerConfiguration()
    .WriteTo.File("Logs/web-log-.log", rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();
#endregion
try
{
    var builder = WebApplication.CreateBuilder(args);

    #region Serilog Configuration
    builder.Host.UseSerilog((context, lc) => lc
        .MinimumLevel.Debug()
        .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .ReadFrom.Configuration(builder.Configuration)
    );
    #endregion

    #region Mediator Configuration
    builder.Services.AddCortexMediator(
        new[] { typeof(Program),
                typeof(AddSettingCommand)
        },
        options => options.AddDefaultBehaviors()
    );
    #endregion

    #region Mapster Configuration
    builder.Services.AddMapster();
    #endregion


    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    var migrationAssembly = Assembly.GetAssembly(typeof(ApplicationDbContext));

    builder.Services.AddDbContext(connectionString,migrationAssembly);
    builder.Services.AddDependencyInjection();



    builder.Services.AddDatabaseDeveloperPageExceptionFilter();

    #region Identity Configuration
    builder.Services.AddIdentity();
    #endregion

    #region External Authentication
    builder.Services.AddExternalLogin(builder.Configuration);
    #endregion
    builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
    builder.Services.AddControllersWithViews();
    builder.Services.AddRazorPages();

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseMigrationsEndPoint();
    }
    else
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapStaticAssets();

    app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
        .WithStaticAssets();

    app.MapRazorPages()
       .WithStaticAssets();
    app.Run();
}
catch(Exception ex)
{
    Log.Fatal(ex, "Application Crush");
}
finally
{
    Log.CloseAndFlush();
}
