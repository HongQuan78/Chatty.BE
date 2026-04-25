using System.Collections.Generic;

namespace Chatty.BE.Application.Common;

public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    public string? ErrorCode { get; }
    public IDictionary<string, string[]>? ValidationErrors { get; }

    protected Result(bool isSuccess, string? error, string? errorCode, IDictionary<string, string[]>? validationErrors = null)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorCode = errorCode;
        ValidationErrors = validationErrors;
    }

    public static Result Success() => new(true, null, null);
    public static Result Failure(string error, string? code = null) => new(false, error, code);
    public static Result ValidationError(IDictionary<string, string[]> errors) 
        => new(false, "One or more validation errors occurred.", "VALIDATION_ERROR", errors);
}

public class Result<T> : Result
{
    public T? Value { get; }

    private Result(bool isSuccess, T? value, string? error, string? errorCode, IDictionary<string, string[]>? validationErrors = null)
        : base(isSuccess, error, errorCode, validationErrors)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new(true, value, null, null);
    public new static Result<T> Failure(string error, string? code = null) => new(false, default, error, code);
    public new static Result<T> ValidationError(IDictionary<string, string[]> errors) 
        => new(false, default, "One or more validation errors occurred.", "VALIDATION_ERROR", errors);

    public Result<TNew> Map<TNew>(Func<T, TNew> mapper) =>
        IsSuccess ? Result<TNew>.Success(mapper(Value!)) : Result<TNew>.Failure(Error!, ErrorCode);

    public async Task<Result<TNew>> MapAsync<TNew>(Func<T, Task<TNew>> mapper) =>
        IsSuccess ? Result<TNew>.Success(await mapper(Value!)) : Result<TNew>.Failure(Error!, ErrorCode);
}
