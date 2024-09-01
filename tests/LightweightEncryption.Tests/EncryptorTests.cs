// <copyright file="EncryptorTests.cs" owner="Raghu R">
// Copyright (c) Raghu R. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

using LightweightEncryption.KeyVault;
using Moq;

namespace LightweightEncryption.Tests
{
    /// <summary>
    /// Encryptor tests for encryption and decryption, <seealso cref="IEncryptor"/>
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
        public static IEnumerable<object[]> EncryptorTestsData => new List<object[]>
        {
            new object[] { null, },
            new object[] { "1", },
            new object[] { "Hello World", },
            new object[] { "Bellatrix;^GammARaY123# PhoTON*SINGularity  GravitATIONAlWaves", },
            new object[] { "some random thing, some random thing, some random thing, some random thing, some random thing, some random thing" },
        };

        /// <summary>
        /// Encryptor tests for large data.
        /// </summary>
        public static IEnumerable<object[]> EncryptorTestsLargeData => new List<object[]>
        {
            new object[] { Path.Combine("TestData", "testdata128kbfile.json") },
            new object[] { Path.Combine("TestData", "testdata256kbfile.json") },
            new object[] { Path.Combine("TestData", "testdata512kbfile.json") },
        };

        /// <summary>
        /// Encryptor tests for encryption and decryption.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        [Theory]
        [MemberData(nameof(EncryptorTestsData))]
        [Trait("Category", "Unit")]
        public async Task EncryptAsyncTestAsync(string input)
        {

        }
    }
}
