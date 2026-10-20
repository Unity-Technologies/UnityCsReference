// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Collections.LowLevel.Unsafe.NotBurstCompatible;

namespace Unity.Entities.Serialization.Binary
{
    unsafe partial class BinaryAdapter :
        IContravariantBinaryAdapter<UnityEngine.Object>
    {
        void IContravariantBinaryAdapter<UnityEngine.Object>.Serialize(IBinarySerializationContext context, UnityEngine.Object value)
        {
            var id = EditorLightShim.Hooks.GetGlobalObjectId(value);
            context.Writer->AddNBC(id);
        }

        object IContravariantBinaryAdapter<UnityEngine.Object>.Deserialize(IBinaryDeserializationContext context)
        {
            context.Reader->ReadNextNBC(out var value);

            return EditorLightShim.Hooks.GlobalObjectIdToObject(value);
        }
    }
}
