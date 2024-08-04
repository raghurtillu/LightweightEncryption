"""
custom_exceptions.py
This file contains custom exception classes.
"""

class AzCliServiceUnavailableException(Exception):
    """
    Exception raised when the Azure CLI service is unavailable.
    """
    pass

class AzCliAccessException(Exception):
    """
    Exception raised when the Azure CLI service is not accessible.
    """
    pass

class AzCliResourceNotFoundException(Exception):
    """
    Exception raised when an Azure CLI resource is not found.
    """
    pass

class AzCliExecutionException(Exception):
    """
    Exception raised when the Azure CLI service encounters an execution error.
    """
    pass