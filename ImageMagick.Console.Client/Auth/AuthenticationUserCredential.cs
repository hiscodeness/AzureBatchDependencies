// -----------------------------------------------------------------------------------------
// <copyright file="AuthenticationUserCredential.cs" company="Microsoft">
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ImageMagick.Console.Client.Auth
{
    public class AuthenticationUserCredential
    {
        public AuthenticationUserCredential()
        {
            SupportsRealAuthentication = true;
        }

        public string UserName { get; set; }

        public string UserToken { get; set; }

        public string Tenant { get; set; }

        public bool SupportsRealAuthentication { get; set; }

        public static AuthenticationUserCredential Empty
        {
            get { return new AuthenticationUserCredential { SupportsRealAuthentication = false }; }
        }

        /// <summary>
        /// Creates a credential for a user of the form 1234-abdc-1234@greenbuttontest.onmicrosoft.com
        /// </summary>
        /// <param name="unattendedAccountId">UnattendedAccountId. Expected form is {UnattendedAccountId}@{tenant}</param>
        /// <param name="token">Password for the user</param>
        /// <returns></returns>
        public static AuthenticationUserCredential Parse(string unattendedAccountId, string token)
        {
            var unattendedAccount = ExtractUnattendedAccount(unattendedAccountId);
            
            if (string.IsNullOrWhiteSpace(unattendedAccount.Tenant) || string.IsNullOrWhiteSpace(unattendedAccount.UserName))
            {
                throw new ArgumentException("UnattendedAccountId should be of form 'ClientId=...;TenantId=...'");
            }

            unattendedAccount.UserToken = token;
            return unattendedAccount;
        }

        /// <summary>
        /// Creates a AuthenticationUserCredential from a token.
        /// </summary>
        /// <param name="userCredential">Expected userCredential should be in the form ClientId=...;TenantId=...</param>
        /// <returns>AuthenticationUserCredential</returns>
        private static AuthenticationUserCredential ExtractUnattendedAccount(string userCredential)
        {
            // Remove white spaces and split on ';'
            var cleanedSpacing = Regex.Replace(userCredential, @"\s+", "").Split(new[] { ';' });

            var authCredential = new AuthenticationUserCredential();

            foreach (var item in cleanedSpacing)
            {
                string[] splitItem = item.Split(new[] { '=' }, 2, StringSplitOptions.None);
                if (splitItem.Length == 2)
                {
                    string key = splitItem[0].Trim();
                    string value = splitItem[1].Trim();

                    if (string.Equals("TenantId", key, StringComparison.InvariantCultureIgnoreCase))
                    {
                        authCredential.Tenant = Uri.EscapeDataString(value);
                    }
                    else if (string.Equals("ClientId", key, StringComparison.InvariantCultureIgnoreCase))
                    {
                        authCredential.UserName = value;
                    }
                }
            }

            return authCredential;
        }
    }
}