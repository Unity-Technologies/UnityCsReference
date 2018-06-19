// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;
using UnityEngine;

namespace UnityEditor
{
    // This base class can be used when the type of an imported object derives from ScriptableObject
    // Since SOs have the special need of being reloaded after a re-import
    // we need to check for validity of the target in a few places
    abstract internal class ScriptableObjectAssetEditor : Editor
    {
        // hack to avoid null references when a scriptedImporter runs and replaces the current selection
        internal override string targetTitle
        {
            get
            {
                if (!target)
                {
                    serializedObject.Update();
                    InternalSetTargets(serializedObject.targetObjects);
                }
                return base.targetTitle;
            }
        }

        public override GUIContent GetPreviewTitle()
        {
            return GUIContent.Temp(targetTitle);
        }
    }
}
