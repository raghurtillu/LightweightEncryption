// This is free and unencumbered software released into the public domain.

using System;

namespace LightweightEncryption.Configuration
{
    /// <summary>
    /// Encryption configuration.
    /// </summary>
    public class EncryptionConfiguration
    {
        /// <summary>
        /// Gets or sets the key vault.
        /// </summary>
        public required string Keyvault { get; set; }

        /// <summary>
        /// Gets or sets the pseudo symmetric key from which the encryption key is derived.
        /// </summary>
        public required string SecretName { get; set; }

        /// <summary>
        /// Gets or sets the pseudo symmetric key version.
        /// </summary>
        public required string SecretVersion { get; set; }

        /// <summary>
        /// Gets or sets the encryption type.
        /// </summary>
        public string Type { get; set; } = "symmetric";

        /// <summary>
        /// Gets or sets the encryption algorithm.
        /// </summary>
        public string Algorithm { get; set; } = "AES-GCM";
    }
}
