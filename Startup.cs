using System;
using System.Collections.Generic;
using System.Linq;
using Easyweb.Site.Infrastructure;
using Easyweb.Site.Infrastructure.Middleware;
using Easyweb.Site.Infrastructure.Options;
using Easyweb.Site.Infrastructure.Routing;
using Easyweb.Site.Infrastructure.Startup;
using Easyweb.Site.Core.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Easyweb.Site.DataApi;
using Easyweb.Site;
using Easyweb.Site.Core.Imaging;
using System.IO;
using Microsoft.Extensions.FileProviders;

namespace Easyweb
{
    /// <summary>
    /// Startup class that is run on application init
    /// </summary>
    public class Startup
    {
        private readonly IWebHostEnvironment _env;

        /// <summary>
        /// Startup class that is run on application init, set in <see cref="Program"/>
        /// </summary>
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            _env = env;
        }

        /// <summary>
        /// Configuration containing all settings for the app
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// Configures all services neccessary for the app functionality
        /// This is run to configure all options for the application, before running 
        /// <see cref="Configure(IApplicationBuilder, IHostingEnvironment, IHttpContextAccessor, ILoggerFactory, IOptions{SiteOptions})"/> 
        /// which then activates and enables them
        /// </summary>
        public void ConfigureServices(IServiceCollection services)
        {
            // Registers the settings/options/configuration files and makes them available for 
            // dependency injection as IOption<Type> with automatic reload
            // Contains: IOptions<DataOptions> IOptions<SiteOptions>
            //
            // To add a new default option, use: services.Configure<MySiteOptions>(configuration.GetSection(nameof(MySiteOptions)));
            // or reach custom configuration items unbound like: 
            // Configuration["SiteOptions:DomainOptions:CustomHost"] 
            // which corresponds to: { SiteOptions: { DomainOptions: { CustomHost: "myHost" } } } in appSettings.json
            services.ConfigureEasywebOptions(Configuration);

            // Register default easyweb services, giving access to DI for all different interfaces/classes, ex:
            // IDataHandler/CacheDataProvider, SiteStateContext, IEmailService, IThumbnailService, IFormService, IStatisticsService, ClientContext, IUrlHelper
            // Also adds default MVC-services: 
            // AddMemoryCache(), AddHttpContextAccessor(), Lowercase-routing, ITagHelperComponent that runs action on header on request
            //
            // Configures security user login from Easyweb if Security.UseAuthentication is set in appsettings
            // and always adds easyweb-login for admin-mode on site
            //
            // Adds response caching and response compression, including for 'image/svg+xml' 
            //
            // Adds custom json localization services found in /Resources, provided through IStringLocalizer or <ew-translate />-taghelper
            //
            // Adds possible startup-injections from plugins
            services.AddEasywebDefaults(Configuration);

            // Registers required data services to fetch CMS-data from Easyweb API through a custom LINQ to Easyweb API-provider
            //
            services.RegisterDataServices(Configuration);

            // Thumbnailgenerator for automatically creating thumbnails from requested images for Windows-systems.
            // Replace the inejcted generator to provide equal functionality for non-windows-systems.
            //
            services.AddTransient<IThumbnailGenerator, DefaultThumbnailGenerator>();

            // Adds output caching for static cold start html-responses
            //
            services.AddOutputCaching();

            // Add MVC
            //
            var mvcBuilder = services.AddControllersWithViews(config =>
            {
                // Run a filter on post that will inform all templates of a post being made,
                // allowing them to take action on POSTs they might have sent themselves in their own contained environment.
                //
                config.Filters.Add(typeof(HttpOnPostFilter));

                // Possibility to suppress the model binding change where non-nullable values (like a simple int in a model) is treated as [Required] and will fail in model-validation
                // if no value is set. In previous versions and .NET Framework MVC, the behaviour used to be that a default value was set unless specifically specified as [Required].
                //
                // config.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
            })
                // Add default Easyweb MVC-config.
                // 1. Setting additional view locations (/Views, /Views/[Module], /Views/_Layout, /Views/_Default, /Views/_Templates), 
                // 2. Adds localization view subfolder expander (Ex. Home/Views/en-US/Index.chtml)
                // 3. Sets JSON-serializer to use camelCase and ignore reference loop handling, latest compat-version and data annotation localization
                .AddEasywebMvcConfig(Configuration)// Allows recomilation on view change in both production and development.
                .AddRazorRuntimeCompilation();

            // Allows recomilation on view change in development.
            if (_env.IsDevelopment())
                mvcBuilder.AddRazorRuntimeCompilation();
        }

        /// <summary>
        /// Method called at app init that speicy what to run and in what order. Used to configure the HTTP request pipeline.
        /// </summary>
        public void Configure(IApplicationBuilder app,
            AppSettings appSettings)
        {
            // Error pages and development resources
            //
            if (_env.IsDevelopment())
            {
                // Quick fix to ensure /wwwroot-folder exists to avoid a first run-error as it's not committed to repo
                //
                var webRootPath = Path.Combine(_env.ContentRootPath, "wwwroot");
                if (!Directory.Exists(webRootPath))
                    Directory.CreateDirectory(webRootPath);
                
                // Friendly developer error pages
                //
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // Custom exception handling, showing Views/Static.cshtml or, if that also fails, Resources/StaticError.html
                //
                app.UseExceptionHandler(err => err.Run(async context => await context.WriteEasywebException()));

                // An SSL setting that solves some STS-issues. https://aka.ms/aspnetcore-hsts
                //
                app.UseHsts();
            }

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(_env.ContentRootPath, "js")),
                RequestPath = "/js",
            });

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(_env.ContentRootPath, "css")),
                RequestPath = "/css", 
            });

            // Apply default easyweb settings for running the site
            //
            app.UseEasywebDefaults(_env, appSettings);

            // Aktiverar standard-routing-middleware
            //
            app.UseRouting();

            // Apart from activating possible in-site auth, always activate the external auth required for site admins to administer the site using Easyweb inline edit
            //
            app.UseAuthentication();
            app.UseAuthorization();


            // Global error handling, redirect defaults and output cache handling if activated.
            //
            app.UseEasywebGlobalRequestHandlers(_env, appSettings);

            // Add MVC-routing and add our Easyweb-routes
            // Will bind the routes in order to avoid collition:
            // 1. Home route, being '/'
            // 2. Image and document routes
            // 3. Custom/default modules with route template, like '/news' 
            // 4. The default catch all route, normally used for Pages
            //
            // You can add additional custom routes by simply adding routes.MapRoute(...) before AddEasywebRoutes
            app.UseEndpoints(routes =>
            {
                // Add default easyweb routes
                routes.AddEasywebRoutes(appSettings.SiteOptions);
            });
        }
    }
}
