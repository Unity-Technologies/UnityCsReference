// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.UIElements;
using Unity.UIToolkit.Editor;

namespace Unity.UI.Builder
{
    class BuilderInspectorVariablesListItem : VariablesListItem
    {
        protected override BaseField<Object> CreateAssetField()
        {
            return new AssetReferenceStyleField() { name = k_AssetFieldName }.WithClassList(k_HiddenFieldClassName);
        }
    }
}
