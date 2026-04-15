// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.UIElements;

namespace Unity.UIToolkit.Editor;

[UxmlElement(visibility = LibraryVisibility.Hidden)]
sealed partial class CanvasSettingsHeader : UISelectionObjectHeader
{
    [Serializable]
    public new class UxmlSerializedData : UISelectionObjectHeader.UxmlSerializedData
    {
        public new static void Register()
            => UxmlDescriptionCache.RegisterType(typeof(UxmlSerializedData), [], true);

        public override object CreateInstance() =>  new CanvasSettingsHeader();
    }

    protected override VisualTreeAsset IdentifierDetails => null;
}
