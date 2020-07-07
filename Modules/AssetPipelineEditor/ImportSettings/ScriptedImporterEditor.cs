// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.AssetImporters
{
    [CustomEditor(typeof(ScriptedImporter), true)]
    [CanEditMultipleObjects]
    [MovedFrom("UnityEditor.Experimental.AssetImporters")]
    public class ScriptedImporterEditor : AssetImporterEditor
    {
        internal override string targetTitle
        {
            get { return base.targetTitle + " (" + ObjectNames.NicifyVariableName(target.GetType().Name) + ")"; }
        }
    }
}
