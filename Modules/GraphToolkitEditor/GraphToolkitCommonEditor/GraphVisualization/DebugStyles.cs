// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEngine;

namespace Unity.GraphToolkit.Editor.GraphVisualization
{
    /// <summary>
    /// Provides theme-aware visual resources for representing evaluation states when you debug a graph.
    /// </summary>
    /// <remarks>
    /// The colors adapt to the current Editor theme, so you don't have to track the theme yourself.
    ///
    /// Don't rely on colors alone to communicate a debug state. <see cref="True"/> and <see cref="False"/> differ mainly
    /// in hue, so users with a color vision deficiency might not be able to tell them apart. Pair the color with a
    /// second visual cue, such as an icon (e.g. <see cref="ConditionReference.Icon"/>), a dash pattern (e.g. <see cref="TransitionReference.IsDashed"/>),
    /// a line width override (e.g. <see cref="TransitionReference.WidthOverride"/>) or an animation (e.g. <see cref="GraphMotion.Play(TransitionReference, float)"/>)
    /// so that the state remains readable without color.
    /// </remarks>
    /// <example>
    /// A graph tool sets a debug color and icon on a transition element:
    /// <code>
    /// using var debugContext = Registry.CreateVisualizationContext(myStateMachine.ID);
    /// TransitionReference transitionRef = debugContext.GetTransitionReference(myTransition.ID);
    /// transitionRef.LineColor = DebugStyles.Pending;
    /// transitionRef.Icon = DebugStyles.PendingIcon;
    /// </code>
    /// </example>
    public static partial class DebugStyles
    {
        [AutoStaticsCleanupOnCodeReload]
        static Texture2D s_TrueIcon;
        [AutoStaticsCleanupOnCodeReload]
        static Texture2D s_FalseIcon;
        [AutoStaticsCleanupOnCodeReload]
        static Texture2D s_PendingIcon;

        /// <summary>
        /// The suggested color to represent a true or passing state during debug visualization.
        /// </summary>
        public static Color True => Themed(new Color(0 / 255f, 129 / 255f, 38 / 255f), new Color(20 / 255f, 211 / 255f, 104 / 255f));

        /// <summary>
        /// The suggested color to represent a false or failing state during debug visualization.
        /// </summary>
        public static Color False => Themed(new Color(177 / 255f, 12 / 255f, 12 / 255f), new Color(255 / 255f, 83 / 255f, 74 / 255f));

        /// <summary>
        /// The suggested color to represent a pending or undetermined state during debug visualization.
        /// </summary>
        public static Color Pending => Themed(new Color(85 / 255f, 85 / 255f, 85 / 255f), new Color(196 / 255f, 196 / 255f, 196 / 255f));

        /// <summary>
        /// The suggested icon to represent a true or passing state during debug visualization.
        /// </summary>
        public static Texture2D TrueIcon => s_TrueIcon ??= LoadIcon("TrueDebugging");

        /// <summary>
        /// The suggested icon to represent a false or failing state during debug visualization.
        /// </summary>
        public static Texture2D FalseIcon => s_FalseIcon ??= LoadIcon("FalseDebugging");

        /// <summary>
        /// The suggested icon to represent a pending or undetermined state during debug visualization.
        /// </summary>
        public static Texture2D PendingIcon => s_PendingIcon ??= LoadIcon("PendingDebugging");

        static Color Themed(Color light, Color dark) => EditorGUIUtility.isProSkin ? dark : light;

        static Texture2D LoadIcon(string fileName) => EditorGUIUtility.LoadIcon($"{GraphElementHelper.k_IconFolder}StateMachine/Transitions/{fileName}.png");
    }
}
