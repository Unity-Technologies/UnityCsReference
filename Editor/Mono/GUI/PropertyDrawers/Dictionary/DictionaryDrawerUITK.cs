// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEditor
{

[CustomPropertyDrawer(typeof(Dictionary<,>))]
internal partial class DictionaryDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        var dictionaryView = new DictionaryView();

        // At this point the view has just been constructed and is not yet
        // parented, so dictionaryView.panel is null. Calling BindProperty here
        // would (a) set bindingPath, and (b) take the "element.panel == null"
        // branch in DefaultSerializedObjectBindingImplementation.BindProperty
        // and queue a deferred BindingRequest on VisualTreeBindingsUpdater.
        // The inspector's subsequent synchronous Bind(SerializedObject) walk
        // already dispatches SerializedPropertyBindEvent to any descendant
        // with a bindingPath, so the queued request just re-walks the tree
        // on the next panel update and fires the bind event a second time,
        // causing DictionaryView.RebuildFromProperty to run twice.
        dictionaryView.bindingPath = property.propertyPath;
        return dictionaryView;
    }
}

} // end of namespace
