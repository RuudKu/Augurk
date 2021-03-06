﻿/*
 Copyright 2018, Augurk
 
 Licensed under the Apache License, Version 2.0 (the "License");
 you may not use this file except in compliance with the License.
 You may obtain a copy of the License at
 
 http://www.apache.org/licenses/LICENSE-2.0
 
 Unless required by applicable law or agreed to in writing, software
 distributed under the License is distributed on an "AS IS" BASIS,
 WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 See the License for the specific language governing permissions and
 limitations under the License.
*/

using Augurk.Entities;
using Raven.Json.Linq;
using Raven.Client;
using System;
using System.Text.RegularExpressions;

namespace Augurk
{
    public static class AsyncDocumentSessionExtensions
    {
        public static void SetExpirationIfEnabled(this IAsyncDocumentSession session, object document, string version, Configuration configuration)
        {
            if (configuration.ExpirationEnabled &&
                    Regex.IsMatch(version, configuration.ExpirationRegex))
            {
                // Set the expiration in the metadata
                session.Advanced.GetMetadataFor(document)["Raven-Expiration-Date"] =
                    new RavenJValue(DateTime.UtcNow.Date.AddDays(configuration.ExpirationDays));
            }
        }

    }
}
