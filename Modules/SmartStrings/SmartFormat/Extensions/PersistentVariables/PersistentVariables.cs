// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;
using System;
using Unity.SmartStrings.Core.Extensions;

namespace Unity.SmartStrings.PersistentVariables;

/// <summary>
/// Base class for all single source variables.
/// Inherit from this class for storage for a single serialized source value that will send a value changed event when <see cref="Value"/> is changed.
/// This will trigger any localized string that is currently using the variable to update.
/// </summary>
/// <typeparam name="T">The value type to store in this variable.</typeparam>
[Serializable]
[UxmlObject]
public partial class Variable<T> : IVariableValueChanged, ISerializationCallbackReceiver
{
    [SerializeField]
    T m_Value;

    /// <summary>
    /// Raised when <see cref="Value"/> changes.
    /// </summary>
    public event Action<IVariable> ValueChanged;

    /// <summary>
    /// The value for this variable.
    /// Changing this will trigger the <see cref="ValueChanged"/> event.
    /// </summary>
    [UxmlAttribute("value")]
    public T Value
    {
        get => m_Value;
        set
        {
            if (m_Value != null && m_Value.Equals(value))
                return;

            m_Value = value;
            SendValueChangedEvent();
        }
    }

    /// <inheritdoc/>
    public object GetSourceValue(ISelectorInfo _) => Value;

    void SendValueChangedEvent() => ValueChanged?.Invoke(this);

    /// <summary>
    /// Returns the string representation of this variable's value.
    /// </summary>
    /// <returns>The variable's value converted to a string.</returns>
    public override string ToString() => Value.ToString();

    T m_OldValue;

    void ISerializationCallbackReceiver.OnBeforeSerialize()
    {
        m_OldValue = m_Value;
    }

    void ISerializationCallbackReceiver.OnAfterDeserialize()
    {
        // This lets us send value changed events when the user makes changes through the inspector.
        // If an Undo event occurs we will lose the ValueChanged reference though.
        if (m_OldValue != null && !m_OldValue.Equals(m_Value))
        {
            m_OldValue = m_Value;
        }
    }
}

/// <summary>
/// A <see cref="IVariable"/> that holds a single bool value.
/// </summary>
[Serializable]
public partial class BoolVariable : Variable<bool> {}

/// <summary>
/// A <see cref="IVariable"/> that holds a single signed byte value.
/// </summary>
[Serializable]
public partial class SByteVariable : Variable<sbyte> {}

/// <summary>
/// A <see cref="IVariable"/> that holds a single byte value.
/// </summary>
[Serializable]
public partial class ByteVariable : Variable<byte> {}

/// <summary>
/// A <see cref="IVariable"/> that holds a single short value.
/// </summary>
[Serializable]
public partial class ShortVariable : Variable<short> {}

/// <summary>
/// A <see cref="IVariable"/> that holds a single unsigned short value.
/// </summary>
[Serializable]
public partial class UShortVariable : Variable<ushort> {}

/// <summary>
/// A <see cref="IVariable"/> that holds a single integer value.
/// </summary>
[Serializable]
public partial class IntVariable : Variable<int> {}

/// <summary>
/// A <see cref="IVariable"/> that holds a single unsigned integer value.
/// </summary>
[Serializable]
public partial class UIntVariable : Variable<uint> {}

/// <summary>
/// A <see cref="IVariable"/> that holds a single long value.
/// </summary>
[Serializable]
public partial class LongVariable : Variable<long> {}

/// <summary>
/// A <see cref="IVariable"/> that holds a single unsigned long value.
/// </summary>
[Serializable]
public partial class ULongVariable : Variable<ulong> {}

/// <summary>
/// A <see cref="IVariable"/> that holds a single string value.
/// </summary>
[Serializable]
public partial class StringVariable : Variable<string> {}

/// <summary>
/// A <see cref="IVariable"/> that holds a single float value.
/// </summary>
[Serializable]
public partial class FloatVariable : Variable<float> {}

/// <summary>
/// A <see cref="IVariable"/> that holds a single double value.
/// </summary>
[Serializable]
public partial class DoubleVariable : Variable<double> {}

/// <summary>
/// A <see cref="IVariable"/> that can reference an <see cref="UnityEngine.Object"/> instance.
/// </summary>
[Serializable]
public partial class ObjectVariable : Variable<UnityEngine.Object> {}
