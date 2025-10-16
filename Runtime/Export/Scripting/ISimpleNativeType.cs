// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// TODO: Add #ifdef here

using Unity.DataModel;
using UnityEngine.SceneManagement;

namespace UnityEngine
{
    internal interface ISimpleNativeType
    {
        internal void UDMWriteNativeObject(Accessor objectModelAccessor, SerializeInstructionFlags options);
        internal void UDMReadNativeObject(ConstAccessor objectModelAccessor, SerializeInstructionFlags options, UDMRefResolver resolver);
    };
}
