// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace UnityEngine
{
    // LayerMask allow you to display the LayerMask popup menu in the inspector
    [RequiredByNativeCode(Optional = true, GenerateProxy = true)]
    [NativeHeader("Runtime/BaseClasses/BitField.h")]
    [NativeHeader("Runtime/BaseClasses/TagManager.h")]
    [NativeClass("BitField", "struct BitField;")]
    public struct LayerMask
    {
        [NativeName("m_Bits")]
        private int m_Mask;

        public static implicit operator int(LayerMask mask)
        {
            return mask.m_Mask;
        }

        // implicitly converts an integer to a LayerMask
        public static implicit operator LayerMask(int intVal)
        {
            LayerMask mask;
            mask.m_Mask = intVal;
            return mask;
        }

        // Converts a layer mask value to an integer value.
        public int value
        {
            get { return m_Mask; }
            set { m_Mask = value; }
        }

        // Given a layer number, returns the name of the layer as defined in either a Builtin or a User Layer in the [[wiki:class-TagManager|Tag Manager]]
        [StaticAccessor("GetTagManager()", StaticAccessorType.Dot)]
        [NativeMethod("LayerToString")]
        extern public static string LayerToName(int layer);

        // Given a layer name, returns the layer index as defined by either a Builtin or a User Layer in the [[wiki:class-TagManager|Tag Manager]]
        [StaticAccessor("GetTagManager()", StaticAccessorType.Dot)]
        [NativeMethod("StringToLayer")]
        extern public static int NameToLayer(string layerName);

        // Given a set of layer names, returns the equivalent layer mask for all of them.
        public static int GetMask(params string[] layerNames)
        {
            if (layerNames == null) throw new ArgumentNullException("layerNames");

            int mask = 0;
            foreach (string name in layerNames)
            {
                int layer = NameToLayer(name);

                if (layer != -1)
                    mask |= 1 << layer;
            }
            return mask;
        }
    }
}
