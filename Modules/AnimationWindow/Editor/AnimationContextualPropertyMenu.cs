// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;
using UnityEditor;
using Object = UnityEngine.Object;

namespace UnityEditorInternal
{
    [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
    class AnimationPropertyContextualMenu
    {
        // UI-framework-agnostic surface used by AnimationPropertyContextualMenu 
        // Concrete adapters wrap the actual menu type (GenericMenu or DropdownMenu) 
        interface IMenuBuilder
        {
            // callback may be null when enabled is false; adapters must render a disabled entry.
            void AppendItem(GUIContent content, Action callback, bool enabled);
            void AppendSeparator();
        }

        public static AnimationPropertyContextualMenu Instance = new AnimationPropertyContextualMenu();

        IAnimationContextualResponder m_Responder;

        private static GUIContent addKeyContent = EditorGUIUtility.TrTextContent("Add Key");
        private static GUIContent updateKeyContent = EditorGUIUtility.TrTextContent("Update Key");
        private static GUIContent removeKeyContent = EditorGUIUtility.TrTextContent("Remove Key");
        private static GUIContent removeCurveContent = EditorGUIUtility.TrTextContent("Remove All Keys");
        private static GUIContent goToPreviousKeyContent = EditorGUIUtility.TrTextContent("Go to Previous Key");
        private static GUIContent goToNextKeyContent = EditorGUIUtility.TrTextContent("Go to Next Key");
        private static GUIContent addCandidatesContent = EditorGUIUtility.TrTextContent("Key All Modified");
        private static GUIContent addAnimatedContent = EditorGUIUtility.TrTextContent("Key All Animated");

        // for tests that match emitted items without re-translating the literals.
        internal static string AddKeyText => addKeyContent.text;
        internal static string UpdateKeyText => updateKeyContent.text;
        internal static string RemoveKeyText => removeKeyContent.text;
        internal static string RemoveCurveText => removeCurveContent.text;
        internal static string GoToPreviousKeyText => goToPreviousKeyContent.text;
        internal static string GoToNextKeyText => goToNextKeyContent.text;
        internal static string AddCandidatesText => addCandidatesContent.text;
        internal static string AddAnimatedText => addAnimatedContent.text;

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

            var modifications = AnimationWindowUtility.SerializedPropertyToPropertyModifications(property);
            PopulateMenu(new GenericMenuBuilder(menu), modifications, property.serializedObject.targetObject);
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
                modifications.AddRange(MaterialAnimationUtility.MaterialPropertyToPropertyModifications(property, renderer));

            PopulateMenu(new GenericMenuBuilder(menu), modifications.ToArray(), renderers[0]);
        }

        // Bridges DropdownMenu calls into the shared menu layout.
        [VisibleToOtherModules("UnityEditor.UIToolkitAuthoringModule")]
        internal void PopulateDropdownContextMenu(DropdownMenu menu, PropertyModification[] modifications, Object targetObject)
        {
            if (menu == null)
                return;

            PopulateMenu(new DropdownMenuBuilder(menu), modifications, targetObject);
        }

        // Shared menu layout. The caller supplies an IMenuBuilder adapter for whichever
        // menu type it owns; the enable/disable rules and item ordering below are the
        // single source of truth for both IMGUI and UI Toolkit emissions.
        void PopulateMenu(IMenuBuilder builder, PropertyModification[] modifications, Object targetObject)
        {
            if (m_Responder == null || modifications == null || modifications.Length == 0)
                return;

            if (!m_Responder.IsAnimatable(modifications))
                return;

            if (m_Responder.IsEditable(targetObject))
                AppendEnabled(builder, modifications);
            else
                AppendDisabled(builder);
        }

        void AppendEnabled(IMenuBuilder builder, PropertyModification[] modifications)
        {
            bool hasKey = m_Responder.KeyExists(modifications);
            bool hasCandidate = m_Responder.CandidateExists(modifications);
            bool hasCurve = hasKey || m_Responder.CurveExists(modifications);
            bool hasAnyCandidate = m_Responder.HasAnyCandidates();
            bool hasAnyCurve = m_Responder.HasAnyCurves();

            builder.AppendItem((hasKey && hasCandidate) ? updateKeyContent : addKeyContent,
                () => m_Responder.AddKey(modifications), true);
            builder.AppendItem(removeKeyContent,
                () => m_Responder.RemoveKey(modifications), hasKey);
            builder.AppendItem(removeCurveContent,
                () => m_Responder.RemoveCurve(modifications), hasCurve);
            builder.AppendSeparator();
            builder.AppendItem(addCandidatesContent,
                () => m_Responder.AddCandidateKeys(), hasAnyCandidate);
            builder.AppendItem(addAnimatedContent,
                () => m_Responder.AddAnimatedKeys(), hasAnyCurve);
            builder.AppendSeparator();
            builder.AppendItem(goToPreviousKeyContent,
                () => m_Responder.GoToPreviousKeyframe(modifications), hasCurve);
            builder.AppendItem(goToNextKeyContent,
                () => m_Responder.GoToNextKeyframe(modifications), hasCurve);
        }

        static void AppendDisabled(IMenuBuilder builder)
        {
            builder.AppendItem(addKeyContent, null, false);
            builder.AppendItem(removeKeyContent, null, false);
            builder.AppendItem(removeCurveContent, null, false);
            builder.AppendSeparator();
            builder.AppendItem(addCandidatesContent, null, false);
            builder.AppendItem(addAnimatedContent, null, false);
            builder.AppendSeparator();
            builder.AppendItem(goToPreviousKeyContent, null, false);
            builder.AppendItem(goToNextKeyContent, null, false);
        }

        // GenericMenu has no first-class "disabled item with callback" - falling back
        // to AddDisabledItem is the conventional way to grey out an entry in IMGUI.
        sealed class GenericMenuBuilder : IMenuBuilder
        {
            readonly GenericMenu m_Menu;
            public GenericMenuBuilder(GenericMenu menu) { m_Menu = menu; }

            public void AppendItem(GUIContent content, Action callback, bool enabled)
            {
                if (enabled && callback != null)
                    m_Menu.AddItem(content, false, () => callback());
                else
                    m_Menu.AddDisabledItem(content);
            }

            public void AppendSeparator() => m_Menu.AddSeparator(string.Empty);
        }

        // DropdownMenu takes a string label rather than GUIContent; the IMGUI-style
        // tooltip/image fields aren't surfaced today, but the path through .text keeps
        // the localized literal that EditorGUIUtility.TrTextContent produced.
        sealed class DropdownMenuBuilder : IMenuBuilder
        {
            readonly DropdownMenu m_Menu;
            public DropdownMenuBuilder(DropdownMenu menu) { m_Menu = menu; }

            public void AppendItem(GUIContent content, Action callback, bool enabled)
            {
                m_Menu.AppendAction(content.text,
                    callback != null ? _ => callback() : null,
                    enabled ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            }

            public void AppendSeparator() => m_Menu.AppendSeparator();
        }
    }
}
