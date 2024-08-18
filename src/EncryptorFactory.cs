// This is free and unencumbered software released into the public domain.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dawn;
using LightweightEncryption.Configuration;
using LightweightEncryption.KeyVault;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace LightweightEncryption
{
    /// <summary>
    /// Implementation of <seealso cref="IEncryptorFactory"/>.
    /// </summary>
    public class EncryptorFactory : IEncryptorFactory
    {
        private readonly EncryptionConfiguration encryptionConfiguration;
        private readonly IKeyVaultSecretClientFactory keyVaultSecretClientFactory;
        private readonly IMemoryCache memoryCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="EncryptorFactory"/> class.
        /// </summary>
        /// <param name="encryptionConfiguration">EncryptionConfiguration.</param>
        /// <param name="keyVaultSecretClientFactory">KeyvaultSecretClientFactory.</param>
        /// <param name="memoryCache">MemoryCache.</param>
        public EncryptorFactory(
            IOptions<EncryptionConfiguration> encryptionConfiguration,
            IKeyVaultSecretClientFactory keyVaultSecretClientFactory,
            IMemoryCache memoryCache)
        {
            this.encryptionConfiguration = Guard.Argument(encryptionConfiguration, nameof(encryptionConfiguration)).NotNull().Value.Value;
            this.keyVaultSecretClientFactory = Guard.Argument(keyVaultSecretClientFactory, nameof(keyVaultSecretClientFactory)).NotNull().Value;
            this.memoryCache = Guard.Argument(memoryCache, nameof(memoryCache)).NotNull().Value;
        }

        /// <inheritdoc/>
        public IEncryptor GetEncryptor()
        {
            if ("symmetric".Equals(this.encryptionConfiguration.Type, StringComparison.OrdinalIgnoreCase))
            {
                if ("aes-gcm".Equals(this.encryptionConfiguration.Algorithm, StringComparison.OrdinalIgnoreCase))
                {
                    return new Encryptor(this.encryptionConfiguration, this.keyVaultSecretClientFactory, this.memoryCache);
                }
            }

            throw new NotImplementedException($"Unsupported encryption type: {this.encryptionConfiguration.Type ?? string.Empty} " +
                                              $"or encryption algorithm: {this.encryptionConfiguration.Algorithm ?? string.Empty}");
        }
    }
}