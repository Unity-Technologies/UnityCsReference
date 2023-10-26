// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;

namespace UnityEditor.IMGUI.Controls
{
    internal class CallbackDataSource : AdvancedDropdownDataSource
    {
        Func<AdvancedDropdownItem> m_BuildCallback;

        internal CallbackDataSource(Func<AdvancedDropdownItem> buildCallback)
        {
            m_BuildCallback = buildCallback;
        }

        protected override AdvancedDropdownItem FetchData()
        {
            return m_BuildCallback();
        }
    }
}
