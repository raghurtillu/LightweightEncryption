"""
This module provides a logger for generating encryption keys.
The logger is configured to log messages with INFO level to the console.
Usage:
    1. Import the logger module:
        ```
        ```
    2. Get the logger instance:
        ```
        logger = logger.getLogger('GenerateEncryptionKeys')
        ```
    3. Set the logger level:
        ```
        ```
    4. Log messages using the logger:
        ```
        logger.info('This is an informational message')
        ```
    Note: The logger is configured to log messages with INFO level to the console.
"""

import logging
import sys
import logger

# Create a logger
logger = logging.getLogger('GenerateEncryptionKeys')
logger.setLevel(logging.INFO)

# Create a console handler
console_handler = logging.StreamHandler(sys.stdout)
console_handler.setLevel(logging.INFO)

# Create a formatter and add it to the console handler
formatter = logging.Formatter('%(asctime)s %(message)s')
console_handler.setFormatter(formatter)

# Add the console handler to the logger
logger.addHandler(console_handler)