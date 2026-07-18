namespace backend.exceptions;

/// <summary>
///     客户端输入验证失败 → HTTP 400
/// </summary>
public sealed class ValidationException : ArgumentException
{
    public ValidationException(string message) : base(message)
    {
    }

    public ValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
///     资源不存在 → HTTP 404
/// </summary>
public sealed class NotFoundException : KeyNotFoundException
{
    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
///     业务规则冲突（重复注册、账户封禁等） → HTTP 409
/// </summary>
public sealed class ConflictException : InvalidOperationException
{
    public ConflictException(string message) : base(message)
    {
    }

    public ConflictException(string message, Exception innerException) : base(message, innerException)
    {
    }
}