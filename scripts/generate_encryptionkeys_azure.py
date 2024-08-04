"""
generate_encryptionkeys_azure.py

This script generates encryption key and encryption key version and stores them in Azure Key Vault.
The encryption key is a pseudo key which is used to derive the actual encryption key dynamically to encrypt the data.
The encryption key version is used to track the version of the encryption key.
"""

import argparse
import os
import sys
import secrets
from logger import logger
from azure_helpers import select_subscription, group_exists, vault_exists, set_secret_and_get_secret_version
from datetime import datetime, timedelta
from typing import Any

DEFAULT_ENCRYPTION_KEY_LEN = 32
DEFAULT_ENCRYPTION_KEY_NAME = "secret--encryption--symmetricKey"
DEFAULT_ENCRYPTION_KEY_VERSION_NAME = "secret--encryption--symmetricKeyVersion"
DEFAULT_ENCRYPTION_KEY_EXPIRATION = (datetime.now() + timedelta(days=90)).strftime("%Y-%m-%d")

def log_error_and_exit(message: str) -> None:
    """
    Logs the error message and exits the script.
    """
    logger.error(message)
    sys.exit(1)

def validate_date(date: str) -> None:
    """
    Validates the date format.
    """
    try:
        datetime.strptime(date, "%Y-%m-%d")
        # check if the date is in the past
        if datetime.strptime(date, "%Y-%m-%d") < datetime.now():
            log_error_and_exit(f"Invalid expiration date '{date}', should be in the future.")
    except ValueError:
        log_error_and_exit("Incorrect date format, should be 'YYYY-MM-DD'")

def convert_tags(tags: str) -> dict:
    """
    Converts the tags string to a dictionary.
    """
    tags_dict = {}
    tags_list = tags.split(",")
    for tag in tags_list:
        key, value = tag.split("=")
        tags_dict[key] = value
    return tags_dict

def generate_encryption_keys(args: Any) -> None:
    """
    Generate encryption key and encryption key version name.
    """
    subscription_id = args.subscription_id
    resource_group = args.resource_group
    location = args.location
    vault_name = args.vault_name
    key_name = args.key_name if args.key_name else DEFAULT_ENCRYPTION_KEY_NAME
    key_version_name = args.key_version_name if args.key_version_name else DEFAULT_ENCRYPTION_KEY_VERSION_NAME
    expiration = args.expiration if args.expiration else DEFAULT_ENCRYPTION_KEY_EXPIRATION; validate_date(expiration)
    tags = convert_tags(args.tags) if args.tags else {"user": os.getlogin()}
    dry_run = args.dry_run if args.dry_run else True

    logger.info("Running the script '%s' with following settings:", __file__)
    logger.info("  Subscription id: '%s'", subscription_id)
    logger.info("  Resource group: '%s'", resource_group)
    logger.info("  Location: '%s'", location)
    logger.info("  Vault name: '%s'", vault_name)
    logger.info("  Key name: '%s'", key_name)
    logger.info("  Key version name: '%s'", key_version_name)
    logger.info("  Expiration: '%s'", expiration)
    logger.info("  Tags: '%s'", tags)
    logger.info("  Dry run: '%s'", dry_run)

    # Create the encryption key
    logger.info("Generating encryption key and encryption key version...")
    key = secrets.token_hex(DEFAULT_ENCRYPTION_KEY_LEN)

    if dry_run:
        logger.info("Dry run mode, no changes will be made.")
        logger.info(f"Encryption key '{key_name}' will be created with a value of '{key}'.")
        return

    # Set the subscription
    select_subscription(subscription_id)
    
    # Check if the resource group exists
    if not group_exists(resource_group):
        log_error_and_exit("Resource group '{}' does not exist".format(resource_group))

    # Check if the key vault exists
    if not vault_exists(resource_group, vault_name):
        log_error_and_exit("Key vault '{}' does not exist".format(vault_name))

    # set the encryption key
    key_id = set_secret_and_get_secret_version(vault_name, key_name, key, "application/octet-stream", expiration, tags)
    
    # set the encryption key version with the encryption key version        
    key_version_id = set_secret_and_get_secret_version(vault_name, key_version_name, key_id, "application/octet-stream", expiration, tags)

    logger.info(f"Encryption key '{key_name}' and Encryption key version name '{key_version_name}' generated successfully.")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Generate encryption key and key version and store them in Azure Key Vault")
    parser.add_argument('-s','--subscription-id', required=True, help="Subscription id")
    parser.add_argument('-r','--resource-group', required=True, help="Name of the resource group")
    parser.add_argument('-l','--location', required=True, help="Location of the key vault")
    parser.add_argument('-v','--vault-name', required=True, help="Name of the key vault")
    parser.add_argument('-k','--key-name', required=False, help="Name of the key, if not specified, 'secret--encryption--symmetricKey' will be used")
    parser.add_argument('-y','--key-version-name', required=False, help="Name of the key version, if not specified, 'secret--encryption--symmetricKeyVersion' will be used")
    parser.add_argument('-e', '--expiration', required=False, help="Expiration date for the key in ISO 8601 format, 'YYYY-MM-DD', if not specified, the key will expire in 3 months from the date of creation")
    parser.add_argument('-t', '--tags', required=False, help="Tags to be added to the key vault")
    parser.add_argument('-d','--dry_run', required=False, help="Run the script in dry run mode (no changes will be made), this is the default if not specified")
    try:
        args = parser.parse_args()
        generate_encryption_keys(args)
    except Exception as e:
        log_error_and_exit("An error occurred while parsing the arguments: %s", str(e))
