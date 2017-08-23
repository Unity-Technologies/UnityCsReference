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

        IAnimationContextualResponder m_Responder;

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
            MaterialEditor.contextualPropertyMenu += OnPropertyContextMenu;
        }

        public void SetResponder(IAnimationContextualResponder responder)
        {
            m_Responder = responder;
        }

        public bool IsResponder(IAnimationContextualResponder responder)
        {
            return responder == m_Responder;
        }

        void OnPropertyContextMenu(GenericMenu menu, SerializedProperty property)
        {
            if (m_Responder == null)
                return;

            PropertyModification[] modifications = AnimationWindowUtility.SerializedPropertyToPropertyModifications(property);

            bool isPropertyAnimatable = m_Responder.IsAnimatable(modifications);
            if (isPropertyAnimatable)
            {
                var targetObject = property.serializedObject.targetObject;
                if (m_Responder.IsEditable(targetObject))
                    OnPropertyContextMenu(menu, modifications);
                else
                    OnDisabledPropertyContextMenu(menu);
            }
        }

        void OnPropertyContextMenu(GenericMenu menu, MaterialProperty property, Renderer[] renderers)
        {
            if (m_Responder == null)
                return;

            if (property.targets == null || property.targets.Length == 0)
                return;

            if (renderers == null || renderers.Length == 0)
                return;

            var modifications = new List<PropertyModification>();
            foreach (Renderer renderer in renderers)
            {
                modifications.AddRange(MaterialAnimationUtility.MaterialPropertyToPropertyModifications(property, renderer));
            }

            if (m_Responder.IsEditable(renderers[0]))
                OnPropertyContextMenu(menu, modifications.ToArray());
            else
                OnDisabledPropertyContextMenu(menu);
        }

        void OnPropertyContextMenu(GenericMenu menu, PropertyModification[] modifications)
        {
            bool hasKey = m_Responder.KeyExists(modifications);
            bool hasCandidate = m_Responder.CandidateExists(modifications);
            bool hasCurve = (hasKey || m_Responder.CurveExists(modifications));

            bool hasAnyCandidate = m_Responder.HasAnyCandidates();
            bool hasAnyCurve = m_Responder.HasAnyCurves();

            menu.AddItem(((hasKey && hasCandidate) ? updateKeyContent : addKeyContent), false, () =>
                {
                    m_Responder.AddKey(modifications);
                });

            if (hasKey)
            {
                menu.AddItem(removeKeyContent, false, () =>
                    {
                        m_Responder.RemoveKey(modifications);
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
                        m_Responder.RemoveCurve(modifications);
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
                        m_Responder.AddCandidateKeys();
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
                        m_Responder.AddAnimatedKeys();
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
                        m_Responder.GoToPreviousKeyframe(modifications);
                    });
                menu.AddItem(goToNextKeyContent, false, () =>
                    {
                        m_Responder.GoToNextKeyframe(modifications);
                    });
            }
            else
            {
                menu.AddDisabledItem(goToPreviousKeyContent);
                menu.AddDisabledItem(goToNextKeyContent);
            }
        }

        void OnDisabledPropertyContextMenu(GenericMenu menu)
        {
            menu.AddDisabledItem(addKeyContent);
            menu.AddDisabledItem(removeKeyContent);
            menu.AddDisabledItem(removeCurveContent);
            menu.AddSeparator(string.Empty);
            menu.AddDisabledItem(addCandidatesContent);
            menu.AddDisabledItem(addAnimatedContent);
            menu.AddSeparator(string.Empty);
            menu.AddDisabledItem(goToPreviousKeyContent);
            menu.AddDisabledItem(goToNextKeyContent);
        }
    }
}
