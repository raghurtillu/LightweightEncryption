// <copyright file="Encryptor.cs" owner="Raghu R">
// Copyright (c) Raghu R. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

using System.Security.Cryptography;
using System.Text;
using Dawn;
using LightweightEncryption.Configuration;
using LightweightEncryption.KeyVault;
using Microsoft.Extensions.Caching.Memory;

namespace LightweightEncryption
{
    /// <summary>
    /// Implementation of <seealso cref="IEncryptor"/>.
    /// </summary>
    public sealed class Encryptor : IEncryptor
    {
        // Encryption header related constants.
        // The encrypted data format is as follows:
        // Preamble (4 bytes)
        // Salt (32 bytes) of which first 12 bytes are used as Nonce
        // MasterKeyVersion (32 bytes)
        // Tag (16 bytes)
        // CipherText (variable length)
        private const int PreambleOffset = 0;
        private const int PreambleSizeInBytes = 4;
        private const int SaltOffset = PreambleOffset + PreambleSizeInBytes;
        private const int SaltSizeInBytes = 32;
        private const int NonceOffset = SaltOffset;
        private const int NonceSizeInBytes = 12;
        private const int MasterKeyVersionOffset = SaltOffset + SaltSizeInBytes;
        private const int MasterKeyVersionSizeInBytes = 32;
        private const int TagOffset = MasterKeyVersionOffset + MasterKeyVersionSizeInBytes;
        private const int TagSizeInBytes = 16;
        private const int HeaderSize = PreambleSizeInBytes + SaltSizeInBytes + MasterKeyVersionSizeInBytes + TagSizeInBytes;

        // HKDF related constants.
        private const int DerivedKeySizeInBytes = 32;

        // The preamble is used to identify the encrypted data.
        private static readonly byte[] Preamble = new byte[4] { (byte)'e', (byte)'n', (byte)'c', (byte)'r' };
        private static readonly UTF8Encoding Utf8Encoder = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        private readonly IKeyVaultSecretClient keyVaultSecretClient;
        private readonly IMemoryCache memoryCache;
        private readonly TimeSpan cacheExpirationTimeInHours;
        private readonly string masterKeySecretName;
        private readonly string masterKeySecretVersionName;

        /// <summary>
        /// Initializes a new instance of the <see cref="Encryptor"/> class.
        /// </summary>
        /// <param name="encryptionConfiguration">EncryptionConfiguration.</param>
        /// <param name="keyVaultSecretClientFactory">KeyvaultSecretClientFactory.</param>
        /// <param name="memoryCache">MemoryCache.</param>
        public Encryptor(
            EncryptionConfiguration encryptionConfiguration,
            IKeyVaultSecretClientFactory keyVaultSecretClientFactory,
            IMemoryCache memoryCache)
        {
            encryptionConfiguration = Guard.Argument(encryptionConfiguration, nameof(encryptionConfiguration))
                .NotNull()
                .Member(i => i.SecretName, s => s.NotNull().NotEmpty())
                .Member(i => i.SecretVersionName, s => s.NotNull().NotEmpty())
                .Value;

            keyVaultSecretClientFactory = Guard.Argument(keyVaultSecretClientFactory, nameof(keyVaultSecretClientFactory)).NotNull().Value;
            this.keyVaultSecretClient = keyVaultSecretClientFactory.GetKeyVaultSecretClient();
            this.memoryCache = Guard.Argument(memoryCache, nameof(memoryCache)).NotNull().Value;
            this.cacheExpirationTimeInHours = TimeSpan.FromHours(1);
            this.masterKeySecretName = encryptionConfiguration.SecretName;
            this.masterKeySecretVersionName = encryptionConfiguration.SecretVersionName;
        }

        /// <inheritdoc />
        public async Task<string> EncryptAsync(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            // Get latest master key secret version
            var masterKeyVersionAsString = await this.GetSecretAsyncAsString(secretName: this.masterKeySecretVersionName);
            if (string.IsNullOrEmpty(masterKeyVersionAsString))
            {
                throw new InvalidOperationException($"Pseudo master key '{this.masterKeySecretName}' for version " +
                    $"'{this.masterKeySecretVersionName}' in keyvault '{this.keyVaultSecretClient.GetKeyVaultName()}' not found or is empty.");
            }

            // Get the master key secret for specified version
            var masterKey = await this.GetSecretAsyncAsBytes(secretName: this.masterKeySecretName, secretVersion: masterKeyVersionAsString);
            if (masterKey == default || masterKey.Length == 0)
            {
                throw new InvalidOperationException($"Pseudo master key '{this.masterKeySecretName}' for version " +
                    $"'{this.masterKeySecretVersionName}' in keyvault '{this.keyVaultSecretClient.GetKeyVaultName()}' not found or is empty.");
            }

            var masterKeyVersion = Utf8Encoder.GetBytes(masterKeyVersionAsString);
            var plainText = Utf8Encoder.GetBytes(input);
            return Encrypt(plainText, masterKey, masterKeyVersion, new byte[HeaderSize + plainText.Length]);

            string Encrypt(ReadOnlySpan<byte> plainText, ReadOnlySpan<byte> masterKey, ReadOnlySpan<byte> masterKeyVersion, Span<byte> encryptedData)
            {
                var buffer = encryptedData.Slice(0, HeaderSize);
                var cipherText = encryptedData.Slice(HeaderSize);

                // Set the preamble.
                Preamble.AsSpan().CopyTo(buffer.Slice(PreambleOffset, PreambleSizeInBytes));

                // Set the salt.
                var salt = buffer.Slice(SaltOffset, SaltSizeInBytes);
                RandomNumberGenerator.Fill(salt);

                // Copy master key version
                var masterKeyVersionSpan = buffer.Slice(MasterKeyVersionOffset, MasterKeyVersionSizeInBytes);
                masterKeyVersion.CopyTo(masterKeyVersionSpan);

                // Derive a key from the master key and the payload header.
                Span<byte> derivedKey = stackalloc byte[DerivedKeySizeInBytes];
                HKDF.Expand(HashAlgorithmName.SHA256, prk: masterKey, output: derivedKey, info: buffer.Slice(0, TagOffset));

                // Encrypt the data using derivedKey.
                var nonce = salt.Slice(0, NonceSizeInBytes);
                var tag = buffer.Slice(TagOffset, TagSizeInBytes);

                using var encryptor = new AesGcm(derivedKey, TagSizeInBytes);
                encryptor.Encrypt(nonce, plainText, cipherText, tag);

                return Convert.ToBase64String(encryptedData);
            }
        }

