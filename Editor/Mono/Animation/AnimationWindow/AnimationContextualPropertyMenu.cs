// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace UnityEditorInternal
{
    internal class AnimationPropertyContextualMenu
    {
        public static AnimationPropertyContextualMenu Instance = new AnimationPropertyContextualMenu();

        private List<IAnimationContextualResponder> m_Responders = new List<IAnimationContextualResponder>();

        private static GUIContent addKeyContent = EditorGUIUtility.TextContent("Add Key");
        private static GUIContent updateKeyContent = EditorGUIUtility.TextContent("Update Key");
        private static GUIContent removeKeyContent = EditorGUIUtility.TextContent("Remove Key");
        private static GUIContent removeCurveContent = EditorGUIUtility.TextContent("Remove All Keys");
        private static GUIContent goToPreviousKeyContent = EditorGUIUtility.TextContent("Go to Previous Key");
        private static GUIContent goToNextKeyContent = EditorGUIUtility.TextContent("Go to Next Key");
        private static GUIContent addCandidatesContent = EditorGUIUtility.TextContent("Key All Modified");
        private static GUIContent addAnimatedContent = EditorGUIUtility.TextContent("Key All Animated");

        public AnimationPropertyContextualMenu()
        {
            EditorApplication.contextualPropertyMenu += OnPropertyContextMenu;
        }

        public void AddResponder(IAnimationContextualResponder responder)
        {
            m_Responders.Add(responder);
        }

        public void RemoveResponder(IAnimationContextualResponder responder)
        {
            m_Responders.Remove(responder);
        }

        void OnPropertyContextMenu(GenericMenu menu, SerializedProperty property)
        {
            if (m_Responders.Count == 0)
                return;

            bool isPropertyAnimatable = m_Responders.Exists(responder => responder.IsAnimatable(property));
            if (isPropertyAnimatable)
            {
                bool isEditable = m_Responders.Exists(responder => responder.IsEditable(property));
                bool hasKey = isEditable && m_Responders.Exists(responder => responder.KeyExists(property));
                bool hasCandidate = isEditable && m_Responders.Exists(responder => responder.CandidateExists(property));
                bool hasCurve = isEditable && (hasKey || m_Responders.Exists(responder => responder.CurveExists(property)));

                bool hasAnyCandidate = isEditable && m_Responders.Exists(responder => responder.HasAnyCandidates());
                bool hasAnyCurve = isEditable && m_Responders.Exists(responder => responder.HasAnyCurves());

                // Important to pass a copy, the original can get Next called on it
                // before the callback invoked
                var propertyCopy = property.Copy();

                if (isEditable)
                {
                    menu.AddItem(((hasKey && hasCandidate) ? updateKeyContent : addKeyContent), false, () =>
                        {
                            m_Responders.ForEach(responder => responder.AddKey(propertyCopy));
                        });
                }
                else
                {
                    menu.AddDisabledItem(addKeyContent);
                }

                if (hasKey)
                {
                    menu.AddItem(removeKeyContent, false, () =>
                        {
                            m_Responders.ForEach(responder => responder.RemoveKey(propertyCopy));
                        });
                }
                else
                {
                    menu.AddDisabledItem(removeKeyContent);
                }

                if (hasCurve)
                {
                    menu.AddItem(removeCurveContent, false, () =>
                        {
                            m_Responders.ForEach(responder => responder.RemoveCurve(propertyCopy));
                        });
                }
                else
                {
                    menu.AddDisabledItem(removeCurveContent);
                }

                menu.AddSeparator(string.Empty);
                if (hasAnyCandidate)
                {
                    menu.AddItem(addCandidatesContent, false, () =>
                        {
                            m_Responders.ForEach(responder => responder.AddCandidateKeys());
                        });
                }
                else
                {
                    menu.AddDisabledItem(addCandidatesContent);
                }

                if (hasAnyCurve)
                {
                    menu.AddItem(addAnimatedContent, false, () =>
                        {
                            m_Responders.ForEach(responder => responder.AddAnimatedKeys());
                        });
                }
                else
                {
                    menu.AddDisabledItem(addAnimatedContent);
                }

                menu.AddSeparator(string.Empty);
                if (hasCurve)
                {
                    menu.AddItem(goToPreviousKeyContent, false, () =>
                        {
                            m_Responders.ForEach(responder => responder.GoToPreviousKeyframe(propertyCopy));
                        });
                    menu.AddItem(goToNextKeyContent, false, () =>
                        {
                            m_Responders.ForEach(responder => responder.GoToNextKeyframe(propertyCopy));
                        });
                }
                else
                {
                    menu.AddDisabledItem(goToPreviousKeyContent);
                    menu.AddDisabledItem(goToNextKeyContent);
                }
            }
        }
    }
}
