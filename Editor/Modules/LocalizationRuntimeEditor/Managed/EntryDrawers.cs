// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Unity.Localization.Editor;

[ResourceEntryDrawer(typeof(StringEntry))]
class StringEntryDrawer : ResourceEntryDrawer
{
    public override VisualElement CreateCell(IResourceEntry entry, ResourceEntryContext context)
    {
        var field = new TextField { multiline = true };
        field.AddToClassList(LocClasses.LocValueField);
        var valueProp = context.EntryProperty?.FindPropertyRelative("m_Value");
        if (valueProp != null)
        {
            field.BindProperty(valueProp);
        }
        else
        {
            var stringEntry = (StringEntry)entry;
            field.SetValueWithoutNotify(stringEntry.Value);
            // One undo step per editing session, capturing the pre-edit state.
            field.RegisterCallback<FocusInEvent>(_ =>
            {
                if (context.Table != null)
                    Undo.RegisterCompleteObjectUndo(context.Table, "Edit Entry Value");
            });
            field.RegisterValueChangedCallback(evt => { stringEntry.Value = evt.newValue; context.MarkDirty(); });
        }
        return field;
    }
}
