// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine.Bindings;
using UnityEngine.Scripting;
using UnityEngine.Internal;

namespace UnityEngine
{
    // GUI STUFF
    // --------------------------------
    [NativeHeader("Modules/IMGUI/GUIStyle.h")]
    [NativeHeader("Runtime/Camera/RenderLayers/GUILayer.h")]
    [UsedByNativeCode]
    public partial class RectOffset
    {
        [ThreadAndSerializationSafe]
        private static extern IntPtr InternalCreate();

        [ThreadAndSerializationSafe]
        private static extern void InternalDestroy(IntPtr ptr);

        // Left edge size.
        [NativeProperty("left", false, TargetType.Field)]
        public extern int left { get; set; }

        // Right edge size.
        [NativeProperty("right", false, TargetType.Field)]
        public extern int right { get; set; }

        // Top edge size.
        [NativeProperty("top", false, TargetType.Field)]
        public extern int top { get; set; }

        // Bottom edge size.
        [NativeProperty("bottom", false, TargetType.Field)]
        public extern int bottom { get; set; }

        // shortcut for left + right (RO)
        public extern int horizontal { get; }

        // shortcut for top + bottom (RO)
        public extern int vertical { get; }

        // Add the border offsets to a /rect/.
        public extern Rect Add(Rect rect);

        // Remove the border offsets from a /rect/.
        public extern Rect Remove(Rect rect);
    }


    // Base class for images & text strings displayed in a GUI.
    [RequireComponent(typeof(Transform))]
    public class GUIElement : Behaviour
    {
        [ExcludeFromDocs]
        public bool HitTest(Vector3 screenPosition)
        {
            return HitTest(new Vector2(screenPosition.x, screenPosition.y), null);
        }

        // Is a point on screen inside the element.
        public bool HitTest(Vector3 screenPosition, [UnityEngine.Internal.DefaultValue("null")] Camera camera)
        {
            return HitTest(new Vector2(screenPosition.x, screenPosition.y), GetCameraOrWindowRect(camera));
        }

        // Returns bounding rectangle of [[GUIElement]] in screen coordinates.
        public Rect GetScreenRect([UnityEngine.Internal.DefaultValue("null")] Camera camera)
        {
            return GetScreenRect(GetCameraOrWindowRect(camera));
        }

        [ExcludeFromDocs]
        public Rect GetScreenRect()
        {
            return GetScreenRect(null);
        }

        private extern Rect GetScreenRect(Rect rect);
        private extern bool HitTest(Vector2 screenPosition, Rect cameraRect);

        private static Rect GetCameraOrWindowRect(Camera camera)
        {
            if (camera != null)
            {
                return camera.pixelRect;
            }

            return new Rect(0, 0, Screen.width, Screen.height);
        }
    }


    // A texture image used in a 2D GUI.
    [NativeHeader("Runtime/Camera/RenderLayers/GUITexture.h")]
    [System.Obsolete("This component is part of the legacy UI system and will be removed in a future release.")]
    public class GUITexture : GUIElement
    {
        // The color of the GUI texture.
        public extern Color color { get; set; }

        // The texture used for drawing.
        public extern Texture texture { get; set; }

        // Pixel inset used for pixel adjustments for size and position.
        public extern Rect pixelInset { get; set; }

        // The border defines the number of pixels from the edge that are not affected by scale.
        public extern RectOffset border { get; set; }
    }


    // [[Component]] added to a camera to make it render 2D GUI elements.
    [RequireComponent(typeof(Camera))]
    [Obsolete("This component is part of the legacy UI system and will be removed in a future release.")]
    public class GUILayer : Behaviour
    {
        // Get the GUI element at a specific screen position.
        public GUIElement HitTest(Vector3 screenPosition)
        {
            return HitTest(new Vector2(screenPosition.x, screenPosition.y));
        }

        private extern GUIElement HitTest(Vector2 screenPosition);
    }
}
