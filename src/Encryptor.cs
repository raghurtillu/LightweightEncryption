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

        private readonly EncryptionConfiguration encryptionConfiguration;
        private readonly IKeyVaultSecretClientFactory keyVaultSecretClientFactory;
        private readonly IMemoryCache memoryCache;
        private readonly TimeSpan cacheExpirationTimeInHours;

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
            this.encryptionConfiguration = Guard.Argument(encryptionConfiguration, nameof(encryptionConfiguration))
                .NotNull()
                .Member(i => i.Keyvault, s => s.NotNull().NotEmpty())
                .Member(i => i.SecretName, s => s.NotNull().NotEmpty())
                .Member(i => i.SecretVersion, s => s.NotNull().NotEmpty())
                .Value;

            this.keyVaultSecretClientFactory = Guard.Argument(keyVaultSecretClientFactory, nameof(keyVaultSecretClientFactory)).NotNull().Value;
            this.memoryCache = Guard.Argument(memoryCache, nameof(memoryCache)).NotNull().Value;
            this.cacheExpirationTimeInHours = TimeSpan.FromHours(1);
        }

        /// <inheritdoc />
        public async Task<string> EncryptAsync(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            var masterKey = await this.GetMasterKeyForVersionAsync(this.encryptionConfiguration.SecretVersion);
            if (masterKey == default || masterKey.Length == 0)
            {
                throw new InvalidOperationException($"Pseudo master key '{this.encryptionConfiguration.SecretName}' for version " +
                    $"'{this.encryptionConfiguration.SecretVersion}' in keyvault '{this.encryptionConfiguration.Keyvault}' not found or is empty.");
            }

            var plainText = Utf8Encoder.GetBytes(input);
            return Encrypt(plainText, masterKey, new byte[HeaderSize + plainText.Length]);

            string Encrypt(ReadOnlySpan<byte> plainText, ReadOnlySpan<byte> masterKey, Span<byte> encryptedData)
            {
                var buffer = encryptedData.Slice(0, HeaderSize);
                var cipherText = encryptedData.Slice(HeaderSize);

                // Set the preamble.
                Preamble.AsSpan().CopyTo(buffer.Slice(PreambleOffset, PreambleSizeInBytes));

                // Set the salt.
                var salt = buffer.Slice(SaltOffset, SaltSizeInBytes);
                RandomNumberGenerator.Fill(salt);

                // Set the masterKeyVersion
                var masterKeyVersion = buffer.Slice(MasterKeyVersionOffset, MasterKeyVersionSizeInBytes);
                if (Utf8Encoder.GetBytes(this.encryptionConfiguration.SecretVersion, masterKeyVersion) != MasterKeyVersionSizeInBytes)
                {
                    throw new InvalidOperationException($"The masterKeyVersion '{this.encryptionConfiguration.SecretVersion}' is not of expected length '{MasterKeyVersionSizeInBytes}'.");
                }

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

            var masterKey = await this.GetMasterKeyForVersionAsync(Utf8Encoder.GetString(encryptedData.AsSpan().Slice(MasterKeyVersionOffset, MasterKeyVersionSizeInBytes)));
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
        /// Gets the pseudo master key for the given version.
        /// </summary>
        /// <param name="masterKeyVersion">Optional secret version of the secret pseudo master key, if the version is not provided latest version is used.</param>
        /// <returns></returns>
        private Task<byte[]?> GetMasterKeyForVersionAsync(string masterKeyVersion)
        {
            return this.memoryCache.GetOrCreateAsync(
                masterKeyVersion,
                async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = this.cacheExpirationTimeInHours;
                    var keyVaultSecretClient = this.keyVaultSecretClientFactory.GetKeyVaultSecretClient(this.encryptionConfiguration.Keyvault);

                    var secret = !string.IsNullOrEmpty(masterKeyVersion)
                                     ? await keyVaultSecretClient.GetSecretAsync(this.encryptionConfiguration.SecretName, masterKeyVersion, default)
                                     : await keyVaultSecretClient.GetSecretAsync(this.encryptionConfiguration.SecretName, default);
                    return Convert.FromHexString(secret);
                });
        }
    }
}
