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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Owin;
using Owin;
using Umbraco.Core;
using Umbraco.Web.Security;
using Microsoft.Owin.Security.OpenIdConnect;

namespace Umbraco8
{
    public static class UmbracoADAuthExtensions
    {

        ///  <summary>
        ///  Configure ActiveDirectory sign-in
        ///  </summary>
        ///  <param name="app"></param>
        ///  <param name="tenant">
        ///  Your tenant ID i.e. YOURDIRECTORYNAME.onmicrosoft.com OR this could be the GUID of your tenant ID
        ///  </param>
        ///  <param name="clientId">
        ///  Also known as the Application Id in the azure portal
        ///  </param>
        ///  <param name="postLoginRedirectUri">
        ///  The URL that will be redirected to after login is successful, example: http://mydomain.com/umbraco/;
        ///  </param>
        ///  <param name="issuerId">
        /// 
        ///  This is the "Issuer Id" for you Azure AD application. This is a GUID value of your tenant ID.
        /// 
        ///  If this value is not set correctly then accounts won't be able to be detected 
        ///  for un-linking in the back office. 
        /// 
        ///  </param>
        /// <param name="caption"></param>
        /// <param name="style"></param>
        /// <param name="icon"></param>
        /// <remarks>
        /// 
        ///  ActiveDirectory account documentation for ASP.Net Identity can be found:
        ///  https://github.com/AzureADSamples/WebApp-WebAPI-OpenIDConnect-DotNet
        /// 
        ///  </remarks>
        public static void ConfigureBackOfficeAzureActiveDirectoryAuth(this IAppBuilder app, 
            string tenant, string clientId, string postLoginRedirectUri, Guid issuerId,
            string caption = "Active Directory", string style = "btn-microsoft", string icon = "fa-windows")
        {         
            var authority = string.Format(
                CultureInfo.InvariantCulture, 
                "https://login.windows.net/{0}", 
                tenant);

            var adOptions = new OpenIdConnectAuthenticationOptions
            {
                SignInAsAuthenticationType = Constants.Security.BackOfficeExternalAuthenticationType,
                ClientId = clientId,
                Authority = authority,
                RedirectUri = postLoginRedirectUri
            };

            adOptions.ForUmbracoBackOffice(style, icon);            
            adOptions.Caption = caption;
            //Need to set the auth tyep as the issuer path
            adOptions.AuthenticationType = string.Format(
                CultureInfo.InvariantCulture,
                "https://sts.windows.net/{0}/",
                issuerId);
            
            // Must add the following code block to avoid JavaScript error on the user profile dialog
            var autoLinkOptions = new ExternalSignInAutoLinkOptions();  // autoLinkExternalAccount = true
            autoLinkOptions.AllowManualLinking = true;

            adOptions.SetBackOfficeExternalLoginProviderOptions(new BackOfficeExternalLoginProviderOptions
            {
                AutoRedirectLoginToExternalProvider = false,
                DenyLocalLogin = false, // if ANY external provider has this property set, local login will be disabled
                AutoLinkOptions = autoLinkOptions,
            });

            app.UseOpenIdConnectAuthentication(adOptions);            
        }    

    }
    
}
