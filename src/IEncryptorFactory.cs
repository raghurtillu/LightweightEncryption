// <copyright file="IEncryptorFactory.cs" owner="Raghu R">
// Copyright (c) Raghu R. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

using System;

namespace LightweightEncryption
{
    /// <summary>
    /// Interface for encryptor factory.
    /// </summary>
    public interface IEncryptorFactory
    {
        /// <summary>
        /// Get the encryptor.
        /// </summary>
        /// <returns>IEncryptor.</returns>
        public IEncryptor GetEncryptor();
    }
}
