// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

namespace UnityEditor
{
    internal static class EditorExtensionMethods
    {
        // Use this method when checking if user hit Space or Return in order to activate the main action
        // for a control, such as opening a popup menu or color picker.
        internal static bool MainActionKeyForControl(this UnityEngine.Event evt, int controlId)
        {
            if (EditorGUIUtility.keyboardControl != controlId)
                return false;

            bool anyModifiers = (evt.alt || evt.shift || evt.command || evt.control);

            // Block window maximize (on OSX ML, we need to show the menu as part of the KeyCode event, so we can't do the usual check)
            if (evt.type == EventType.KeyDown && evt.character == ' ' && !anyModifiers)
            {
                evt.Use();
                return false;
            }

            // Space or return is action key
            return evt.type == EventType.KeyDown &&
                (evt.keyCode == KeyCode.Space || evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter) &&
                !anyModifiers;
        }

        internal static bool IsArrayOrList(this Type listType)
        {
            if (listType.IsArray)
            {
                return true;
            }
            else if (listType.IsGenericType && listType.GetGenericTypeDefinition() == typeof(List<>))
            {
                return true;
            }
            return false;
        }

        internal static Type GetArrayOrListElementType(this Type listType)
        {
            if (listType.IsArray)
            {
                return listType.GetElementType();
            }
            else if (listType.IsGenericType && listType.GetGenericTypeDefinition() == typeof(List<>))
            {
                return listType.GetGenericArguments()[0];
            }
            return null;
        }

        internal static List<Enum> EnumGetNonObsoleteValues(this Type type)
        {
            // each enum value has the same position in both values and names arrays
            string[] names = Enum.GetNames(type);
            Enum[] values = Enum.GetValues(type).Cast<Enum>().ToArray();
            var result = new List<Enum>();
            for (int i = 0; i < names.Length; i++)
            {
                var info = type.GetMember(names[i]);
                var attrs = info[0].GetCustomAttributes(typeof(ObsoleteAttribute), false);
                var isObsolete = false;
                foreach (var attr in attrs)
                {
                    if (attr is ObsoleteAttribute)
                        isObsolete = true;
                }
                if (!isObsolete)
                    result.Add(values[i]);
            }
            return result;
        }
    }
}
