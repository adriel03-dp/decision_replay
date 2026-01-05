namespace DecisionReplay.Application.Interfaces;

/// <summary>
/// Input Validation Service Interface
/// 
/// Provides comprehensive input validation including:
/// - Content filtering
/// - Domain type detection  
/// - Input sanitization
/// - Spam and inappropriate content detection
/// </summary>
public interface IInputValidationService
{
    /// <summary>
    /// Validates user input for decision creation
    /// </summary>
    /// <param name="input">The natural language input to validate</param>
    /// <returns>Validation result with domain type and sanitized input</returns>
    ValidationResult ValidateInput(string input);
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public string? DomainType { get; set; }
    public string? SanitizedInput { get; set; }

    public static ValidationResult Success(string? domainType, string sanitizedInput)
    {
        return new ValidationResult
        {
            IsValid = true,
            DomainType = domainType,
            SanitizedInput = sanitizedInput
        };
    }

    public static ValidationResult Failed(string errorMessage)
    {
        return new ValidationResult
        {
            IsValid = false,
            ErrorMessage = errorMessage
        };
    }
}