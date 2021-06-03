// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Toolbars
{
    sealed class EditorToolbarManager : ScriptableSingleton<EditorToolbarManager>
    {
        readonly Dictionary<string, ToolbarElementDefinition> m_IdToDefinition;

        EditorToolbarManager()
        {
            var types = TypeCache.GetTypesWithAttribute<EditorToolbarElementAttribute>();
            m_IdToDefinition = new Dictionary<string, ToolbarElementDefinition>(types.Count);

            for (int i = 0; i < types.Count; ++i)
            {
                var type = types[i];
                if (typeof(VisualElement).IsAssignableFrom(type))
                {
                    var attr = (EditorToolbarElementAttribute)type.GetCustomAttributes(
                        typeof(EditorToolbarElementAttribute), false)[0];
                    string id = attr.id;
                    if (Exists(id))
                    {
                        Debug.LogWarning(
                            $"Editor Toolbar Element with id {id} already exists. The element will be skipped.");
                        continue;
                    }

                    m_IdToDefinition.Add(attr.id, new ToolbarElementDefinition(id, type, attr.targetWindows));
                }
            }
        }

        public bool TryCreateElementFromId(EditorWindow context, string id, out VisualElement element)
        {
            if (m_IdToDefinition.TryGetValue(id, out ToolbarElementDefinition definition))
            {
                bool inProperContext = false;
                if (definition.targetContexts == null || definition.targetContexts.Length == 0)
                {
                    inProperContext = true;
                }
                else
                {
                    foreach (var c in definition.targetContexts)
                    {
                        //The main toolbar isn't a window but we support DefaultMainToolbar as a valid context for adding elements
                        if (context == null && (c == null || typeof(MainToolbarVisual).IsAssignableFrom(c))
                            || c.IsInstanceOfType(context))
                        {
                            inProperContext = true;
                        }
                    }
                }

                if (inProperContext)
                {
                    try
                    {
                        const BindingFlags flags =  BindingFlags.Instance |
                            BindingFlags.Public |
                            BindingFlags.NonPublic |
                            BindingFlags.CreateInstance;

                        element = (VisualElement)Activator.CreateInstance(definition.elementType, flags, (Binder)null,
                            null, (CultureInfo)null, (object[])null);
                        element.AddToClassList(EditorToolbar.elementClassName);
                        return true;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed creating toolbar element from ID \"{id}\".\n{e}");
                    }
                }
            }

            element = null;
            return false;
        }

        public bool Exists(string id)
        {
            return m_IdToDefinition.ContainsKey(id);
        }
    }
}
