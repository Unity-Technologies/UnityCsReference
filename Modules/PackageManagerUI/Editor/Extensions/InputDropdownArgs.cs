// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace UnityEditor.PackageManager.UI
{
    internal struct InputDropdownArgs
    {
        public string title;
        public Background? icon;
        internal string iconUssClass;
        public string placeholderText;
        public string submitButtonText;
        public string defaultValue;
        public Action<string> onInputSubmitted;
    }
}
