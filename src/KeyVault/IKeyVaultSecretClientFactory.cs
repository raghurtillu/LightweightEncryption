// This is free and unencumbered software released into the public domain.

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
