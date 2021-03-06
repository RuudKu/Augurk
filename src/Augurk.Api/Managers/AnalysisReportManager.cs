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

using System.Threading.Tasks;
using Newtonsoft.Json;
using Raven.Json.Linq;
using Augurk.Entities.Analysis;
using Raven.Client.Linq;
using System.Collections.Generic;
using System.Linq;
using Augurk.Api.Indeces.Analysis;

namespace Augurk.Api.Managers
{
    /// <summary>
    /// Provides methods to persist and retrieve analysis reports from storage.
    /// </summary>
    public class AnalysisReportManager
    {
        /// <summary>
        /// Gets or sets the JsonSerializerSettings that should be used when (de)serializing.
        /// </summary>
        internal static JsonSerializerSettings JsonSerializerSettings { get; set; }

        /// <summary>
        /// Gets or sets the configuration manager which should be used by this instance.
        /// </summary>
        private ConfigurationManager ConfigurationManager { get; set; }

        public AnalysisReportManager()
            : this(new ConfigurationManager())
        {
               
        }

        internal AnalysisReportManager(ConfigurationManager configurationManager)
        {
            ConfigurationManager = configurationManager;
        }

        /// <summary>
        /// Inserts or updates the provided <paramref name="report"/> for the provided <paramref name="productName">product</paramref> and <paramref name="version"/>.
        /// </summary>
        /// <param name="productName">Name of the product that the analysis report relates to.</param>
        /// <param name="version">Version of the product that the analysis report relates to.</param>
        /// <param name="report">An <see cref="AnalysisReport"/> to insert or update.</param>
        public async Task InsertOrUpdateAnalysisReportAsync(string productName, string version, AnalysisReport report)
        {
            var configuration = await ConfigurationManager.GetOrCreateConfigurationAsync();

            using (var session = Database.DocumentStore.OpenAsyncSession())
            {
                // Store will override the existing report if it already exists
                await session.StoreAsync(report, $"{productName}/{version}/{report.AnalyzedProject}");

                session.SetExpirationIfEnabled(report, version, configuration);
                session.Advanced.GetMetadataFor(report)["Product"] = new RavenJValue(productName);

                await session.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Gets all the available <see cref="AnalysisReports"/> stored for the provided <paramref name="productName">product</paramref> and <paramref name="version"/>.
        /// </summary>
        /// <param name="productName">Name of the product to get the analysis reports for.</param>
        /// <param name="version">Version of the product to get the analysis reports for.</param>
        /// <returns>A range of <see cref="AnalysisReport"/> instances stored for the provided product and version.</returns>
        public IEnumerable<AnalysisReport> GetAnalysisReportsByProductAndVersionAsync(string productName, string version)
        {
            using(var session = Database.DocumentStore.OpenSession())
            {
                return session.Query<AnalysisReports_ByProductAndVersion.Entry, AnalysisReports_ByProductAndVersion>()
                           .Where(report => report.Product == productName && report.Version == version)
                           .OfType<AnalysisReport>()
                           .ToList();
            }
        }

        /// <summary>
        /// Persists the provided range of <paramref name="invocations"/> for the provided <paramref name="productName">product</paramref> and <paramref name="version"/>.
        /// </summary>
        /// <param name="productName">Name of the product to store the invocations for.</param>
        /// <param name="version">Version of the product to store the invocations for.</param>
        /// <param name="invocations">A range of <see cref="DbInvocation"/> instances representing the invocations to persist.</param>
        public async Task PersistDbInvocationsAsync(string productName, string version, IEnumerable<DbInvocation> invocations)
        {
            var configuration = await ConfigurationManager.GetOrCreateConfigurationAsync();

            using (var session = Database.DocumentStore.OpenAsyncSession())
            {
                foreach(var invocation in invocations)
                {
                    await session.StoreAsync(invocation, $"{productName}/{version}/{invocation.Signature}");
                    session.SetExpirationIfEnabled(invocation, version, configuration);
                    var metadata = session.Advanced.GetMetadataFor(invocation);
                    metadata["Product"] = new RavenJValue(productName);
                    metadata["Version"] = new RavenJValue(version);
                }

                await session.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Deletes all stored invocations for the provided <paramref name="productName">product</paramref> and <paramref name="version"/>.
        /// </summary>
        /// <param name="productName">Name of the product to delete the invocations for.</param>
        /// <param name="version">Version of the product to delete the invocations for.</param>
        public async Task DeleteDbInvocationsAsync(string productName, string version)
        {
            await Database.DocumentStore.DatabaseCommands.DeleteByIndex(nameof(Invocation_ByProductAndVersion).Replace("_", "/"),
                new Raven.Abstractions.Data.IndexQuery()
                {
                    Query = $"Product:{productName} AND Version:{version}"
                }, new Raven.Abstractions.Data.BulkOperationOptions
                {
                    AllowStale = true
                }).WaitForCompletionAsync();
        }
    }
}