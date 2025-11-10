"""
RevitAI Exception Hierarchy
Custom exceptions for error handling across the application
"""


class RevitAIError(Exception):
    """Base exception for all RevitAI errors"""
    pass


class APIError(RevitAIError):
    """Claude API errors (timeout, rate limit, network)"""
    pass


class ValidationError(RevitAIError):
    """Safety validation failures (operation not allowed)"""
    pass


class RevitAPIError(RevitAIError):
    """Revit API operation failures"""
    pass


class ConfigurationError(RevitAIError):
    """Configuration file or API key errors"""
    pass


class PreviewError(RevitAIError):
    """Preview graphics generation errors"""
    pass
