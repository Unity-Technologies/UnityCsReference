// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

internal interface IStyleRuleSelectionHandler
{
    EntityId AcquireInstanceId(StyleRule rule);
    void ReleaseInstanceId(StyleRule rule);

    void Remap(List<StyleRuleRemap> candidates);
    public void Clear();
}

