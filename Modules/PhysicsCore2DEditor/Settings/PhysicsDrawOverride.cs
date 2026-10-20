// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;

using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEngine;

using static Unity.U2D.Physics.Scripting2D;

namespace Unity.U2D.Physics.Editor
{
    /// <summary>
    /// The Editor draw override: a per-user preference that replaces the draw settings of every physics world while it is enabled.
    /// </summary>
    /// <remarks>
    /// The override changes no stored value. Each world keeps its own draw settings, as does the project settings asset, and reading a world's draw properties still returns the world's own values.
    /// Only the drawing itself uses the override values, and only for the values selected in <see cref="Settings.overrideOptions"/>.
    /// A world opts out by setting <see cref="PhysicsWorld.disableDrawOverride"/>, after which it draws as if the override were disabled.
    /// The state is saved in the Editor preferences for the current user, so it follows them into every project and is never shared through version control.
    /// This is Editor-only and never affects a player.
    /// </remarks>
    public static partial class PhysicsDrawOverride
    {
        /// <summary>
        /// Selects which draw values the override replaces while it is enabled.
        /// </summary>
        /// <remarks>
        /// A value whose option is not set is left to each world. <see cref="HideWithGizmos"/> has no matching value: it hides all physics drawing in any Scene or Game view whose Gizmos button is off.
        /// </remarks>
        [Flags]
        public enum OverrideOptions
        {
            /// <summary>
            /// Nothing is overridden.
            /// </summary>
            None = 0,

            /// <summary>
            /// Hide physics drawing in a Scene or Game view whose Gizmos button is off.
            /// A world that opts out of the override keeps drawing, like every other override value.
            /// </summary>
            HideWithGizmos = 1 << 0,

            /// <summary>
            /// Override <see cref="Settings.drawOptions"/>.
            /// </summary>
            DrawOptions = 1 << 1,

            /// <summary>
            /// Override <see cref="Settings.drawFillOptions"/>.
            /// </summary>
            DrawFillOptions = 1 << 2,

            /// <summary>
            /// Override <see cref="Settings.drawContactType"/>.
            /// </summary>
            DrawContactType = 1 << 3,

            /// <summary>
            /// Override <see cref="Settings.drawFilter"/>.
            /// </summary>
            DrawFilter = 1 << 4,

            /// <summary>
            /// Override <see cref="Settings.drawTarget"/>.
            /// </summary>
            DrawTarget = 1 << 5,

            /// <summary>
            /// Override <see cref="Settings.drawThickness"/>.
            /// </summary>
            DrawThickness = 1 << 6,

            /// <summary>
            /// Override <see cref="Settings.drawFillAlpha"/>.
            /// </summary>
            DrawFillAlpha = 1 << 7,

            /// <summary>
            /// Override <see cref="Settings.drawPointScale"/>.
            /// </summary>
            DrawPointScale = 1 << 8,

            /// <summary>
            /// Override <see cref="Settings.drawNormalScale"/>.
            /// </summary>
            DrawNormalScale = 1 << 9,

            /// <summary>
            /// Override <see cref="Settings.drawForceScale"/>.
            /// </summary>
            DrawForceScale = 1 << 10,

            /// <summary>
            /// Override <see cref="Settings.drawAtTransform"/>.
            /// </summary>
            DrawAtTransform = 1 << 11,

            /// <summary>
            /// Override <see cref="Settings.drawColors"/>.
            /// </summary>
            DrawColors = 1 << 12
        }

        /// <summary>
        /// The draw values the override uses, together with the selection of which of them apply.
        /// </summary>
        /// <remarks>
        /// Each value mirrors the draw property of the same name on <see cref="PhysicsWorld"/> and <see cref="PhysicsWorldDefinition"/>, and accepts the same range.
        /// Assign the whole struct to <see cref="PhysicsDrawOverride.settings"/> to apply a change.
        /// </remarks>
        [Serializable]
        public struct Settings
        {
            /// <summary>
            /// Selects which values the override replaces.
            /// </summary>
            public OverrideOptions overrideOptions { readonly get => (OverrideOptions)m_Data.m_OverrideOptions; set => m_Data.m_OverrideOptions = (int)value; }

