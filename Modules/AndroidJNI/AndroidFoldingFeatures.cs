// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Scripting;

namespace UnityEngine.Android
{
    public static partial class AndroidApplication
    {
        static AndroidJavaObject s_JavaFoldingFeaturesWrapper;
        static bool s_WindowManagerApiMissing = false;
        static AndroidFoldingFeatures s_AndroidFoldingFeatures;
        static bool s_FoldingFeaturesInitialized = false;

        [Serializable]
        class AndroidFoldingFeatures
        {
            [SerializeField] private AndroidFoldingFeature[] m_FoldingFeatures = null;
            public AndroidFoldingFeature[] foldingFeatures => m_FoldingFeatures;
        }

        class FoldingFeaturesUpdatedCallback : AndroidJavaProxy
        {
            public FoldingFeaturesUpdatedCallback()
                : base("com.unity3d.player.IFoldingFeaturesUpdatedCallback")
            {
            }

            private void onFoldingFeaturesUpdate(string foldingFeaturesJson)
            {
                s_AndroidFoldingFeatures = JsonUtility.FromJson<AndroidFoldingFeatures>(foldingFeaturesJson);
                AndroidApplication.onFoldingFeaturesUpdatedInternal?.Invoke(s_AndroidFoldingFeatures.foldingFeatures);
            }
        }

        static AndroidJavaObject GetFoldingFeaturesWrapper()
        {
            if (s_JavaFoldingFeaturesWrapper == null)
            {
                using (var javaClass = new AndroidJavaClass("com.unity3d.player.UnityFoldingFeaturesWrapper"))
                {
                    s_JavaFoldingFeaturesWrapper = javaClass.CallStatic<AndroidJavaObject>("getInstance");
                    s_WindowManagerApiMissing = s_JavaFoldingFeaturesWrapper.Call<bool>("windowManagerApiMissing");
                }
            }
            if (s_WindowManagerApiMissing)
            {
                throw new InvalidOperationException("WindowManager API is not available! Make sure your gradle project includes \"androidx.window:window\" and \"androidx.window:window-java\" dependencies.");
            }
            return s_JavaFoldingFeaturesWrapper;
        }

        static void EnsureFoldingFeaturesInitialized()
        {
            if (s_FoldingFeaturesInitialized)
            {
                return;
            }
            GetFoldingFeaturesWrapper().Call("registerFoldingFeaturesUpdatedListener", new FoldingFeaturesUpdatedCallback());
            var javaFoldingFeaturesJson = GetFoldingFeaturesWrapper().Call<String>("currentFoldingFeaturesJson");
            s_AndroidFoldingFeatures = JsonUtility.FromJson<AndroidFoldingFeatures>(javaFoldingFeaturesJson);
            s_FoldingFeaturesInitialized = true;
        }

        /// <summary>
        /// Information about the current AndroidFoldingFeatures.
        /// This information should be used immediately after requesting because the system updates it automatically.
        /// </summary>
        /// <returns>Array of AndroidFoldingFeatures if there are any, empty array otherwise.</returns>
        public static AndroidFoldingFeature[] currentFoldingFeatures
        {
            get
            {
                EnsureFoldingFeaturesInitialized();
                return s_AndroidFoldingFeatures.foldingFeatures;
            }
        }

        internal static event Action<AndroidFoldingFeature[]> onFoldingFeaturesUpdatedInternal;

        /// <summary>
        /// Callback raised when the folding features are updated.
        /// Unity passes to the callback the new array of AndroidFoldingFeatures.
        /// </summary>
        public static event Action<AndroidFoldingFeature[]> onFoldingFeaturesUpdated
        {
            add
            {
                EnsureFoldingFeaturesInitialized();
                onFoldingFeaturesUpdatedInternal += value;
            }
            remove
            {
                onFoldingFeaturesUpdatedInternal -= value;
            }
        }
    }

    /// <summary>
    /// <para>Represents how the hinge might occlude content.</para>
    /// <seealso href="https://developer.android.com/reference/androidx/window/layout/FoldingFeature.OcclusionType">developer.android.com</seealso>
    /// </summary>
    public enum AndroidFoldableOcclusionType
    {
        /// <summary>
        /// <para>The AndroidFoldingFeature occludes all content.</para>
        /// <seealso href="https://developer.android.com/reference/androidx/window/layout/FoldingFeature.OcclusionType#FULL()">developer.android.com</seealso>
        /// </summary>
        Full = 0,

        /// <summary>
        /// <para>The AndroidFoldingFeature does not occlude the content in any way.</para>
        /// <seealso href="https://developer.android.com/reference/androidx/window/layout/FoldingFeature.OcclusionType#NONE()">developer.android.com</seealso>
        /// </summary>
        None
    }

