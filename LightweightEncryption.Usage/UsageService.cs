// <copyright file="UsageService.cs" owner="Raghu R">
// Copyright (c) Raghu R. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

using System;
using Dawn;
using Microsoft.Extensions.Hosting;

namespace LightweightEncryption.Usage
{
    /// <summary>
    /// Perform encryption/decryption operations.
    /// </summary>
    public class UsageService : BackgroundService
    {
        private readonly IEncryptorFactory encryptorFactory;
        private readonly RunCommand runCommand;

        /// <summary>
        /// Initializes a new instance of the <see cref="UsageService"/> class.
        /// </summary>
        /// <param name="encryptorFactory">EncryptorFactory.</param>
        /// <param name="runCommand">RunCommand.</param>
        public UsageService(IEncryptorFactory encryptorFactory, RunCommand runCommand)
        {
            this.encryptorFactory = Guard.Argument(encryptorFactory, nameof(encryptorFactory)).NotNull().Value;
            this.runCommand = Guard.Argument(runCommand, nameof(runCommand)).NotNull().Value;
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var command = this.runCommand;
            var encryptor = this.encryptorFactory.GetEncryptor();

            // Perform encryption or decryption based on the operation
            if (command.Operation == Operation.Encrypt)
            {
                string encryptedText = await encryptor.EncryptAsync(command.Payload);
                Console.WriteLine($"Encrypted text: {encryptedText}");
            }
            else if (command.Operation == Operation.Decrypt)
            {
                string decryptedText = await encryptor.DecryptAsync(command.Payload);
                Console.WriteLine($"Decrypted text: {decryptedText}");
            }
            else
            {
                Console.WriteLine("Invalid operation specified.");
            }
        }
    }
}
