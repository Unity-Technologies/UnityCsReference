// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

﻿using UnityEditor.UIElements.ProjectSettings;

namespace UnityEditor.Inspector.GraphicsSettingsInspectors
{
    internal abstract class GraphicsSettingsElement : ProjectSettingsElementWithSO
    {
        //We rely on SupportedOn attribute for the cases when we need to show element for SRP.
        //Here is a way to specify when we want to have element visible for BuiltinOnly.
        //Important notice: we check first for SupportedOn first, then for this backup field.
        public virtual bool BuiltinOnly => false;
    }
}
