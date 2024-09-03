// <copyright file="RunCommand.cs" owner="Raghu R">
// Copyright (c) Raghu R. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using CommandLine;

namespace LightweightEncryption.Usage
{
    /// <summary>
    /// Operation.
    /// </summary>
    public enum Operation
    {
        /// <summary>
        /// Encryption operation.
        /// </summary>
        Encrypt,

        /// <summary>
        /// Decryption operation.
        /// </summary>
        Decrypt,
    }

    /// <summary>
    /// Run command.
    /// </summary>
    [Verb("run", HelpText = "Run the encryption/decryption.")]
    public sealed class RunCommand
    {
        /// <summary>
        /// Gets or sets the operation.
        /// </summary>
        [Option('o', "operation", Required = true, HelpText = "Operation to perform. Allowed operations are Encrypt or Decrypt")]
        public Operation Operation { get; set; }

        /// <summary>
        /// Gets or sets the subscription.
        /// </summary>
        [Option('s', "subscription", Required = true, HelpText = "Azure Subscription.")]
        public Guid Subscription { get; set; }

        /// <summary>
        /// Gets or sets the resource group.
        /// </summary>
        [Option('r', "resource-group", Required = true, HelpText = "Azure Resource group.")]
        required public string ResourceGroup { get; set; }

        /// <summary>
        /// Gets or sets the key vault name.
        /// </summary>
        [Option('v', "vault-name", Required = true, HelpText = "Azure Key vault name.")]
        required public string KeyVaultName { get; set; }

        /// <summary>
        /// Gets or sets the payload to encrypt/decrypt.
        /// </summary>
        [Option('p', "payload", Required = true, HelpText = "Payload to encrypt/decrypt.")]
        required public string Payload { get; set; }

        /// <summary>
        /// Gets or sets the key name.
        /// </summary>
        [Option('k', "key-name", Default = "secret--encryption--symmetricKey", Required = false, HelpText = "Key name.")]
        public string KeyName { get; set; }

        /// <summary>
        /// Gets or sets the key version name.
        /// </summary>
        [Option('n', "key-version-name", Default = "secret--encryption--symmetricKeyVersion", Required = false, HelpText = "Key version name.")]
        public string KeyVersionName { get; set; }
    }
}
