// <copyright file="KeyVaultSecretClientFactory.cs" owner="Raghu R">
// Copyright (c) Raghu R. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Globalization;
using Azure.Core;
using Azure.Security.KeyVault.Secrets;
using Dawn;
using LightweightEncryption.Configuration;
using Microsoft.Extensions.Options;

namespace LightweightEncryption.KeyVault
{
    /// <summary>
    /// Implementation of <seealso cref="IKeyVaultSecretClientFactory"/>.
    /// </summary>
    internal sealed class KeyVaultSecretClientFactory : IKeyVaultSecretClientFactory
    {
        private readonly KeyVaultConfiguration keyVaultConfiguration;
        private readonly TokenCredential tokenCredential;

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyVaultSecretClientFactory"/> class.
        /// </summary>
        /// <param name="configuration">KeyVaultConfiguration.</param>
        /// <param name="tokenCredential">TokenCredential.</param>
        public KeyVaultSecretClientFactory(IOptions<KeyVaultConfiguration> configuration, TokenCredential tokenCredential)
        {
            Guard.Argument(configuration, nameof(configuration)).NotNull();
            Guard.Argument(configuration.Value, nameof(configuration)).NotNull();
            Guard.Argument(tokenCredential, nameof(tokenCredential)).NotNull();

            this.keyVaultConfiguration = configuration.Value;
            this.tokenCredential = tokenCredential;

            if (!this.keyVaultConfiguration.Validate())
            {
                throw new ArgumentException("Invalid KeyVaultConfiguration");
            }
        }

        /// <inheritdoc/>
        public IKeyVaultSecretClient GetKeyVaultSecretClient(string keyVaultName)
        {
            if (string.IsNullOrWhiteSpace(keyVaultName))
            {
                keyVaultName = this.keyVaultConfiguration.DefaultKeyVaultName;
            }

            var keyVaultUri = this.GetKeyVaultUri(keyVaultName);
            var secretClient = this.GetSecretClient(keyVaultUri);

            return new KeyVaultSecretClient(keyVaultName, secretClient);
        }

        /// <summary>
        /// Gets the Key Vault URI.
        /// </summary>
        /// <param name="keyVaultName">KeyVault name.</param>
        /// <returns>KeyVault uri.</returns>
        private Uri GetKeyVaultUri(string keyVaultName)
        {
            return new Uri(string.Format(CultureInfo.InvariantCulture, this.keyVaultConfiguration.KeyVaultUri, keyVaultName));
        }

        /// <summary>
        /// Gets the secret client.
        /// </summary>
        /// <param name="keyVaultUri">KeyVault uri.</param>
        /// <returns>Secret client.</returns>
        private SecretClient GetSecretClient(Uri keyVaultUri)
        {
            var secretClientOptions = new SecretClientOptions
            {
                Retry =
                {
                    Delay = TimeSpan.FromSeconds(2),
                    MaxRetries = this.keyVaultConfiguration.RequestMaxRetries,
                    Mode = RetryMode.Exponential,
                    NetworkTimeout = this.keyVaultConfiguration.RequestTimeout,
                },
            };

            return new SecretClient(keyVaultUri, this.tokenCredential, secretClientOptions);
        }
    }
}
