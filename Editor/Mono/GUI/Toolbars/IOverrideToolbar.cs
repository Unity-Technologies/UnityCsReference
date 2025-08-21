// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;

namespace UnityEditor.Toolbars
{
    public enum OverridableToolbar
    {
        ToolSettings,
        ViewOptions
    }
    
    public interface IOverrideToolbar
    {
        public void PopulateToolbar(OverridableToolbar toolbarType, List<string> elements);
    }
}
