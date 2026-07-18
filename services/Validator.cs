using System.ComponentModel.DataAnnotations;
using backend.Utils;

namespace backend.services;

public class EmailValidator
{
    public static EmailValidationResult Validate(string email)
    {
        return string.IsNullOrWhiteSpace(email) ? EmailValidationResult.Empty :
            !new EmailAddressAttribute().IsValid(email) ? EmailValidationResult.InvalidFormat :
            DisposableDomains.IsDisposable(email.Split("@")[1]) ? EmailValidationResult.IsDisposable :
            EmailValidationResult.Valid;
    }
}

public enum EmailValidationResult
{
    Valid,
    Empty,
    InvalidFormat,
    IsDisposable
}