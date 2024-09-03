# LightweightEncryption
Lightweight encryption library provides a fast, simple and strong encryption for your data.
It is based on AES-GCM encryption algorithm and provides support for auto-rotation of encryption keys.

A use case for this library is to encrypt <b>P</b>ersonally <b>I</b>dentifiable <b>I</b>nformation (PII) HTTP request/response in a web server to a LogStore or a Database.

This library uses a pseudo master key to derive encryption keys dynamically at run time for each encryption operation, as a result the encryption keys are never stored in memory or persisted.

There is a master key version that keeps track of the master key to allow for auto-rotation of encryption keys.

## Prerequisites

Before you begin, ensure you have met the following requirements:

- You have installed [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
- You are using Visual Studio 2022 or later.
- You have an Azure subscription and key vault to store the pseudo master key and master key version.

## Using LightweightEncryption
There are two parts to using LightweightEncryption:
1. Generating pseudo master key and master key version.
2. Using the pseudo master key and master key version to encrypt and decrypt data.

### Generating pseudo master key and master key version

To generate the pseudo master key and master key version, you can use the `generate_encryptionkeys_azure.py` script located in the `Scripts` folder. This script will create and store the keys in your Azure Key Vault.

#### Steps:

1. **Install the required Python packages**:
   ```python
   pip install azure-identity azure-keyvault-keys
   ```
    
2. **Set up your Azure credentials**:
    Ensure you have the necessary environment variables set for Azure authentication. You can use the Azure CLI to log in:
    
3. **Run the script**:
    Execute the `generate_encryptionkeys_azure.py` script to generate and store the keys:
    
#### Script: `generate_encryptionkeys_azure.py`

This script will create an RSA key for both the pseudo master key and the master key version in your Azure Key Vault. It also sets a rotation policy for the pseudo master key to rotate every 90 days.

Now you are ready to use these keys for encryption and decryption operations in your application.

