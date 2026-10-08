// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.UIElements;

namespace Unity.Multiplayer.PlayMode.Editor;

class FieldIcon<T> : Image
{
    const string k_FieldIconClass = "unity-instance-field__icon";

    public FieldIcon(BaseField<T> field, Icons.ImageName iconName)
    {
        image = Icons.GetImage(iconName);
        AddToClassList(k_FieldIconClass);

        // Sized here rather than left to the class alone: the rule lives in ScenarioConfigEditor.uss,
        // which only the Play Mode Scenarios window loads, so in the status views the icon would take
        // its size from the raw texture and stretch the row it sits in.
        style.width = 16;
        style.height = 16;
        style.flexShrink = 0;
        style.alignSelf = Align.Center;

        // Goes before the field's value. A field built without a label keeps no label element in its
        // hierarchy, so a fixed index would put the icon after the value on those fields instead.
        field.Insert(field.IndexOf(field.labelElement) + 1, this);
    }
}
