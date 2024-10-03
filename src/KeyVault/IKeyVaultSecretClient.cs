// <copyright file="IKeyVaultSecretClient.cs" owner="Raghu R">
// Copyright (c) Raghu R. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

using System;

namespace LightweightEncryption.KeyVault
{
    /// <summary>
    /// Interface for Key Vault secret client operations.
    /// </summary>
    public interface IKeyVaultSecretClient
    {
        /// <summary>
        /// Gets the key vault name.
        /// </summary>
        public string GetKeyVaultName();

        /// <summary>
        /// Gets the latest secret version from the Key Vault.
        /// </summary>
        /// <param name="name">Secret name.</param>
        /// <param name="cancellationToken">CancellationToken.</param>
        /// <returns></returns>
        public Task<string> GetSecretAsync(string name, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the specified secret version from the Key Vault.
        /// </summary>
        /// <param name="name">Secret name.</param>
        /// <param name="version">Secret version.</param>
        /// <param name="cancellationToken">CancellationToken.</param>
        /// <returns>Secret in string form.</returns>
        public Task<string> GetSecretAsync(string name, string version, CancellationToken cancellationToken);
    }
}
