//===============================================================================
// Microsoft FastTrack for Azure
// Umbraco CMS Azure AD Integration Samples
//===============================================================================
// Copyright © Microsoft Corporation.  All rights reserved.
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY
// OF ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT
// LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
// FITNESS FOR A PARTICULAR PURPOSE.
//===============================================================================
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Web.BackOffice.Security;
using Umbraco.Extensions;

namespace UmbracoDemoAzSql
{
    public class Startup
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="Startup" /> class.
        /// </summary>
        /// <param name="webHostEnvironment">The web hosting environment.</param>
        /// <param name="config">The configuration.</param>
        /// <remarks>
        /// Only a few services are possible to be injected here https://github.com/dotnet/aspnetcore/issues/9337
        /// </remarks>
        public Startup(IWebHostEnvironment webHostEnvironment, IConfiguration config)
        {
            _env = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Configures the services.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <remarks>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        /// </remarks>
        public void ConfigureServices(IServiceCollection services)
        {
#pragma warning disable IDE0022 // Use expression body for methods
            services.AddUmbraco(_env, _config)
                .AddBackOffice()
                .AddWebsite()
                .AddComposers()

                .AddBackOfficeExternalLogins(extLoginBuilder =>
                {
                    var extLoginOpts = new ExternalSignInAutoLinkOptions(
                        autoLinkExternalAccount: true,
                        defaultUserGroups: new[] { "admin" },
                        defaultCulture: "en-US",
                        allowManualLinking: true)
                    {
                        OnExternalLogin = (user, loginInfo) =>
                        {
                            return true;
                        },
                    };

                    var loginProviderOptions = new BackOfficeExternalLoginProviderOptions(
                        "btn-microsoft",
                        "fa-windows",
                        extLoginOpts,
                        autoRedirectLoginToExternalProvider: false);

                    extLoginBuilder.AddBackOfficeLogin(
                        auth =>
                        {
                            var azAdConfig = _config.GetSection("AzureAd");

                            auth
                // https://github.com/umbraco/Umbraco-CMS/pull/9470
                .AddMicrosoftIdentityWebApp(options =>
                {
                    options.CallbackPath = "/umbraco-signin-oidc";
                    options.Instance = "https://login.microsoftonline.com/";
                    options.TenantId = azAdConfig["TenantId"];
                    options.ClientId = azAdConfig["ClientId"];
                    options.SignedOutRedirectUri = "/umbraco";

                    // https://github.com/AzureAD/microsoft-identity-web/issues/749
                    //options.ClaimActions.MapJsonKey(ClaimTypes.Email, ClaimConstants.PreferredUserName);

                    // Preferred over IClaimsTransformation which runs for every AuthenticateAsync
                    options.Events.OnTokenValidated = ctx =>
                    {
                        var username = ctx.Principal?.Claims.FirstOrDefault(c => c.Type == ClaimConstants.PreferredUserName);
                        if (username != null && ctx.Principal?.Identity is ClaimsIdentity claimsIdentity)
                        {
                            claimsIdentity.AddClaim(
                                new Claim(
                                    ClaimTypes.Email,
                                    username.Value
                                )
                            );
                        }

                        return Task.CompletedTask;
                    };
                },
                openIdConnectScheme: auth.SchemeForBackOffice(Constants.AzureAd),
                cookieScheme: "Fake");
                        });
                })

                .Build();
#pragma warning restore IDE0022 // Use expression body for methods

        }

        /// <summary>
        /// Configures the application.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <param name="env">The web hosting environment.</param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseUmbraco()
                .WithMiddleware(u =>
                {
                    u.UseBackOffice();
                    u.UseWebsite();
                })
                .WithEndpoints(u =>
                {
                    u.UseInstallerEndpoints();
                    u.UseBackOfficeEndpoints();
                    u.UseWebsiteEndpoints();
                });
        }
    }
}
