// <copyright file="IEncryptor.cs" owner="Raghu R">
// Copyright (c) Raghu R. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace LightweightEncryption
{
    /// <summary>
    /// Interface for encryption and decryption.
    /// </summary>
    public interface IEncryptor
    {
        /// <summary>
        /// Encrypts the given plain text.
        /// </summary>
        /// <param name="plainText">Payload to encrypt.</param>
        /// <returns>Encrypted payload.</returns>
        public Task<string> EncryptAsync(string plainText);

        /// <summary>
        /// Decrypts the given encrypted text.
        /// </summary>
        /// <param name="encryptedText">Encrypted payload.</param>
        /// <returns>Decrypted value.</returns>
        public Task<string> DecryptAsync(string encryptedText);
    }
}
