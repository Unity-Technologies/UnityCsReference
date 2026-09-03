// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#nullable enable
using System;
namespace Unity.AcceleratorClient.Models;

// TValue and TError must be distinct types: Result<T, T> makes the implicit conversions
// and Match overloads ambiguous. Wrap one side in a single-field record if they'd collide.
internal readonly struct Result<TValue, TError>
{
    private readonly TValue m_Value;
    private readonly TError m_Error;

    public bool IsSuccess { get; }

    private Result(TValue value, TError error, bool isSuccess)
    {
        m_Value = value;
        m_Error = error;
        IsSuccess = isSuccess;
    }

    public TValue Value =>
        IsSuccess
            ? m_Value
            : throw new InvalidOperationException(
                "Cannot read Value from a failed Result; check IsSuccess first or use Match.");

    public TError Error =>
        !IsSuccess
            ? m_Error
            : throw new InvalidOperationException(
                "Cannot read Error from a successful Result; check IsSuccess first or use Match.");

    public static implicit operator Result<TValue, TError>(TValue value) =>
        new(value, default!, isSuccess: true);

    public static implicit operator Result<TValue, TError>(TError error) =>
        new(default!, error, isSuccess: false);

    // Use when TValue is an interface type: implicit conversions don't fire for interfaces.
    public static Result<TValue, TError> Ok(TValue value) =>
        new(value, default!, isSuccess: true);

    // Use when TError is an interface type: implicit conversions don't fire for interfaces.
    public static Result<TValue, TError> Fail(TError error) =>
        new(default!, error, isSuccess: false);

    public TOut Match<TOut>(Func<TValue, TOut> onSuccess, Func<TError, TOut> onError) =>
        IsSuccess ? onSuccess(m_Value) : onError(m_Error);
}

// Void-success / typed-error variant of Result<TValue, TError>.
internal readonly struct Result<TError>
{
    private readonly TError m_Error;

    public bool IsSuccess { get; }

    private Result(TError error, bool isSuccess)
    {
        m_Error = error;
        IsSuccess = isSuccess;
    }

    public static Result<TError> Success => new(default!, isSuccess: true);

    // Use when TError is an interface type: implicit conversion can't fire for interfaces.
    public static Result<TError> Fail(TError error) => new(error, isSuccess: false);

    public TError Error =>
        !IsSuccess
            ? m_Error
            : throw new InvalidOperationException(
                "Cannot read Error from a successful Result; check IsSuccess first or use Match.");

    public static implicit operator Result<TError>(TError error) =>
        new(error, isSuccess: false);

    public TOut Match<TOut>(Func<TOut> onSuccess, Func<TError, TOut> onError) =>
        IsSuccess ? onSuccess() : onError(m_Error);
}
