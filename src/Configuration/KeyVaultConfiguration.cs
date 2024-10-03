// <copyright file="KeyVaultConfiguration.cs" owner="Raghu R">
// Copyright (c) Raghu R. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using Dawn;

namespace LightweightEncryption.Configuration
{
    /// <summary>
    /// Keyvault Configuration.
    /// </summary>
    public sealed class KeyVaultConfiguration
    {
        /// <summary>
        /// Gets or sets the default key vault name.
        /// </summary>
        required public string KeyVaultName { get; set; }

        /// <summary>
        /// Gets or sets the request timeout.
        /// </summary>
        public TimeSpan RequestTimeoutInSeconds { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets the request max retries.
        /// </summary>
        public int RequestMaxRetries { get; set; } = 3;

        /// <summary>
        /// Gets or sets the Key Vault URI.
        /// </summary>
        required public string KeyVaultUri { get; set; }

        /// <summary>
        /// Validates the keyVaultConfiguration.
        /// </summary>
        /// <returns>True if validation is successful, false otherwise.</returns>
        public bool Validate()
        {
            Guard.Argument(this, nameof(KeyVaultConfiguration))
                .NotNull()
                .Member(x => x.KeyVaultUri, v => v.NotNull().NotEmpty())
                .Member(x => x.KeyVaultName, v => v.NotNull().NotEmpty());

            return true;
        }
    }
}
