// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Scripting;

namespace UnityEngine
{
    [RequiredByNativeCode]
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class DisallowMultipleComponent : Attribute {}

    // The RequireComponent attribute lets automatically add required component as a dependency.
    [RequiredByNativeCode]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class RequireComponent : Attribute
    {
        //*undocumented*
        public Type m_Type0;
        //*undocumented*
        public Type m_Type1;
        //*undocumented*
        public Type m_Type2;

        // Require a single component
        public RequireComponent(Type requiredComponent) { m_Type0 = requiredComponent; }
        // Require a two components
        public RequireComponent(Type requiredComponent, Type requiredComponent2) { m_Type0 = requiredComponent; m_Type1 = requiredComponent2;  }
        // Require three components
        public RequireComponent(Type requiredComponent, Type requiredComponent2, Type requiredComponent3) { m_Type0 = requiredComponent; m_Type1 = requiredComponent2; m_Type2 = requiredComponent3; }
    }


    // The AddComponentMenu attribute allows you to place a script anywhere in the "Component" menu, instead of just the "Component->Scripts" menu.
    public sealed class AddComponentMenu : Attribute
    {
        private string m_AddComponentMenu;
        private int m_Ordering;

        // The script will be placed in the component menu according to /menuName/. /menuName/ is the path to the component
        public AddComponentMenu(string menuName) { m_AddComponentMenu = menuName; m_Ordering = 0; }
        // same as above, but also specify a custom Ordering.
        public AddComponentMenu(string menuName, int order) { m_AddComponentMenu = menuName; m_Ordering = order; }

        //* undocumented
        public string componentMenu { get {return m_AddComponentMenu; } }

        //* undocumented
        public int componentOrder { get {return m_Ordering; } }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class CreateAssetMenuAttribute : Attribute
    {
        public string menuName { get; set; }
        public string fileName { get; set; }
        public int order { get; set; }
    }

    // The ContextMenu attribute allows you to add commands to the context menu
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    [RequiredByNativeCode]
    public sealed class ContextMenu : Attribute
    {
        // Creates a context menu item that invokes the non-static method when selected
        public ContextMenu(string itemName) : this(itemName, false) {}
        public ContextMenu(string itemName, bool isValidateFunction) : this(itemName, isValidateFunction, 1000000) {}
        public ContextMenu(string itemName, bool isValidateFunction, int priority)
        {
            menuItem = itemName;
            validate = isValidateFunction;
            this.priority = priority;
        }

        //*undocumented*
        public readonly string menuItem;
        //*undocumented*
        public readonly bool validate;
        //*undocumented*
        public readonly int priority;
    }

    // Makes a script execute in edit mode.
    [UsedByNativeCode]
    public sealed class ExecuteInEditMode : Attribute {}

    // Makes a script execute always: when in edit mode, play mode (also for scripts prefab isolation mode in play mode)
    [UsedByNativeCode]
    public sealed class ExecuteAlways : Attribute {}

    // Makes a variable not show up in the inspector but be serialized.
    [UsedByNativeCode]
    public sealed class HideInInspector : Attribute {}

    // Sets a custom help URL for a script.
    [UsedByNativeCode]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class HelpURLAttribute : Attribute
    {
        public HelpURLAttribute(string url) { m_Url = url; m_DispatchingFieldName = ""; m_Dispatcher = false; }

        //internal ability to retarget the url on an inside field value's type's HelpURL
        internal HelpURLAttribute(string defaultURL, string dispatchingFieldName) { m_Url = defaultURL; m_DispatchingFieldName = dispatchingFieldName; m_Dispatcher = !String.IsNullOrEmpty(dispatchingFieldName); }

        public string URL => m_Url;

        internal readonly string m_Url;
        internal readonly bool m_Dispatcher;
        internal readonly string m_DispatchingFieldName;
    }

    [UsedByNativeCode]
    [AttributeUsage(AttributeTargets.Class)]
    public class DefaultExecutionOrder : Attribute
    {
        public DefaultExecutionOrder(int order)
        {
            m_Order = order;
        }

        public int order
        {
            get { return m_Order; }
        }

        int m_Order;
    }

    [AttributeUsage(AttributeTargets.Assembly)]
    [RequiredByNativeCode]
    public class AssemblyIsEditorAssembly : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    [UsedByNativeCode]
    public class ExcludeFromPresetAttribute : Attribute {}
}
