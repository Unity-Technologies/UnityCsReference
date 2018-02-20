// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEngine
{
    // How should the custom cursor be rendered
    public enum CursorMode
    {
        // Use hardware cursors on supported platforms.
        Auto = 0,

        // Force the use of software cursors.
        ForceSoftware = 1,
    }

    // How should the cursor behave?
    public enum CursorLockMode
    {
        // Normal
        None = 0,

        // Locked to the center of the game window
        Locked = 1,

        // Confined to the game window
        Confined = 2
    }

    // Cursor API for setting the cursor that is used for rendering.
    [NativeHeader("Runtime/Export/Cursor.bindings.h")]
    public class Cursor
    {
        private static void SetCursor(Texture2D texture, CursorMode cursorMode)
        {
            SetCursor(texture, Vector2.zero, cursorMode);
        }

        public static extern void SetCursor(Texture2D texture, Vector2 hotspot, CursorMode cursorMode);

        // Should the cursor be visible?
        public static extern bool visible { get; set; }

        // Is the cursor normal/locked/confined?
        public static extern CursorLockMode lockState { get; set; }
    }
}
