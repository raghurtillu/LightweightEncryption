// <copyright file="KeyVaultSecretClient.cs" owner="Raghu R">
// Copyright (c) Raghu R. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

using Azure.Core;
using Azure.Security.KeyVault.Secrets;
using Dawn;

namespace LightweightEncryption.KeyVault
{
    /// <summary>
    /// Implementation of <seealso cref="IKeyVaultSecretClient"/>.
    /// </summary>
    public sealed class KeyVaultSecretClient : IKeyVaultSecretClient
    {
        private readonly string keyVaultName;
        private readonly SecretClient secretClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyVaultSecretClient"/> class.
        /// </summary>
        /// <param name="keyVaultName">KeyVault name.</param>
        /// <param name="secretClient">Secret client.</param>
        public KeyVaultSecretClient(string keyVaultName, SecretClient secretClient)
        {
            this.keyVaultName = Guard.Argument(keyVaultName, nameof(keyVaultName)).NotNull().NotEmpty().NotWhiteSpace();
            this.secretClient = Guard.Argument(secretClient, nameof(secretClient)).NotNull();
        }

        /// <summary>
        /// Gets the KeyVault name.
        /// </summary>
        public string GetKeyVaultName() => 
            this.keyVaultName;

        /// <inheritdoc/>
        public async Task<string> GetSecretAsync(string name, CancellationToken cancellationToken)
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            // Get latest secret version
            return await this.GetSecretAsync(name, version: null, cancellationToken);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        /// <inheritdoc/>
        public async Task<string> GetSecretAsync(string name, string version, CancellationToken cancellationToken)
        {
            // Get secret with specified version
            var secret = await this.secretClient.GetSecretAsync(name, version, cancellationToken);
            return secret.Value.Value;
        }
    }
}
