// <copyright file="Program.cs" owner="Raghu R">
// Copyright (c) Raghu R. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Azure.Core;
using Azure.Identity;
using CommandLine;
using Dawn;
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
            RunCommand? runCommand = default;
            var result = Parser.Default.ParseArguments<RunCommand>(args)
                .WithParsed<RunCommand>(options => runCommand = options)
                .WithNotParsed<RunCommand>(errors =>
                {
                    Console.WriteLine("Errors during argument parsing:");
                    errors
                        .ToList()
                        .ForEach(e => Console.WriteLine(e));
                });


            try
            {
                if (result.Errors.Count() > 0)
                {
                    Console.WriteLine("Errors during argument parsing. Exiting.");
                    var errorMessages = result.Errors.Select(e => e.ToString());
                    var errorMessage = string.Join(Environment.NewLine, errorMessages);
                    Console.WriteLine("Errors during argument parsing:");
                    Console.WriteLine(errorMessage);

                    Environment.ExitCode = 1;
                    return;
                }

                if (runCommand == default)
                {
                    Console.WriteLine("RunCommand is not set. Exiting.");
                    Environment.ExitCode = 1;
                    return;
                }

                await CreateHostBuilder(runCommand);
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
        /// <param name="runCommand">RunCommand.</param>
        /// <returns>Task.</returns>
        private static async Task<IHostBuilder> CreateHostBuilder(RunCommand runCommand)
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

                    // RunCommand
                    serviceCollection.AddSingleton<RunCommand>(runCommand);

                    // Build
                    var serviceProvider = serviceCollection.BuildServiceProvider();

                    serviceCollection.AddHostedService<UsageService>();
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
