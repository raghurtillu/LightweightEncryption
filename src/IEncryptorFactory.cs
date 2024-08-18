// This is free and unencumbered software released into the public domain.

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
