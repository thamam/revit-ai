"""
Logging Infrastructure
Provides structured logging for all RevitAI operations
"""

import os
import logging
from logging.handlers import RotatingFileHandler
from datetime import datetime
from typing import Optional
import sys

from config_manager import get_config_manager


# Log levels
DEBUG = logging.DEBUG
INFO = logging.INFO
WARNING = logging.WARNING
ERROR = logging.ERROR
CRITICAL = logging.CRITICAL


def get_log_directory() -> str:
    """
    Get the log directory path

    Returns:
        Path to logs directory
    """
    # Use %APPDATA%/pyRevit/RevitAI/logs/
    appdata = os.environ.get('APPDATA')
    if appdata:
        log_dir = os.path.join(appdata, 'pyRevit', 'RevitAI', 'logs')
    else:
        # Fallback to temp directory
        log_dir = os.path.join(os.path.expanduser('~'), '.revit-ai', 'logs')

    # Create directory if it doesn't exist
    if not os.path.exists(log_dir):
        os.makedirs(log_dir, exist_ok=True)

    return log_dir


def get_log_file_path() -> str:
    """
    Get the main log file path

    Returns:
        Path to log file
    """
    log_dir = get_log_directory()
    log_file = os.path.join(log_dir, 'revit_ai.log')
    return log_file


def setup_logger(
    name: str = 'RevitAI',
    log_file: Optional[str] = None,
    level: int = INFO,
    max_bytes: int = 10 * 1024 * 1024,  # 10MB
    backup_count: int = 5
) -> logging.Logger:
    """
    Set up a logger with rotating file handler

    Args:
        name: Logger name
        log_file: Path to log file (default: auto-determined)
        level: Logging level
        max_bytes: Maximum log file size before rotation
        backup_count: Number of backup files to keep

    Returns:
        Configured logger instance
    """
    # Get or create logger
    logger = logging.getLogger(name)

    # Avoid adding handlers multiple times
    if logger.handlers:
        return logger

    logger.setLevel(level)

    # Determine log file path
    if log_file is None:
        log_file = get_log_file_path()

    try:
        # Create rotating file handler
        file_handler = RotatingFileHandler(
            log_file,
            maxBytes=max_bytes,
            backupCount=backup_count,
            encoding='utf-8'
        )

        # Create formatter
        formatter = logging.Formatter(
            '%(asctime)s - %(name)s - %(levelname)s - %(message)s',
            datefmt='%Y-%m-%d %H:%M:%S'
        )
        file_handler.setFormatter(formatter)

        # Add handler to logger
        logger.addHandler(file_handler)

    except Exception as e:
        # If file logging fails, fall back to console only
        print(f"Warning: Could not set up file logging: {e}")
        console_handler = logging.StreamHandler(sys.stdout)
        console_handler.setFormatter(formatter)
        logger.addHandler(console_handler)

    # Also log to console (pyRevit output window)
    console_handler = logging.StreamHandler(sys.stdout)
    console_formatter = logging.Formatter(
        '[%(levelname)s] %(message)s'
    )
    console_handler.setFormatter(console_formatter)
    logger.addHandler(console_handler)

    return logger


def get_logger(name: str = 'RevitAI') -> logging.Logger:
    """
    Get logger instance (creates if doesn't exist)

    Args:
        name: Logger name

    Returns:
        Logger instance
    """
    logger = logging.getLogger(name)

    # Set up logger if not already configured
    if not logger.handlers:
        # Load log level from config
        config = get_config_manager()
        try:
            log_level_str = config.get('logging.log_level', 'INFO')
            log_level = getattr(logging, log_level_str.upper(), INFO)
        except Exception:
            log_level = INFO

        # Load max file size and backup count
        try:
            max_size_mb = config.get('logging.max_log_size_mb', 10)
            max_bytes = max_size_mb * 1024 * 1024
            backup_count = config.get('logging.backup_count', 5)
        except Exception:
            max_bytes = 10 * 1024 * 1024
            backup_count = 5

        setup_logger(name, level=log_level, max_bytes=max_bytes, backup_count=backup_count)

    return logger