            /// <summary>
            /// Limits what gets drawn to a broad selection, in place of <see cref="PhysicsWorld.drawOptions"/>.
            /// </summary>
            public PhysicsWorld.DrawOptions drawOptions { readonly get => m_Data.m_DrawOptions; set => m_Data.m_DrawOptions = value; }

            /// <summary>
            /// Controls how shape geometry is filled, in place of <see cref="PhysicsWorld.drawFillOptions"/>.
            /// </summary>
            public PhysicsWorld.DrawFillOptions drawFillOptions { readonly get => m_Data.m_DrawFillOptions; set => m_Data.m_DrawFillOptions = value; }

            /// <summary>
            /// Controls how contact points are drawn, in place of <see cref="PhysicsWorld.drawContactType"/>.
            /// </summary>
            public PhysicsWorld.DrawContactType drawContactType { readonly get => m_Data.m_DrawContactType; set => m_Data.m_DrawContactType = value; }

            /// <summary>
            /// Limits what gets drawn to a narrow selection, in place of <see cref="PhysicsWorld.drawFilter"/>.
            /// </summary>
            public PhysicsWorld.IgnoreFilter drawFilter { readonly get => m_Data.m_DrawFilter; set => m_Data.m_DrawFilter = value; }

            /// <summary>
            /// Controls which Editor views a world is drawn into, in place of <see cref="PhysicsWorld.drawTarget"/>.
            /// </summary>
            public PhysicsWorld.DrawTarget drawTarget { readonly get => m_Data.m_DrawTarget; set => m_Data.m_DrawTarget = value; }

            /// <summary>
            /// The outline and orientation thickness, in place of <see cref="PhysicsWorld.drawThickness"/>.
            /// </summary>
            public float drawThickness { readonly get => m_Data.m_DrawThickness; set => m_Data.m_DrawThickness = Mathf.Clamp(value, 1f, 10f); }

            /// <summary>
            /// The interior fill alpha, in place of <see cref="PhysicsWorld.drawFillAlpha"/>.
            /// </summary>
            public float drawFillAlpha { readonly get => m_Data.m_DrawFillAlpha; set => m_Data.m_DrawFillAlpha = Mathf.Clamp01(value); }

            /// <summary>
            /// The scale used when drawing points, in place of <see cref="PhysicsWorld.drawPointScale"/>.
            /// </summary>
            public float drawPointScale { readonly get => m_Data.m_DrawPointScale; set => m_Data.m_DrawPointScale = Mathf.Clamp(value, 0.001f, 10f); }

            /// <summary>
            /// The scale used when drawing contact normals, in place of <see cref="PhysicsWorld.drawNormalScale"/>.
            /// </summary>
            public float drawNormalScale { readonly get => m_Data.m_DrawNormalScale; set => m_Data.m_DrawNormalScale = Mathf.Clamp(value, 0.0001f, 10f); }

            /// <summary>
            /// The scale used when drawing contact forces, in place of <see cref="PhysicsWorld.drawForceScale"/>.
            /// </summary>
            public float drawForceScale { readonly get => m_Data.m_DrawForceScale; set => m_Data.m_DrawForceScale = Mathf.Clamp(value, 0.0001f, 10f); }

            /// <summary>
            /// Draws bodies at their Transform's pose rather than their simulated pose, in place of <see cref="PhysicsWorld.drawAtTransform"/>.
            /// </summary>
            public bool drawAtTransform { readonly get => m_Data.m_DrawAtTransform; set => m_Data.m_DrawAtTransform = value; }

            /// <summary>
            /// The colors used to draw bodies, shapes, joints and contacts, in place of <see cref="PhysicsWorld.drawColors"/>.
            /// </summary>
            public PhysicsWorld.DrawColors drawColors { readonly get => m_Data.m_DrawColors; set => m_Data.m_DrawColors = value; }

            #region Internal

            // The values in the layout native expects, so they are sent as they are and saved as one JSON string.
            [SerializeField] internal PhysicsWorld.DrawOverrides m_Data;

            #endregion
        }

