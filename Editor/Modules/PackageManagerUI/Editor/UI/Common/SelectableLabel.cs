// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI.Internal
{
    [UxmlElement]
    internal partial class SelectableLabel : Label
    {
        public SelectableLabel()
        {
            selection.isSelectable = true;
            focusable = true;
            displayTooltipWhenElided = true;
        }
    }
}
