// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEngine.Bindings;

namespace Unity.UIToolkit.Editor;

[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
internal enum SubDocumentOptions
{
    None,
    InContext,
    Isolation,
}

[System.Serializable]
[VisibleToOtherModules("UnityEditor.UIBuilderModule")]
internal class LoadUIDocumentCommand
{
    public const string CommandId = "UIToolkit__LoadDocument__CommandId";

    public List<VisualTreeAsset> subDocuments;
    public List<TemplateAsset> contextInstances;
    public SubDocumentOptions subDocumentOptions;
    public int selectedId = -1;
}
