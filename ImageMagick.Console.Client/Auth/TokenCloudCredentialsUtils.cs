// -----------------------------------------------------------------------------------------
// <copyright file="TokenCloudCredentialsUtils.cs" company="Microsoft">
//    Copyright 2014 Microsoft Corporation
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
// </copyright>
// -----------------------------------------------------------------------------------------

using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Security.Authentication;
using System.Threading;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.WindowsAzure;

namespace ImageMagick.Console.Client.Auth
{
    /// <summary>
    /// Wraps the Azure Active Directory Authentication Library code for acquiring an authentication token.
    /// Refer to https://github.com/AzureADSamples/Daemon-DotNet/ for a further example,
    /// or to http://go.microsoft.com/fwlink/?LinkId=394414 for more information about the authentication protocols.
    /// </summary>
    public class TokenCloudCredentialsUtils
    {
        private static readonly string AadInstance = ConfigurationManager.AppSettings["AADInstance"] ?? "https://login.windows.net/{0}";

        private static readonly string BatchAppsResource = ConfigurationManager.AppSettings["BatchAppsResource"];

        /// <summary>
        /// Retrieves a <see cref="TokenCloudCredentials"/> class that can be used to
        /// authenticate calls into BatchApps.
        /// If the token call fails because the service is unavailable, retry twice with a 3 second pause between
        /// </summary>
        /// <param name="credential">
        /// The credential.
        /// </param>
        public static TokenCloudCredentials GetAuthenticationToken(AuthenticationUserCredential credential)
        {
            string authority = string.Format(CultureInfo.InvariantCulture, AadInstance, credential.Tenant);

            var authenticationContext = new AuthenticationContext(authority, TokenCache.DefaultShared);
            var clientCredential = new ClientCredential(credential.UserName, credential.UserToken);

            int retryCount = 0;

            while (true)
            {
                retryCount++;
                try
                {
                    // ADAL includes an in memory cache, so this call will only send a message to the server if the cached token is expired.
                    var result = authenticationContext.AcquireToken(BatchAppsResource, clientCredential);

                    if (string.IsNullOrEmpty(result.AccessToken))
                    {
                        throw new AuthenticationException("Failed to retrieve access token");
                    }

                    return new TokenCloudCredentials(result.AccessToken);
                }
                catch (AdalServiceException ex)
                {
                    if (ex.ErrorCode == "temporarily_unavailable" && retryCount < 3)
                    {
                        Debug.WriteLine("Failed to connect to AAD. Retrying...");

                        Thread.Sleep(3000);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }
    }
}