class OperationLogger:
    """
    Helper class for logging operations with context

    Provides structured logging for RevitAI operations including:
    - Operation start/end
    - Performance metrics
    - Error tracking
    """

    def __init__(self, operation_name: str, logger: Optional[logging.Logger] = None):
        """
        Initialize operation logger

        Args:
            operation_name: Name of the operation being logged
            logger: Logger instance (default: get_logger())
        """
        self.operation_name = operation_name
        self.logger = logger or get_logger()
        self.start_time = None
        self.end_time = None

    def start(self, **context):
        """
        Log operation start

        Args:
            **context: Additional context to log
        """
        self.start_time = datetime.now()

        context_str = ", ".join([f"{k}={v}" for k, v in context.items()])
        self.logger.info(f"[START] {self.operation_name} | {context_str}")

    def end(self, success: bool = True, **context):
        """
        Log operation end

        Args:
            success: Whether operation succeeded
            **context: Additional context to log (e.g., result count)
        """
        self.end_time = datetime.now()

        if self.start_time:
            duration = (self.end_time - self.start_time).total_seconds()
            duration_str = f"{duration:.2f}s"
        else:
            duration_str = "unknown"

        status = "SUCCESS" if success else "FAILED"
        context_str = ", ".join([f"{k}={v}" for k, v in context.items()])

        self.logger.info(
            f"[END] {self.operation_name} | status={status}, duration={duration_str} | {context_str}"
        )

    def error(self, exception: Exception, **context):
        """
        Log operation error

        Args:
            exception: Exception that occurred
            **context: Additional context
        """
        context_str = ", ".join([f"{k}={v}" for k, v in context.items()])

        self.logger.error(
            f"[ERROR] {self.operation_name} | {exception.__class__.__name__}: {exception} | {context_str}",
            exc_info=True
        )

        # Also end the operation as failed
        self.end(success=False, error=str(exception))

    def __enter__(self):
        """Context manager entry"""
        return self

    def __exit__(self, exc_type, exc_val, exc_tb):
        """Context manager exit"""
        if exc_type is not None:
            self.error(exc_val)
        return False  # Don't suppress exceptions


def log_llm_request(prompt: str, context: dict, response: dict, duration: float):
    """
    Log LLM API request/response

    Args:
        prompt: User's prompt
        context: Revit context sent to LLM
        response: LLM response
        duration: Request duration in seconds
    """
    logger = get_logger()

    # Anonymize sensitive data
    safe_prompt = prompt[:100] + "..." if len(prompt) > 100 else prompt
    safe_context = {k: v for k, v in context.items() if k not in ['project_name', 'user_name']}

    logger.info(
        f"[LLM] Request completed | duration={duration:.2f}s | "
        f"prompt_length={len(prompt)} | operation={response.get('operation', 'unknown')}"
    )

    logger.debug(f"[LLM] Prompt: {safe_prompt}")
    logger.debug(f"[LLM] Context: {safe_context}")
    logger.debug(f"[LLM] Response: {response}")


def log_revit_operation(operation_type: str, element_count: int, success: bool, error: Optional[str] = None):
    """
    Log Revit API operation

    Args:
        operation_type: Type of operation (e.g., "create_dimensions")
        element_count: Number of elements processed
        success: Whether operation succeeded
        error: Error message if failed
    """
    logger = get_logger()

    status = "SUCCESS" if success else "FAILED"
    log_message = f"[REVIT] {operation_type} | status={status}, elements={element_count}"

    if error:
        log_message += f" | error={error}"

    if success:
        logger.info(log_message)
    else:
        logger.error(log_message)


def test_logger():
    """Test function for logger"""
    print("Testing Logger...")

    logger = get_logger()

    # Test different log levels
    logger.debug("This is a debug message")
    logger.info("This is an info message")
    logger.warning("This is a warning message")
    logger.error("This is an error message")

    # Test operation logger
    op_logger = OperationLogger("test_operation")
    op_logger.start(user="test", scope="current_view")

    # Simulate some work
    import time
    time.sleep(0.1)

    op_logger.end(success=True, result_count=42)

    # Test context manager
    try:
        with OperationLogger("test_with_error") as op:
            op.start()
            raise ValueError("Test error")
    except ValueError:
        pass  # Expected

    # Test LLM logging
    log_llm_request(
        prompt="Add dimensions to all rooms",
        context={"levels": ["Level 1", "Level 2"]},
        response={"operation": "create_dimensions"},
        duration=1.23
    )

    # Test Revit operation logging
    log_revit_operation("create_dimensions", element_count=10, success=True)
    log_revit_operation("create_tags", element_count=5, success=False, error="API limit exceeded")

    print(f"\n✓ Logs written to: {get_log_file_path()}")

    return True


if __name__ == '__main__':
    # Run test when module is executed directly
    test_logger()
