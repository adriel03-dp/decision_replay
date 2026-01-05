using DecisionReplay.Application.Interfaces;
using System.Text.RegularExpressions;

namespace DecisionReplay.Infrastructure.Services;

/// <summary>
/// Input Validation and Domain Detection Service
/// 
/// Provides:
/// - Content filtering and validation
/// - Domain type detection
/// - Input sanitization
/// - Inappropriate content detection
/// </summary>
public class InputValidationService : IInputValidationService
{
    private static readonly HashSet<string> ProhibitedWords = new(StringComparer.OrdinalIgnoreCase)
    {
        // Add prohibited words here - keeping it minimal for example
        "hack", "exploit", "illegal", "fraud", "scam", "spam", "adult-content",
        "violence", "hate-speech", "terrorism", "drugs", "weapons"
    };

    private static readonly Dictionary<string, List<string>> DomainKeywords = new()
    {
        ["Software Development"] = new()
        {
            "app", "application", "software", "code", "programming", "development", "api", "database",
            "frontend", "backend", "mobile", "web", "react", "angular", "vue", "node", "python",
            "java", "c#", "javascript", "typescript", "framework", "library", "microservice",
            "deployment", "testing", "debugging", "version", "git", "repository", "agile", "scrum"
        },
        ["Business Strategy"] = new()
        {
            "business", "strategy", "market", "competition", "revenue", "profit", "investment",
            "funding", "venture", "startup", "expansion", "growth", "acquisition", "merger",
            "partnership", "customer", "client", "sales", "marketing", "brand", "product launch"
        },
        ["Construction"] = new()
        {
            "build", "construction", "house", "building", "architecture", "contractor", "foundation",
            "materials", "concrete", "steel", "blueprint", "permit", "zoning", "renovation",
            "plumbing", "electrical", "roofing", "flooring", "windows", "doors"
        },
        ["Finance"] = new()
        {
            "finance", "financial", "investment", "loan", "mortgage", "budget", "cost", "expense",
            "revenue", "profit", "loss", "accounting", "tax", "bank", "portfolio", "stocks",
            "bonds", "cryptocurrency", "trading", "insurance"
        },
        ["Healthcare"] = new()
        {
            "health", "medical", "hospital", "clinic", "patient", "treatment", "therapy", "medicine",
            "surgery", "diagnosis", "doctor", "nurse", "pharmaceutical", "clinical trial",
            "healthcare system", "telemedicine"
        },
        ["Education"] = new()
        {
            "education", "school", "university", "college", "student", "teacher", "curriculum",
            "learning", "training", "course", "degree", "certification", "online education",
            "e-learning", "classroom"
        },
        ["Manufacturing"] = new()
        {
            "manufacturing", "production", "factory", "assembly", "supply chain", "logistics",
            "inventory", "quality control", "automation", "machinery", "equipment", "warehouse"
        },
        ["Personal Planning"] = new()
        {
            "personal", "family", "life", "career", "retirement", "wedding", "vacation", "move",
            "relocate", "purchase", "buying", "selling", "hobby", "relationship"
        }
    };

    public ValidationResult ValidateInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return ValidationResult.Failed("Input cannot be empty.");
        }

        if (input.Length < 10)
        {
            return ValidationResult.Failed("Please provide more details (minimum 10 characters).");
        }

        if (input.Length > 5000)
        {
            return ValidationResult.Failed("Input is too long. Please keep it under 5000 characters.");
        }

        // Check for prohibited content
        var prohibitedCheck = CheckForProhibitedContent(input);
        if (!prohibitedCheck.IsValid)
        {
            return prohibitedCheck;
        }

        // Check for spam-like patterns
        var spamCheck = CheckForSpamPatterns(input);
        if (!spamCheck.IsValid)
        {
            return spamCheck;
        }

        // Detect domain type
        var domainType = DetectDomainType(input);

        return ValidationResult.Success(domainType, SanitizeInput(input));
    }

    private ValidationResult CheckForProhibitedContent(string input)
    {
        var words = input.ToLower().Split(' ', '.', ',', '!', '?', ';', ':');

        foreach (var word in words)
        {
            if (ProhibitedWords.Contains(word.Trim()))
            {
                return ValidationResult.Failed($"Input contains inappropriate content. Please review your decision description.");
            }
        }

        // Check for suspicious patterns
        if (Regex.IsMatch(input, @"https?://", RegexOptions.IgnoreCase))
        {
            return ValidationResult.Failed("URLs are not allowed in decision descriptions.");
        }

        if (Regex.IsMatch(input, @"\b\d{16}\b|\b\d{4}[-\s]?\d{4}[-\s]?\d{4}[-\s]?\d{4}\b", RegexOptions.IgnoreCase))
        {
            return ValidationResult.Failed("Please do not include sensitive information like credit card numbers.");
        }

        if (Regex.IsMatch(input, @"\b\d{3}[-.]?\d{2}[-.]?\d{4}\b", RegexOptions.IgnoreCase))
        {
            return ValidationResult.Failed("Please do not include sensitive information like Social Security numbers.");
        }

        return ValidationResult.Success(null, input);
    }

    private ValidationResult CheckForSpamPatterns(string input)
    {
        // Check for excessive repetition
        if (Regex.IsMatch(input, @"(.)\1{10,}", RegexOptions.IgnoreCase))
        {
            return ValidationResult.Failed("Input contains excessive repetition. Please provide a meaningful decision description.");
        }

        // Check for excessive capitalization
        var upperCount = input.Count(char.IsUpper);
        var letterCount = input.Count(char.IsLetter);
        if (letterCount > 0 && (double)upperCount / letterCount > 0.5)
        {
            return ValidationResult.Failed("Please avoid excessive use of capital letters.");
        }

        // Check for excessive special characters
        var specialCharCount = input.Count(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c));
        if (specialCharCount > input.Length * 0.3)
        {
            return ValidationResult.Failed("Input contains too many special characters. Please use normal text.");
        }

        return ValidationResult.Success(null, input);
    }

    private string DetectDomainType(string input)
    {
        var inputLower = input.ToLower();
        var domainScores = new Dictionary<string, int>();

        foreach (var domain in DomainKeywords)
        {
            var score = domain.Value.Count(keyword => inputLower.Contains(keyword.ToLower()));
            if (score > 0)
            {
                domainScores[domain.Key] = score;
            }
        }

        if (domainScores.Any())
        {
            return domainScores.OrderByDescending(x => x.Value).First().Key;
        }

        return "General Planning";
    }

    private string SanitizeInput(string input)
    {
        // Remove potentially harmful characters while preserving readability
        input = Regex.Replace(input, @"[<>\""]", "", RegexOptions.IgnoreCase);

        // Normalize whitespace
        input = Regex.Replace(input, @"\s+", " ");

        return input.Trim();
    }
}