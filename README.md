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
- You have an [Azure](https://azure.microsoft.com) subscription and [keyvault](https://azure.microsoft.com/en-us/products/key-vault) to store the pseudo master key and master key version.

## Using LightweightEncryption
One can use this either via nuget or by cloning this repo and taking compile time dependency.

### Using Nuget

There are two parts to using LightweightEncryption:
1. Generating pseudo master key and master key version.
2. Using the pseudo master key and master key version to encrypt and decrypt data.

### Generating pseudo master key and master key version

You can use the `generate_encryptionkeys_azure.py` script located in the `Scripts` folder. This script will create and store the keys in your Azure Key Vault.
This script will generate a 32 byte pseudo master key and the version of the pseudo master key is stored in the master key version name.

#### Steps:

1. **Set up your Azure subscription and keyvault**:
    - Create a resource group in your Azure subscription.
    - Create a key vault in your resource group.
    - Create a service principal with access to the key vault. This service principal could either be your identity or a managed identity.
    - Assign the service principal the necessary permissions to the key vault.
    - In particular `Get, List, Set` permissions on secrets are required.

2. **Install the required Python packages**:
   ```python
   pip install scripts\requirements.txt
   ```

3. **Run the script**:
    Execute the `generate_encryptionkeys_azure.py` script to generate and store the keys:
    - Provide the necessary parameters to the script:
        - `--subscription-id`: Azure subscription id, this parameter is required.
        - `--resource-group`: Azure resource group in which the keyvault resides, this parameter is required.
        - `--location`: Azure region, this parameter is required.
        - `--vault-name`: Azure keyvault, this parameter is required.
        - `--key-name`: Optional parameter to save the pseudo master key, if not specified 'secret--encryption--symmetricKey' will be used.
        - `--key-version-name`: Optional parameter to track the pseudo master key version, if not specified, 'secret--encryption--symmetricKeyVersion' will be used.
        - `--expiration`: Optional parameter to set the expiration time for the pseudo master key in ISO 8601 format, 'YYYY-MM-DD', if not specified, the key will expire in 3 months from the date of creation.
        - `--tags`: Optional parameter to set tags for the pseudo master key, if not specified, the current login user name will be used.
        - `--dry-run`: Optional parameter to run the script in dry run mode, no changes will be applied.
    ```python
1. python generate_encryptionkeys_azure.py --subscription-id <subscription-id> --resource-group <resource-group> --location <location> --vault-name <vault-name> --key-name <key-name> --key-version-name <key-version-name> --expiration <expiration> --tags <tags> --dry-run
    ```
4. **Verify the keys**:
    - Go to the Azure portal and navigate to the key vault.
    - Verify that the pseudo master key and master key version are created.
5. **Encrypt/Decrypt**
1. Add the LightweightEncryption NuGet package to your project.

