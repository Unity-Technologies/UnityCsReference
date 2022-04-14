// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Bindings;
using System.Runtime.InteropServices;

namespace UnityEditor.ShaderFoundry
{
    [NativeHeader("Modules/ShaderFoundry/Public/FixedIntList.h")]
    internal struct FixedIntListInternal
    {
        FoundryHandle m_ListHandle;

        public FoundryHandle ListHandle => m_ListHandle;

        public FixedIntListInternal(FoundryHandle listHandle)
        {
            m_ListHandle = listHandle;
        }

        public extern static FixedIntListInternal Invalid();
        public extern bool IsValid();
        public extern uint GetSize(ShaderContainer container);
        public extern int GetElement(ShaderContainer container, uint elementIndex);
        public extern void SetElement(ShaderContainer container, uint elementIndex, int id);

        public static FixedIntListInternal Empty => Invalid();

        public static FoundryHandle Build(ShaderContainer container, List<int> values)
        {
            if ((values == null) || (values.Count <= 0))
                return FoundryHandle.Invalid();
            var listHandle = container.AddIntBlob((uint)values.Count);
            for (var i = 0; i < values.Count; ++i)
                container.SetIntBlobElement(listHandle, (uint)i, values[i]);
            return listHandle;
        }

        public static FoundryHandle Build<T>(ShaderContainer container, List<T> items, Func<T, int> indexFunc)
        {
            if ((items == null) || (items.Count <= 0))
                return FoundryHandle.Invalid();
            var listHandle = container.AddIntBlob((uint)items.Count);
            for (var i = 0; i < items.Count; ++i)
                container.SetIntBlobElement(listHandle, (uint)i, indexFunc(items[i]));
            return listHandle;
        }

        public IEnumerable<T> Select<T>(ShaderContainer container, Func<int, T> func)
        {
            var size = GetSize(container);
            for (uint i = 0; i < size; i++)
            {
                var id = GetElement(container, i);
                yield return func(id);
            }
        }
    }
}
