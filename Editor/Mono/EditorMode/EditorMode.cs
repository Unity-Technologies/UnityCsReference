// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Experimental.EditorMode
{
    /// <summary>
    /// An Editor Mode is a mechanism allowing to disable and override normal behaviour of editor window in order to
    /// better reflect the current context.
    /// </summary>
    [UnityEngine.Internal.ExcludeFromDocs]
    internal abstract class EditorMode
    {
        /// <summary>
        /// Indicates how windows should be treated by default when they are not overridden.
        /// </summary>
        internal virtual EditorOverrideMode OverrideMode => EditorOverrideMode.PassthroughByDefault;

        /// <summary>
        /// Custom name to use for the mode.
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        /// Method called by the <see cref="EditorModes"/> class just before the mode is loaded. Use this method to set
        /// the override mode and to register specific editor window overrides.
        /// </summary>
        public virtual void OnEnterMode(EditorModeContext context)
        {
        }

        /// <summary>
        /// Method called by the <see cref="EditorModes"/> class just before the mode is unloaded.
        /// </summary>
        public virtual void OnExitMode()
        {
        }
    }
}
