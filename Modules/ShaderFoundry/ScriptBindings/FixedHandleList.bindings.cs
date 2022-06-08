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
    [NativeHeader("Modules/ShaderFoundry/Public/FixedHandleList.h")]
    internal struct FixedHandleListInternal
    {
        FoundryHandle m_ListHandle;

        public FoundryHandle ListHandle => m_ListHandle;

        public FixedHandleListInternal(FoundryHandle listHandle)
        {
            m_ListHandle = listHandle;
        }

        public extern static FixedHandleListInternal Invalid();
        public extern bool IsValid();
        public extern uint GetSize(ShaderContainer container);
        public extern FoundryHandle GetElement(ShaderContainer container, uint elementIndex);
        public extern void SetElement(ShaderContainer container, uint elementIndex, FoundryHandle handle);

        public static FixedHandleListInternal Empty => Invalid();

        public static FoundryHandle Build(ShaderContainer container, List<FoundryHandle> values)
        {
            if ((values == null) || (values.Count <= 0))
                return FoundryHandle.Invalid();
            var listHandle = container.AddHandleBlob((uint)values.Count);
            for (var i = 0; i < values.Count; ++i)
                container.SetHandleBlobElement(listHandle, (uint)i, values[i]);
            return listHandle;
        }

        public static FoundryHandle Build<T>(ShaderContainer container, List<T> items, Func<T, FoundryHandle> indexFunc)
        {
            if ((items == null) || (items.Count <= 0))
                return FoundryHandle.Invalid();
            var listHandle = container.AddHandleBlob((uint)items.Count);
            for (var i = 0; i < items.Count; ++i)
                container.SetHandleBlobElement(listHandle, (uint)i, indexFunc(items[i]));
            return listHandle;
        }

        public IEnumerable<T> Select<T>(ShaderContainer container, Func<FoundryHandle, T> func)
        {
            var size = GetSize(container);
            for (uint i = 0; i < size; i++)
            {
                var handle = GetElement(container, i);
                yield return func(handle);
            }
        }
    }

    internal static class FixedHandleListUtilities
    {
        internal static IEnumerable<T> AsListEnumerable<T>(this FoundryHandle listHandle, ShaderContainer container, Func<ShaderContainer, FoundryHandle, T> accessor)
        {
            if ((container != null) && listHandle.IsValid)
            {
                var list = new FixedHandleListInternal(listHandle);
                var size = list.GetSize(container);
                for (uint i = 0; i < size; i++)
                {
                    var handle = list.GetElement(container, i);
                    yield return accessor(container, handle);
                }
            }
        }
    }
}
