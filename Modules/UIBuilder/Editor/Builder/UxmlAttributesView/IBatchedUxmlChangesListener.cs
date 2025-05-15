// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;
namespace Unity.UI.Builder;

internal interface IBatchedUxmlChangesListener
{
    void DeserializeElement();
    void NotifyAllChangesProcessed();
    void AttributeValueChanged(VisualElement field, string value, UxmlAsset uxmlAsset = null);
    void UxmlObjectChanged(VisualElement element);
    BuilderUxmlAttributesView.SynchronizePathResult SynchronizePath(string propertyPath, bool changeUxmlAssets);
    void ToggleUxmlChangeFlagForView(bool enabled);

}
