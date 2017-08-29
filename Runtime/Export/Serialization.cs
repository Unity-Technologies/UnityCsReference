// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using scm = System.ComponentModel;
using uei = UnityEngine.Internal;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;
using UsedByNativeCodeAttribute = UnityEngine.Scripting.UsedByNativeCodeAttribute;

using System;
namespace UnityEngine
{
    [System.Obsolete("Use SerializeField on the private variables that you want to be serialized instead")]
    [RequiredByNativeCode]
    public sealed partial class SerializePrivateVariables : Attribute
    {
    }

    [RequiredByNativeCode]
    public sealed partial class SerializeField : Attribute
    {
    }

    [RequiredByNativeCode]
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class PreferBinarySerialization : Attribute
    {
    }

    [RequiredByNativeCode]
    public interface ISerializationCallbackReceiver
    {
        [RequiredByNativeCode]
        void OnBeforeSerialize();

        [RequiredByNativeCode]
        void OnAfterDeserialize();
    }
}
