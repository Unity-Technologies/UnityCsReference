// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEditor;

namespace Unity.Experimental.EditorMode
{
    /// <summary>
    /// Helper class that allows an Editor Mode to configure the overrides when it is switched on.
    /// </summary>
    /// <remarks>
    /// A context is only valid when loading an editor mode.
    /// </remarks>
    internal sealed class EditorModeContext
    {
        private interface IContextHelper
        {
            void RegisterOverride<TOverride, TWindow>()
                where TOverride : EditorWindowOverride<TWindow>, new()
                where TWindow : EditorWindow;

            void RegisterOverride<TOverride>(Type editorWindowType)
                where TOverride : EditorWindowOverride<EditorWindow>, new();

            void RegisterAsPassthrough(Type editorWindowType);

            void RegisterAsUnsupported(Type editorWindowType);
        }

        private class ContextHelper<TMode> : IContextHelper
            where TMode : EditorMode
        {
            private readonly TMode m_Mode;

            internal ContextHelper(TMode mode)
            {
                m_Mode = mode;
            }

            public void RegisterOverride<TOverride, TWindow>()
                where TOverride : EditorWindowOverride<TWindow>, new()
                where TWindow : EditorWindow
            {
                EditorModes.RegisterOverride<TMode, TOverride, TWindow>(m_Mode);
            }

            public void RegisterOverride<TOverride>(Type editorWindowType)
                where TOverride : EditorWindowOverride<EditorWindow>, new()
            {
                EditorModes.RegisterOverride<TMode, TOverride>(m_Mode, editorWindowType);
            }

            public void RegisterAsPassthrough(Type editorWindowType)
            {
                EditorModes.RegisterAsPassthrough<TMode>(m_Mode, editorWindowType);
            }

            public void RegisterAsUnsupported(Type editorWindowType)
            {
                EditorModes.RegisterAsUnsupported<TMode>(m_Mode, editorWindowType);
            }
        }

        private readonly IContextHelper m_Helper;

        internal static EditorModeContext CreateContext<TMode>(TMode mode)
            where TMode : EditorMode
        {
            return new EditorModeContext(new ContextHelper<TMode>(mode));
        }

        private EditorModeContext(IContextHelper helper)
        {
            m_Helper = helper;
        }

        /// <summary>
        /// Registers an override type for a typed editor window.
        /// </summary>
        /// <typeparam name="TOverride"> The type of the <see cref="IEditorWindowOverride"/> to register. </typeparam>
        /// <typeparam name="TWindow"> The type of the <see cref="EditorWindow"/> to override. </typeparam>
        public void RegisterOverride<TOverride, TWindow>()
            where TOverride : EditorWindowOverride<TWindow>, new()
            where TWindow : EditorWindow
        {
            m_Helper.RegisterOverride<TOverride, TWindow>();
        }

        /// <summary>
        /// Registers an override type for a general editor window.
        /// </summary>
        /// <param name="editorWindowType"> The type of the <see cref="EedditorWindow"/> to override. </param>
        /// <typeparam name="TOverride"> The type of the <see cref="IEditorWindowOverride"/> to register. </typeparam>
        public void RegisterOverride<TOverride>(Type editorWindowType)
            where TOverride : EditorWindowOverride<EditorWindow>, new()
        {
            m_Helper.RegisterOverride<TOverride>(editorWindowType);
        }

        /// <summary>
        /// Registers an editor window as unsupported.
        /// </summary>
        /// <param name="editorWindowType"> The type of the <see cref="EditorWindow"/> to set as unsupported. </param>
        public void RegisterAsUnsupported(Type editorWindowType)
        {
            m_Helper.RegisterAsUnsupported(editorWindowType);
        }

        /// <summary>
        /// Registers an editor window as a passthrough override.
        /// </summary>
        /// <param name="editorWindowType"> The type of the <see cref="EditorWindow"/> to set as a passthrough override. </param>
        public void RegisterAsPassthrough(Type editorWindowType)
        {
            m_Helper.RegisterAsPassthrough(editorWindowType);
        }
    }
}
