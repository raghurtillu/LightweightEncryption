﻿// <copyright file="Program.cs" owner="Raghu R">
// Copyright (c) Raghu R. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Azure.Core;
using Azure.Identity;
using LightweightEncryption.Configuration;
using LightweightEncryption.KeyVault;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LightweightEncryption.Usage
{
    /// <summary>
    /// Program.
    /// </summary>
    public class Program
    {
        private const string EnvironmentName = "Development";

        /// <summary>
        /// Main.
        /// </summary>
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            try
            {
                await CreateHostBuilder();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Startup caught exception: " + ex);
                Environment.ExitCode = 1;
            }

            Console.WriteLine($"App Stopped {Environment.ExitCode}");
        }

        /// <summary>
        /// Create Host Builder.
        /// </summary>
        /// <returns>Task.</returns>
        private static async Task<IHostBuilder> CreateHostBuilder()
        {
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile(Path.Combine("override", "appsettings.override.json"), optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            var configurationRoot = configBuilder.Build();
            var hostBuilder = new HostBuilder()
                .ConfigureAppConfiguration((context, configurationBuilder) =>
                {
                    configurationBuilder.AddConfiguration(configurationRoot);
                })
                .UseEnvironment(EnvironmentName)
                .ConfigureLogging((context, loggingBuilder) =>
                {
                    loggingBuilder.ClearProviders();
                    loggingBuilder.AddConsole().AddDebug();
                })
                .ConfigureServices((hostBuilderContext, serviceCollection) =>
                {
                    // Configuration
                    serviceCollection.Configure<EncryptionConfiguration>(configurationRoot.GetSection(nameof(EncryptionConfiguration)));
                    serviceCollection.Configure<KeyVaultConfiguration>(configurationRoot.GetSection(nameof(KeyVaultConfiguration)));

                    // TokenCredential
                    serviceCollection.AddTransient<TokenCredential>(_ => GetTokenCredential());

                    // KeyVault
                    serviceCollection.AddTransient<IKeyVaultSecretClientFactory, KeyVaultSecretClientFactory>();

                    // MemoryCache
                    serviceCollection.AddMemoryCache();

                    // EncryptorFactory
                    serviceCollection.AddSingleton<IEncryptorFactory, EncryptorFactory>();

                    var serviceProvider = serviceCollection.BuildServiceProvider();

                    var keyVaultConfiguration = serviceProvider.GetRequiredService<IOptions<KeyVaultConfiguration>>();
                    var keyVaultSecretClientFactory = serviceProvider.GetRequiredService<IKeyVaultSecretClientFactory>();
                    var encryptorFactory = serviceProvider.GetRequiredService<IEncryptorFactory>();
                })
                .UseConsoleLifetime();

            await hostBuilder.Build().RunAsync();

            return hostBuilder;
        }

        /// <summary>
        /// Get Token Credential.
        /// </summary>
        /// <returns>TokenCredential.</returns>
        private static TokenCredential GetTokenCredential()
        {
            // DefaultAzureCredential will try different methods of auth, shown here: https://docs.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential?view=azure-dotnet
            // Using InteractiveBrowserCredential here as an example
            // adjust to your needs

            var auth = new InteractiveBrowserCredential();
            auth.Authenticate();

            return auth;
        }
    }
}
