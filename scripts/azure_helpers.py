"""
azure_helpers.py

This script contains helper functions for working with Azure services.
"""

import io
import json
import logging
import sys
import tenacity
from typing import Any
from azure.cli.core import get_default_cli
from custom_exceptions import AzCliAccessException, AzCliExecutionException, AzCliServiceUnavailableException, AzCliResourceNotFoundException
from logger import logger

def select_subscription(subscription_id: str) -> None:
    """
    This function checks if the subscription_id is set to be the current active in az cli. If not, it sets it.
    Will throw if subscription_id is not in the account list.
    """
    args = ['account', 'list', '--query', '[?isDefault]']
    subscription = next((x for x in az(args)), None)
    if not subscription:
        raise AzCliAccessException(f"Not Logged into az cli or can't access subscription: {subscription_id}")

    if subscription['id'].lower() != subscription_id.lower():
        args = ['account', 'set', '--subscription', subscription_id]
        az(args)

    logger.info("Selected subscription: '%s'", subscription_id)

def group_exists(resource_group: str) -> bool:
    """
    This function checks if the resource group exists.
    """
    try:
        args = ['group', 'show', '--name', resource_group]
        return az(args) is not None
    except AzCliResourceNotFoundException:
        return False

def vault_exists(resource_group: str, vault_name: str) -> bool:
    """
    This function checks if the key vault exists.
    """
    try:
        args = ['keyvault', 'show', '--name', vault_name, '--resource-group', resource_group]
        return az(args) is not None
    except AzCliResourceNotFoundException:
        return False

def set_secret_and_get_secret_version(key_vault: str, secret_name: str, secret_value: str, content_type: str, expiration_date: str, tags: dict = None) -> str:
    """
    This function sets a secret in the key vault and returns the secret version.

    Args:
        key_vault (str): The name of the key vault.
        secret_name (str): The name of the secret.
        secret_value (str): The value of the secret.
        content_type (str): The content type of the secret.
        expiration_date (str, optional): The expiration date of the secret.
        tags (dict, optional): A dictionary of tags to associate with the secret. Defaults to None.
    """
    secret_id_list = set_secret(key_vault, secret_name, secret_value, content_type, expiration_date, tags)
    return secret_id_list[0].split('/')[-1]

def set_secret(key_vault: str, secret_name: str, secret_value: str, content_type: str, expiration_date: str, tags: dict = None) -> Any:
    args = ['keyvault', 'secret', 'set', '--query', '[id]',
            '--vault-name', key_vault, 
            '--name', secret_name, 
            '--value', secret_value, 
            '--content-type', content_type]
    
    if expiration_date:
        args.extend(['--expires', expiration_date])

    if tags:
        for key, value in tags.items():
            args.extend(['--tags', f"{key}={value}"])
    
    # set secret in key vault
    # az keyvault secret set --name MySecretName --vault-name MyKeyVault --value MyVault --tags "user=MyUser" --expires "2024-12-31"
    return az(args)

@tenacity.retry(wait=tenacity.wait_fixed(5), stop=tenacity.stop_after_attempt(5), retry=tenacity.retry_if_exception_type(AzCliServiceUnavailableException))
def az(args: list[str], redact_values: list[str]=None) -> Any:
    """
    Executes an Azure CLI command using the provided arguments.

    Args:
        args (list[str]): The list of arguments to pass to the Azure CLI command.
        redact_values (list[str], optional): A list of values to redact from the command output. Defaults to None.
    Returns:
        Any: The result of the Azure CLI command.

    """
    # Runs the az command in-process using imported azure.cli.main
    az_cli = get_default_cli()
    # Redirect stdout to a buffer
    az_cli.out_file = io.TextIOWrapper(io.BytesIO(), sys.stdout.encoding)
    args_with_output = args + ['--output', 'json']
    args_to_log = ' '.join(args_with_output)

    if redact_values:
        for redact in redact_values:
            args_to_log = args_to_log.replace(redact, '*****')
    
    logger.info('az - Executing az %s', args_to_log)
    
    # invoke az cli command and capture output
    exit_code = 0
    try:
        # suppress logging during az cli command execution
        logging.disable(logging.ERROR)
        exit_code = az_cli.invoke(args_with_output)
    except SystemExit as system_exit:
        if system_exit.code == 3:
            # az cli exit code 3 == ResourceNotFound (404)
            raise AzCliResourceNotFoundException(f'Exit Code:{exit_code}') from system_exit

        if system_exit.code == 1 and az_cli.result and "HTTPSConnectionPool(host='management.azure.com', port=443): Max retries exceeded with url" in str(az_cli.result.error):
            raise AzCliServiceUnavailableException(f'Exit Code:{exit_code}') from system_exit

        logger.error('az - Command failed with exit code: %s', system_exit.code)
        raise AzCliExecutionException(f'Exit Code:{exit_code}') from system_exit
    finally:
        logging.disable(logging.NOTSET)
    
    az_cli.out_file.seek(0)
    output = az_cli.out_file.read()
    if output and exit_code == 0:
        try:
            # if output is json, return json object
            return json.loads(output)
        except json.JSONDecodeError:
            logger.error('az - Failed to decode output: %s', output)
            raise AzCliExecutionException(f'Exit Code:{exit_code}')

    # get detailed error from CommandResultItem.error
    # https://github.com/Microsoft/knack/blob/master/knack/cli.py#L214-L219
    if az_cli.result and az_cli.result.error:
        logger.error('az - Command failed with error: %s', az_cli.result.error)
        raise AzCliExecutionException(f'Exit Code:{exit_code}, Result:{str(az_cli.result.error)}')

    return None
