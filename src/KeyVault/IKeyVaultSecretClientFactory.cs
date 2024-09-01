// <copyright file="IKeyVaultSecretClientFactory.cs" owner="Raghu R">
// Copyright (c) Raghu R. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

using System;

namespace LightweightEncryption.KeyVault
{
    /// <summary>
    /// Interface for Key Vault secret client factory.
    /// </summary>
    public interface IKeyVaultSecretClientFactory
    {
        /// <summary>
        /// Get the Key Vault secret client.
        /// </summary>
        /// <param name="keyVaultName">KeyVault name.</param>
        /// <returns>IKeyVaultSecretClient.</returns>
        public IKeyVaultSecretClient GetKeyVaultSecretClient(string keyVaultName);
    }
}
