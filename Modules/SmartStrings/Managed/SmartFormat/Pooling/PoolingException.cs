// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.

using System;
using System.Runtime.Serialization;

namespace Unity.SmartStrings.Pooling;

/// <summary>
/// Represents an <i>Exception</i> thrown by the pooling subsystem.
/// </summary>
[Serializable]
public class PoolingException : InvalidOperationException
{
    /// <summary>
    /// Creates a instance of a <see cref="PoolingException"/>.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="poolType"></param>
    public PoolingException(string message, Type poolType) : base(message)
    {
        PoolType = poolType;
    }

    ///<inheritdoc/>
    protected PoolingException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        PoolType = typeof(object);
    }

    /// <summary>
    /// Gets the type of pool, which threw the exception.
    /// </summary>
    public Type PoolType { get; }
}
