// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting;

namespace UnityEditor
{
    // The WindowAction attribute allows you to add global actions to the windows (items in the generic menu or extra buttons).
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class WindowActionAttribute : Attribute
    {
        [RequiredSignature]
        private static WindowAction signature()
        {
            return null;
        }
    }


    internal class WindowAction
    {
        public delegate void ExecuteHandler(EditorWindow window, WindowAction action);
        public delegate bool ValidateHandler(EditorWindow window, WindowAction action);
        public delegate bool DrawHandler(EditorWindow window, WindowAction action, Rect rect);

        public string id;
        public ExecuteHandler executeHandler;
        public object userData;
        public ValidateHandler validateHandler;
        public string menuPath;
        public float? width;
        public Texture2D icon;
        public DrawHandler drawHandler; // i.e. if used in the tab bar
        public int priority;


        private WindowAction(string id, ExecuteHandler executeHandler, string menuPath)
        {
            this.id = id;
            this.executeHandler = executeHandler;
            this.menuPath = menuPath;
        }

        public static WindowAction CreateWindowMenuItem(string id, ExecuteHandler executeHandler, string menuPath)
        {
            return new WindowAction(id, executeHandler, menuPath);
        }

        public static WindowAction CreateWindowActionButton(string id, ExecuteHandler executeHandler, string menuPath, float width, Texture2D icon)
        {
            return new WindowAction(id, executeHandler, menuPath)
            {
                width = width,
                icon = icon
            };
        }

        public static WindowAction CreateWindowActionButton(string id, ExecuteHandler executeHandler, string menuPath, float width, DrawHandler drawHandler)
        {
            return new WindowAction(id, executeHandler, menuPath)
            {
                width = width,
                drawHandler = drawHandler
            };
        }
    }
}
