// <copyright file="EncryptorTests.cs" owner="Raghu R">
// Copyright (c) Raghu R. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
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
            this.mockKeyVaultSecretClientFactory.Setup(x => x.GetKeyVaultSecretClient())
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
        /// Encryptor constructor tests.
        /// </summary>
        [Theory]
        [Trait("Category", "Unit")]
        [InlineData(true, true, true)]
        [InlineData(true, true, false)]
        [InlineData(true, false, true)]
        [InlineData(true, false, false)]
        [InlineData(false, true, true)]
        [InlineData(false, false, true)]
        [InlineData(false, true, false)]
        [InlineData(false, true, true)]
        [InlineData(true, false, false)]
        [InlineData(true, true, false)]
        [InlineData(true, true, true)]
        public void EncryptorConstructorThrowsExceptionWhenInvalidArgumentsPassed(bool isConfigurationNull, bool isKeyVaultSecretFactoryNull, bool isMemoryCacheNull)
        {
            // Arrange
            EncryptionConfiguration? configuration = !isConfigurationNull ? GetEncryptionConfiguration() : default;
            IKeyVaultSecretClientFactory? secretClientFactory = !isKeyVaultSecretFactoryNull ? this.mockKeyVaultSecretClientFactory.Object : default;
            IMemoryCache? memoryCache = !isMemoryCacheNull ? GetMemoryCache() : default;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new Encryptor(configuration, secretClientFactory, memoryCache));
        }

        /// <summary>
        /// Encryptor tests when input is null or empty.
        /// </summary>
        /// <returns></returns>
        [Fact]
        [Trait("Category", "Unit")]
        public async Task EncryptorDoesNotEncryptWhenInputIsNullOrEmptyAsync()
        {
            // Arrange
            string? input = null;
            Encryptor encryptor = new Encryptor(GetEncryptionConfiguration(), mockKeyVaultSecretClientFactory.Object, GetMemoryCache());

            // Act
            string encrypted = await encryptor.EncryptAsync(input);

            // Assert
            Assert.Null(encrypted);

            input = string.Empty;
            encrypted = await encryptor.EncryptAsync(input);
            Assert.Empty(encrypted);
        }

        /// <summary>
        /// Decrypt tests when input is null or empty.
        /// </summary>
        /// <returns></returns>
        [Fact]
        [Trait("Category", "Unit")]
        public async Task EncryptorDoesNotDecryptWhenInputIsNullOrEmptyAsync()
        {
            // Arrange
            string? input = null;
            Encryptor encryptor = new Encryptor(GetEncryptionConfiguration(), mockKeyVaultSecretClientFactory.Object, GetMemoryCache());
            // Act
            string decrypted = await encryptor.DecryptAsync(input);
            // Assert
            Assert.Null(decrypted);
            input = string.Empty;
            decrypted = await encryptor.DecryptAsync(input);
            Assert.Empty(decrypted);
        }

        /// <summary>
        /// Encryptor tests when key vault secret client throws exception.
        /// </summary>
        /// <returns></returns>
        [Fact]
        [Trait("Category", "Unit")]
        public async Task EncryptorThrowsExceptionWhenKeyVaultSecretClientThrowsExceptionAsync()
        {
            // Arrange
            var key = GetKey();
            var keyVersion = GetKeyVersion();
            var configuration = GetEncryptionConfiguration();

            // Setup the mock to throw exception
            this.mockKeyVaultSecretClient.Setup(x => x.GetSecretAsync(It.Is<string>(x => x.Equals(configuration.SecretVersionName)), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("KeyVaultSecretClient exception")).Verifiable();

            var encryptor = new Encryptor(configuration, this.mockKeyVaultSecretClientFactory.Object, GetMemoryCache());

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => encryptor.EncryptAsync("Hello World"));
            Mock.VerifyAll(this.mockKeyVaultSecretClientFactory, this.mockKeyVaultSecretClient);
        }

        /// <summary>
        /// Encryptor tests when key vault returns invalid key.
        /// </summary>
        /// <returns></returns>
        [Fact]
        [Trait("Category", "Unit")]
        public async Task EncryptorThrowsExceptionWhenKeyVaultReturnsInvalidKeyAsync()
        {
            // Arrange
            var key = GetKey();
            var keyVersion = GetKeyVersion();
            var configuration = GetEncryptionConfiguration();

            // Setup the mock to return the key version
            this.mockKeyVaultSecretClient.Setup(x => x.GetSecretAsync(It.Is<string>(x => x.Equals(configuration.SecretVersionName)), It.IsAny<CancellationToken>()))
                .ReturnsAsync(keyVersion).Verifiable();
            // Setup the mock to return the key
            this.mockKeyVaultSecretClient.Setup(x => x.GetSecretAsync(It.Is<string>(x => x.Equals(configuration.SecretName)), It.Is<string>(x => x.Equals(keyVersion)), It.IsAny<CancellationToken>()))
                .ReturnsAsync(string.Empty).Verifiable();
            var encryptor = new Encryptor(configuration, this.mockKeyVaultSecretClientFactory.Object, GetMemoryCache());

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => encryptor.EncryptAsync("Hello World"));
            Mock.VerifyAll(this.mockKeyVaultSecretClientFactory, this.mockKeyVaultSecretClient);
        }

        /// <summary>
        /// Encryptor tests when decryption data format is invalid.
        /// </summary>
        /// <returns></returns>
        [Fact]
        [Trait("Category", "Unit")]
        public async Task EncryptorThrowsExceptionWhenDecryptDataFormatIsInvalidAsync()
        {
            // Arrange
            var key = GetKey();
            var keyVersion = GetKeyVersion();
            var configuration = GetEncryptionConfiguration();

            var encryptor = new Encryptor(configuration, this.mockKeyVaultSecretClientFactory.Object, GetMemoryCache());

            // Act & Assert
            await Assert.ThrowsAsync<FormatException>(() => encryptor.DecryptAsync("InvalidData"));
        }

        /// <summary>
        /// Encryptor tests when decryption data header size is invalid.
        /// </summary>
        /// <returns></returns>
        [Fact]
        [Trait("Category", "Unit")]
        public async Task EncryptorThrowsExceptionWhenDecryptDataHeaderSizeIsInvalidAsync()
        {
            // Arrange
            var key = GetKey();
            var keyVersion = GetKeyVersion();
            var configuration = GetEncryptionConfiguration();
            var encryptor = new Encryptor(configuration, this.mockKeyVaultSecretClientFactory.Object, GetMemoryCache());
            string? base64String = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("Hello World"));

            // Act & Assert
            await Assert.ThrowsAsync<InvalidDataException>(() => encryptor.DecryptAsync(base64String));
        }

        /// <summary>
        /// Encryptor tests when decryption data preamble is invalid.
        /// </summary>
        /// <returns></returns>
        [Fact]
        [Trait("Category", "Unit")]
        public async Task EncryptorThrowsExceptionWhenDecryptDataPreambleIsInvalidAsync()
        {
            // Arrange
            var key = GetKey();
            var keyVersion = GetKeyVersion();
            var configuration = GetEncryptionConfiguration();
            var encryptor = new Encryptor(configuration, this.mockKeyVaultSecretClientFactory.Object, GetMemoryCache());
            string? base64String = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("ZW5jcvLQK/EWcR+z/vB8wUZxUeWyyy6JoKpn1MHL4eT1tqFwOTU1NGU1NTFmYzllNDU4ZWE4Y2U2ZjZiZTBh"));
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => encryptor.DecryptAsync(base64String));
        }

        /// <summary>
        /// Encryptor tests for encryption and decryption.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [Theory]
        [MemberData(nameof(EncryptorTestsData))]
        [Trait("Category", "Unit")]
        public async Task EncryptorForTestDataSucceedsAsync(string? input)
        {
            var key = GetKey();
            var keyVersion = GetKeyVersion();
            var configuration = GetEncryptionConfiguration();

            // Setup the mock to return the key version
            this.mockKeyVaultSecretClient.Setup(x => x.GetSecretAsync(It.Is<string>(x => x.Equals(configuration.SecretVersionName)), It.IsAny<CancellationToken>()))
                .ReturnsAsync(keyVersion).Verifiable();

            // Setup the mock to return the key
            this.mockKeyVaultSecretClient.Setup(x => x.GetSecretAsync(It.Is<string>(x => x.Equals(configuration.SecretName)), It.Is<string>(x => x.Equals(keyVersion)), It.IsAny<CancellationToken>()))
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
                this.mockKeyVaultSecretClient.Verify(x => x.GetSecretAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
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
                this.mockKeyVaultSecretClient.Verify(x => x.GetSecretAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce); 
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
        public async Task EncryptorForLargeTestDataSucceedsAsync(string filePath)
        {
            var key = GetKey();
            var keyVersion = GetKeyVersion();
            var configuration = GetEncryptionConfiguration();

            // Setup the mock to return the key version
            this.mockKeyVaultSecretClient.Setup(x => x.GetSecretAsync(It.Is<string>(x => x.Equals(configuration.SecretVersionName)), It.IsAny<CancellationToken>()))
                .ReturnsAsync(keyVersion).Verifiable();

            // Setup the mock to return the key
            this.mockKeyVaultSecretClient.Setup(x => x.GetSecretAsync(It.Is<string>(x => x.Equals(configuration.SecretName)), It.Is<string>(x => x.Equals(keyVersion)), It.IsAny<CancellationToken>()))
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
            this.mockKeyVaultSecretClient.Verify(x => x.GetSecretAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);

            // encrypt again to test cache
            encrypted = await encryptor.EncryptAsync(input);
            Assert.NotEqual(input, encrypted);

            decrypted = await encryptor.DecryptAsync(encrypted);
            Assert.Equal(input, decrypted);

            // Verify that the key was fetched only once from prior encryption
            this.mockKeyVaultSecretClient.Verify(x => x.GetSecretAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        }

        /// <summary>
        /// Get encryption configuration.
        /// </summary>
        /// <returns>EncryptionConfiguration.</returns>
        private static EncryptionConfiguration GetEncryptionConfiguration()
        {
            return new EncryptionConfiguration
            {
                SecretName = "secret--encryption--symmetricKey",
                SecretVersionName = "secret--encryption--symmetricKeyVersion",
            };
        }

        /// <summary>
        /// Get key.
        /// </summary>
        /// <returns>Key.</returns>
        private static string GetKey()
            => Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");

        /// <summary>
        /// Get key version.
        /// </summary>
        /// <returns>Key version.</returns>
        private static string GetKeyVersion()
            => "9554E551FC9E458EA8CE6F6BE0AE4A8B";

        /// <summary>
        /// Get memory cache.
        /// </summary>
        /// <returns></returns>
        private static IMemoryCache GetMemoryCache()
            => new MemoryCache(new MemoryCacheOptions());
    }
}

#pragma warning restore CS8604 // Possible null reference argument.
