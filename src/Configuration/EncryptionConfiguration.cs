// <copyright file="EncryptionConfiguration.cs" owner="Raghu R">
// Copyright (c) Raghu R. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

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
        required public string Keyvault { get; set; }

        /// <summary>
        /// Gets or sets the pseudo symmetric key from which the encryption key is derived.
        /// </summary>
        required public string SecretName { get; set; }

        /// <summary>
        /// Gets or sets the pseudo symmetric key version.
        /// </summary>
        required public string SecretVersion { get; set; }

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