        // Loads the saved preference, seeding it from the default world definition the first time, and applies it.
        // Runs on every code load, since the statics are cleared on a code reload while the saved preference is not.
        [OnCodeLoaded]
        private static void Load()
        {
            s_Enabled = EditorPrefs.GetBool(k_EnabledKey, false);
            s_Settings = EditorPrefs.HasKey(k_SettingsKey) ? JsonUtility.FromJson<Settings>(EditorPrefs.GetString(k_SettingsKey)) : defaultSettings;

            Apply();
        }

        // Sends the current state to native, installs or removes the gizmo check, and repaints the views so the change shows at once.
        private static void Apply()
        {
            PhysicsGlobal_SetDrawOverrides(s_Enabled, s_Settings.m_Data);

            var hideWithGizmos = s_Enabled && (s_Settings.overrideOptions & OverrideOptions.HideWithGizmos) != 0;
            PhysicsWorldRenderer.s_RenderViewCheck = hideWithGizmos ? new Func<bool>(Handles.ShouldRenderGizmos) : null;

            SceneView.RepaintAll();
        }

        // Saves the current state, applies it and tells listeners.
        private static void Save()
        {
            EditorPrefs.SetBool(k_EnabledKey, s_Enabled);
            EditorPrefs.SetString(k_SettingsKey, JsonUtility.ToJson(s_Settings));

            Apply();
            changed?.Invoke();
        }

        /// <summary>
        /// Raised after <see cref="enabled"/> or <see cref="settings"/> changes.
        /// </summary>
        /// <remarks>
        /// Editor tooling that draws into its own world can use this to follow the override, by reading <see cref="settings"/> and applying the selected values to that world.
        /// </remarks>
        [AutoStaticsCleanup]
        public static event Action changed;

        /// <summary>
        /// Controls whether the override is in effect.
        /// </summary>
        /// <remarks>
        /// While false, every world draws with its own settings and <see cref="settings"/> is kept but unused.
        /// </remarks>
        public static bool enabled
        {
            get => s_Enabled;
            set
            {
                if (s_Enabled == value)
                    return;

                s_Enabled = value;
                Save();
            }
        }

        /// <summary>
        /// The override values and the selection of which of them apply.
        /// </summary>
        /// <remarks>
        /// Read it, change the values you need and assign it back. Assigning saves it and, while <see cref="enabled"/> is true, applies it at once.
        /// </remarks>
        public static Settings settings
        {
            get => s_Settings;
            set
            {
                s_Settings = value;
                Save();
            }
        }

        /// <summary>
        /// The settings the override starts from: the current default world definition's draw values, with the draw options and hiding with gizmos selected.
        /// </summary>
        /// <remarks>
        /// The default world definition comes from the assigned settings asset, or from the engine defaults when the project has none.
        /// Assign this to <see cref="settings"/> to reset the override.
        /// </remarks>
        public static Settings defaultSettings
        {
            get
            {
                var definition = PhysicsWorldDefinition.defaultDefinition;

                return new Settings
                {
                    overrideOptions = OverrideOptions.DrawOptions | OverrideOptions.HideWithGizmos,
                    drawOptions = definition.drawOptions,
                    drawFillOptions = definition.drawFillOptions,
                    drawContactType = definition.drawContactType,
                    drawFilter = definition.drawFilter,
                    drawTarget = definition.drawTarget,
                    drawThickness = definition.drawThickness,
                    drawFillAlpha = definition.drawFillAlpha,
                    drawPointScale = definition.drawPointScale,
                    drawNormalScale = definition.drawNormalScale,
                    drawForceScale = definition.drawForceScale,
                    drawAtTransform = definition.drawAtTransform,
                    drawColors = definition.drawColors
                };
            }
        }

        #region Internal

        private const string k_EnabledKey = "PhysicsCore2D.DrawOverride.Enabled";
        private const string k_SettingsKey = "PhysicsCore2D.DrawOverride.Settings";

        [AutoStaticsCleanup] private static bool s_Enabled;
        [AutoStaticsCleanup] private static Settings s_Settings;

        #endregion
    }
}
