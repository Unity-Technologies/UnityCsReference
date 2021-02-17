namespace UnityEngine.UIElements
{
    /// <summary>
    /// A collection of static methods that provide simple World, Screen, and Panel coordinate transformations.
    /// </summary>
    public static class RuntimePanelUtils
    {
        /// <summary>
        /// Transforms a screen absolute position to its equivalent local coordinate on given panel.
        /// <param name="panel">The local coordinates reference panel.</param>
        /// <param name="screenPosition">The screen position to transform.</param>
        /// <returns>A position in panel coordinates that corresponds to the provided screen position.</returns>
        /// </summary>
        public static Vector2 ScreenToPanel(IPanel panel, Vector2 screenPosition)
        {
            return ((BaseRuntimePanel)panel).ScreenToPanel(screenPosition);
        }

        /// <summary>
        /// Transforms a world absolute position to its equivalent local coordinate on given panel,
        /// using provided camera for internal WorldToScreen transformation.
        /// <param name="panel">The local coordinates reference panel.</param>
        /// <param name="worldPosition">The world position to transform.</param>
        /// <param name="camera">The Camera used for internal WorldToScreen transformation.</param>
        /// <returns>A position in panel coordinates that corresponds to the provided world position.</returns>
        /// </summary>
        public static Vector2 CameraTransformWorldToPanel(IPanel panel, Vector3 worldPosition, Camera camera)
        {
            Vector2 screenPoint = camera.WorldToScreenPoint(worldPosition);
            screenPoint.y = Screen.height - screenPoint.y; // Flip y axis. TODO: add appropriate #if (if necessary)
            return ((BaseRuntimePanel)panel).ScreenToPanel(screenPoint);
        }

        /// <summary>
        /// Transforms a world position and size (in world units) to their equivalent local position and size
        /// on given panel, using provided camera for internal WorldToScreen transformation.
        /// <param name="panel">The local coordinates reference panel.</param>
        /// <param name="worldPosition">The world position to transform.</param>
        /// <param name="worldSize">The world size to transform. The object in the panel will appear to have
        /// that size when compared to other 3D objects at neighboring positions.</param>
        /// <param name="camera">The Camera used for internal WorldToScreen transformation.</param>
        /// <returns>A (position, size) Rect in panel coordinates that corresponds to the provided world position
        /// and size.</returns>
        /// </summary>
        public static Rect CameraTransformWorldToPanelRect(IPanel panel, Vector3 worldPosition, Vector2 worldSize, Camera camera)
        {
            worldSize.y = -worldSize.y; // BottomRight has negative y offset
            Vector2 topLeftOnPanel = CameraTransformWorldToPanel(panel, worldPosition, camera);
            Vector3 bottomRightInWorldFacingCam = worldPosition + camera.worldToCameraMatrix.MultiplyVector(worldSize);
            Vector2 bottomRightOnPanel = CameraTransformWorldToPanel(panel, bottomRightInWorldFacingCam, camera);
            return new Rect(topLeftOnPanel, bottomRightOnPanel - topLeftOnPanel);
        }

        /// <summary>
        /// Resets the dynamic atlas of the panel. Textured elements will be repainted.
        /// </summary>
        public static void ResetDynamicAtlas(this IPanel panel)
        {
            var p = panel as BaseVisualElementPanel;
            if (p == null)
                return;

            var atlas = p.atlas as DynamicAtlas;
            atlas?.Reset();
        }

        /// <summary>
        /// Notifies the dynamic atlas of the panel that the content of the provided texture has changed. If the dynamic
        /// atlas contains the texture, it will update it.
        /// </summary>
        /// <param name="panel">The current panel</param>
        /// <param name="texture">The texture whose content has changed.</param>
        public static void SetTextureDirty(this IPanel panel, Texture2D texture)
        {
            var p = panel as BaseVisualElementPanel;
            if (p == null)
                return;

            var atlas = p.atlas as DynamicAtlas;
            atlas?.SetDirty(texture);
        }
    }
}
