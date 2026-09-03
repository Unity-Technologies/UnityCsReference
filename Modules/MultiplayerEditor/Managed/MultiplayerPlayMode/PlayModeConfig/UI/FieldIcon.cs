// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#pragma warning disable UAL0015,UAL0018,UAL0019,UAL0020,UAL0021 // AutoStaticsCleanup usage analysis: HeadlessRuntime not yet converted
using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor;

class FieldIcon<T> : Image
{
    const string k_FieldIconClass = "unity-instance-field__icon";

    public FieldIcon(BaseField<T> field, Icons.ImageName iconName)
    {
        image = Icons.GetImage(iconName);
        AddToClassList(k_FieldIconClass);
        field.Insert(1, this);
    }
}
#pragma warning restore UAL0015,UAL0018,UAL0019,UAL0020,UAL0021
