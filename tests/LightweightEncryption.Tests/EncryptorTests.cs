// <copyright file="EncryptorTests.cs" owner="Raghu R">
// Copyright (c) Raghu R. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

using LightweightEncryption.Configuration;
using LightweightEncryption.KeyVault;
using Microsoft.Extensions.Caching.Memory;
using Moq;

#pragma warning disable CS8604 // Possible null reference argument.

namespace LightweightEncryption.Tests
{
    /// <summary>
    /// Encryptor tests.
    /// </summary>
    public class EncryptorTests
    {
        private readonly Mock<IKeyVaultSecretClientFactory> mockKeyVaultSecretClientFactory;
        private readonly Mock<IKeyVaultSecretClient> mockKeyVaultSecretClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="EncryptorTests"/> class.
        /// </summary>
        public EncryptorTests()
        {
            this.mockKeyVaultSecretClientFactory = new Mock<IKeyVaultSecretClientFactory>();
            this.mockKeyVaultSecretClient = new Mock<IKeyVaultSecretClient>();
            this.mockKeyVaultSecretClientFactory.Setup(x => x.GetKeyVaultSecretClient(It.IsAny<string>()))
                .Returns(this.mockKeyVaultSecretClient.Object).Verifiable();
        }

        /// <summary>
        /// Encryptor in memory tests data.
        /// </summary>
        public static IEnumerable<object?[]> EncryptorTestsData => new List<object?[]>
            {
                new object?[] { null },
                new object?[] { "1" },
                new object?[] { "Hello World" },
                new object?[] { "Bellatrix;^GammARaY123# PhoTON*SINGularity  GravitATIONAlWaves" },
                new object?[] { "some random thing, some random thing, some random thing, some random thing, some random thing, some random thing" },
            };

        /// <summary>
        /// Encryptor tests for large data.
        /// </summary>
        public static IEnumerable<object?[]> EncryptorTestsLargeData => new List<object?[]>
            {
                new object?[] { Path.Combine("TestData", "testdatafile-128kb.txt") },
                new object?[] { Path.Combine("TestData", "testdatafile-256kb.txt") },
                new object?[] { Path.Combine("TestData", "testdatafile-512kb.txt") },
            };

        /// <summary>
        /// Encryptor tests for encryption and decryption.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [Theory]
        [MemberData(nameof(EncryptorTestsData))]
        [Trait("Category", "Unit")]
        public async Task EncryptAsyncTestAsync(string? input)
        {
            var key = GetKey();
            var configuration = GetEncryptionConfiguration(key);

            this.mockKeyVaultSecretClient.Setup(x => x.GetSecretAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(key).Verifiable();

            var encryptor = new Encryptor(configuration, this.mockKeyVaultSecretClientFactory.Object, GetMemoryCache());
            var encrypted = await encryptor.EncryptAsync(input);
            if (input != null)
            {
                Assert.NotEqual(input, encrypted);
            }

            var decrypted = await encryptor.DecryptAsync(encrypted);
            if (input != null)
            {
                Assert.Equal(input, decrypted);
                this.mockKeyVaultSecretClient.Verify(x => x.GetSecretAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
                Mock.VerifyAll(this.mockKeyVaultSecretClientFactory, this.mockKeyVaultSecretClient); 
            }

            // encrypt again to test cache
            encrypted = await encryptor.EncryptAsync(input);
            if (input != null)
            {
                Assert.NotEqual(input, encrypted); 
            }

            decrypted = await encryptor.DecryptAsync(encrypted);
            if (input != null)
            {
                Assert.Equal(input, decrypted);

                // Verify that the key was fetched only once from prior encryption
                this.mockKeyVaultSecretClient.Verify(x => x.GetSecretAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once); 
            }
        }

        /// <summary>
        /// Encryptor tests for large data.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        [Theory]
        [MemberData(nameof(EncryptorTestsLargeData))]
        [Trait("Category", "Unit")]
        public async Task EncryptAsyncTestForLargeDataAsync(string filePath)
        {
            var key = GetKey();
            var configuration = GetEncryptionConfiguration(key);
            this.mockKeyVaultSecretClient.Setup(x => x.GetSecretAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(key).Verifiable();

            var encryptor = new Encryptor(configuration, this.mockKeyVaultSecretClientFactory.Object, GetMemoryCache());

            string input = string.Empty;
            try
            {
                input = File.ReadAllText(filePath);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                Assert.True(false, $"Failed to read from file: {ex.Message}");
                return;
            }

            var encrypted = await encryptor.EncryptAsync(input);
            Assert.NotEqual(input, encrypted);

            var decrypted = await encryptor.DecryptAsync(encrypted);
            Assert.Equal(input, decrypted);
            Mock.VerifyAll(this.mockKeyVaultSecretClientFactory, this.mockKeyVaultSecretClient);
            this.mockKeyVaultSecretClient.Verify(x => x.GetSecretAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);

            // encrypt again to test cache
            encrypted = await encryptor.EncryptAsync(input);
            Assert.NotEqual(input, encrypted);

            decrypted = await encryptor.DecryptAsync(encrypted);
            Assert.Equal(input, decrypted);

            // Verify that the key was fetched only once from prior encryption
            this.mockKeyVaultSecretClient.Verify(x => x.GetSecretAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        private static EncryptionConfiguration GetEncryptionConfiguration(string key)
        {
            return new EncryptionConfiguration
            {
                Keyvault = "keyvault",
                SecretName = key,
                SecretVersion = Guid.NewGuid().ToString("N"),
            };

        }

        /// <summary>
        /// Get key.
        /// </summary>
        /// <returns></returns>
        private static string GetKey()
            => Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");

        /// <summary>
        /// Get memory cache.
        /// </summary>
        /// <returns></returns>
        private static IMemoryCache GetMemoryCache()
            => new MemoryCache(new MemoryCacheOptions());
    }
}

#pragma warning restore CS8604 // Possible null reference argument.