    /// <summary>
    /// <para>Represents the axis for which the AndroidFoldingFeature runs parallel to.</para>
    /// <seealso href="https://developer.android.com/reference/androidx/window/layout/FoldingFeature.Orientation">developer.android.com</seealso>
    /// </summary>
    public enum AndroidFoldableOrientation
    {
        /// <summary>
        /// <para>The width of the AndroidFoldingFeature is greater than the height.</para>
        /// <seealso href="https://developer.android.com/reference/androidx/window/layout/FoldingFeature.Orientation#HORIZONTAL()">developer.android.com</seealso>
        /// </summary>
        Horizontal = 0,

        /// <summary>
        /// <para>The height of the AndroidFoldingFeature is greater than or equal to the width.</para>
        /// <seealso href="https://developer.android.com/reference/androidx/window/layout/FoldingFeature.Orientation#VERTICAL()">developer.android.com</seealso>
        /// </summary>
        Vertical
    }

    /// <summary>
    /// <para>Represents the state of the AndroidFoldingFeature.</para>
    /// <seealso href="https://developer.android.com/reference/androidx/window/layout/FoldingFeature.State">developer.android.com</seealso>
    /// </summary>
    public enum AndroidFoldableState
    {
        /// <summary>
        /// <para>The foldable device is completely open, the screen space that is presented to the user is flat.</para>
        /// <seealso href="https://developer.android.com/reference/androidx/window/layout/FoldingFeature.State#FLAT()">developer.android.com</seealso>
        /// </summary>
        Flat = 0,

        /// <summary>
        /// <para>The foldable device's hinge is in an intermediate position between opened and closed state, there is a non-flat angle between parts of the flexible screen or between physical screen panels.</para>
        /// <seealso href="https://developer.android.com/reference/androidx/window/layout/FoldingFeature.State#HALF_OPENED()">developer.android.com</seealso>
        /// </summary>
        HalfOpened
    }

    /// <summary>
    /// <para>A feature that describes a fold in the flexible display or a hinge between two physical display panels.</para>
    /// <seealso href="https://developer.android.com/reference/androidx/window/layout/FoldingFeature">developer.android.com</seealso>
    /// </summary>
    [Serializable]
    public class AndroidFoldingFeature
    {
        [SerializeField] private int m_X = 0;
        [SerializeField] private int m_Y = 0;
        [SerializeField] private int m_Width = 0;
        [SerializeField] private int m_Height = 0;
        [SerializeField] private int m_OcclusionType = 0;
        [SerializeField] private int m_Orientation = 0;
        [SerializeField] private int m_State = 0;
        [SerializeField] private bool m_IsSeparating = false;
        private RectInt? m_Bounds = null;

        private AndroidFoldingFeature() {}

        /// <summary>
        /// <para>The bounding rectangle of the AndroidFoldingFeature within the application window in the window coordinate space.</para>
        /// <seealso href="https://developer.android.com/reference/androidx/window/layout/DisplayFeature#getBounds()">developer.android.com</seealso>
        /// </summary>
        /// <returns>The bounding rectangle of the AndroidFoldingFeature.</returns>
        public RectInt bounds
        {
            get
            {
                if (!m_Bounds.HasValue)
                {
                    m_Bounds = new RectInt(m_X, m_Y, m_Width, m_Height);
                }
                return m_Bounds.Value;
            }
        }

        /// <summary>
        /// <para>Calculates the occlusion mode to determine if a AndroidFoldingFeature occludes a part of the window.</para>
        /// <seealso href="https://developer.android.com/reference/androidx/window/layout/FoldingFeature#getOcclusionType()">developer.android.com</seealso>
        /// </summary>
        /// <returns>The occlusion mode for the AndroidFoldingFeature.</returns>
        public AndroidFoldableOcclusionType occlusionType => (AndroidFoldableOcclusionType)m_OcclusionType;

        /// <summary>
        /// <seealso href="https://developer.android.com/reference/androidx/window/layout/FoldingFeature#getOrientation()">developer.android.com</seealso>
        /// </summary>
        /// <returns>AndroidFoldableOrientation.Horizontal if the width is greater than the height, AndroidFoldableOrientation.Vertical otherwise.</returns>
        public AndroidFoldableOrientation orientation => (AndroidFoldableOrientation)m_Orientation;

        /// <summary>
        /// <seealso href="https://developer.android.com/reference/androidx/window/layout/FoldingFeature#getOcclusionType()">developer.android.com</seealso>
        /// </summary>
        /// <returns>The AndroidFoldableState for the AndroidFoldingFeature.</returns>
        public AndroidFoldableState state => (AndroidFoldableState)m_State;

        /// <summary>
        /// <para>Calculates if a AndroidFoldingFeature should be thought of as splitting the window into multiple physical areas that can be seen by users as logically separate.</para>
        /// <seealso href="https://developer.android.com/reference/androidx/window/layout/FoldingFeature#getIsSeparating()">developer.android.com</seealso>
        /// </summary>
        /// <returns>True if the AndroidFoldingFeature splits the display into two areas, false otherwise.</returns>
        public bool isSeparating => m_IsSeparating;
    }
}