        /// <inheritdoc />
        public async Task<string> DecryptAsync(string encrypted)
        {
            if (string.IsNullOrEmpty(encrypted))
            {
                return encrypted;
            }

            byte[] encryptedData = Convert.FromBase64String(encrypted);
            if (encryptedData.Length < HeaderSize)
            {
                throw new InvalidDataException($"The encrypted payload '{encryptedData.Length}' is less than header size '{HeaderSize}'.");
            }
            else if (!MemoryExtensions.SequenceEqual(encryptedData.AsSpan().Slice(PreambleOffset, PreambleSizeInBytes), Preamble))
            {
                throw new InvalidOperationException("The encrypted payload has invalid header.");
            }

            // Copy master key version to a byte array
            var masterKeyVersionAsBytes = encryptedData.AsSpan().Slice(MasterKeyVersionOffset, MasterKeyVersionSizeInBytes).ToArray();

            // Convert master key version to string
            var masterKeyVersionAsString = Utf8Encoder.GetString(encryptedData.AsSpan().Slice(MasterKeyVersionOffset, MasterKeyVersionSizeInBytes));

            // Get the master key secret for specified version
            var masterKey = await this.GetSecretAsyncAsBytes(secretName: this.masterKeySecretName, secretVersion: masterKeyVersionAsString);

            return Decrypt(encryptedData, masterKey, new byte[encryptedData.Length - HeaderSize]);

            string Decrypt(ReadOnlySpan<byte> encryptedData, ReadOnlySpan<byte> masterKey, Span<byte> decryptedData)
            {
                var salt = encryptedData.Slice(SaltOffset, SaltSizeInBytes);

                // Derive a key from the master key and the payload header.
                Span<byte> derivedKey = stackalloc byte[DerivedKeySizeInBytes];
                HKDF.Expand(HashAlgorithmName.SHA256, prk: masterKey, output: derivedKey, info: encryptedData.Slice(0, TagOffset));

                // Decrypt the data using derivedKey.
                var nonce = encryptedData.Slice(NonceOffset, NonceSizeInBytes);
                var tag = encryptedData.Slice(TagOffset, TagSizeInBytes);
                var cipherText = encryptedData.Slice(HeaderSize);
                using var encryptor = new AesGcm(derivedKey, TagSizeInBytes);
                encryptor.Decrypt(nonce, cipherText, tag, decryptedData);

                return Utf8Encoder.GetString(decryptedData);
            }
        }

        /// <summary>
        /// Get secret for specified version as a byte array.
        /// If secret version is empty, then latest version is returned.
        /// </summary>
        /// <param name="secretName">secretName.</param>
        /// <param name="secretVersion">secretVersion.</param>
        /// <returns>secret.</returns>
        private Task<byte[]?> GetSecretAsyncAsBytes(string secretName, string secretVersion = "")
        {
            return this.memoryCache.GetOrCreateAsync(
                (secretName, secretVersion),
                async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = this.cacheExpirationTimeInHours;
                    var secret = await this.GetSecretAsyncAsString(secretName, secretVersion);
                    if (string.IsNullOrEmpty(secret))
                    {
                        return default;
                    }

                    return Convert.FromHexString(secret);
                });
        }

        /// <summary>
        /// Get secret for specified version as a string.
        /// If secret version is empty, then latest version is returned.
        /// </summary>
        /// <param name="secretName">secretName.</param>
        /// <param name="secretVersion">secretVersion.</param>
        /// <returns>secret.</returns>
        private Task<string?> GetSecretAsyncAsString(string secretName, string secretVersion = "")
        {
            // cache the secret
            return this.memoryCache.GetOrCreateAsync(
                (secretName, secretVersion),
                async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = this.cacheExpirationTimeInHours;
                    return !string.IsNullOrEmpty(secretVersion)
                        ? await this.keyVaultSecretClient.GetSecretAsync(secretName, secretVersion, default)
                        : await this.keyVaultSecretClient.GetSecretAsync(secretName, default);
                });
        }
    }
}
