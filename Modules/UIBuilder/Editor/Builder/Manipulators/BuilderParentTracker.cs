// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

namespace Unity.UI.Builder
{
    class BuilderParentTracker : BuilderTracker
    {
        static readonly string s_UssClassName = "unity-builder-parent-tracker";

        [Serializable]
        public new class UxmlSerializedData : BuilderTracker.UxmlSerializedData
        {
            public override object CreateInstance() => new BuilderParentTracker();
        }

        public BuilderParentTracker()
        {
            AddToClassList(s_UssClassName);
        }
    }
}
