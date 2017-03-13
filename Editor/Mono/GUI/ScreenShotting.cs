// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace UnityEditor
{
    internal class ScreenShots
    {
        public static Color kToolbarBorderColor = new Color(0.54f, 0.54f, 0.54f, 1f);
        public static Color kWindowBorderColor = new Color(0.51f, 0.51f, 0.51f, 1f);
        public static bool s_TakeComponentScreenshot = false;

        [MenuItem("Window/Screenshot/Set Window Size %&l", false, 1000, true)]
        public static void SetMainWindowSize()
        {
            var main = Resources.FindObjectsOfTypeAll<MainView>()[0];
            main.window.position = new Rect(0, 0, 1024, 768);
        }

        [MenuItem("Window/Screenshot/Set Window Size Small", false, 1000, true)]
        public static void SetMainWindowSizeSmall()
        {
            var main = Resources.FindObjectsOfTypeAll<MainView>()[0];
            main.window.position = new Rect(0, 0, 800 - 38, 600);
        }

        [MenuItem("Window/Screenshot/Snap View %&j", false, 1000, true)]
        public static void Screenshot()
        {
            GUIView v = GetMouseOverView();
            if (v != null)
            {
                string name = GetGUIViewName(v);
                Rect r = v.screenPosition;
                r.y -= 1;
                r.height += 2;
                SaveScreenShot(r, name);
            }
        }

        [MenuItem("Window/Screenshot/Snap View Toolbar", false, 1000, true)]
        public static void ScreenshotToolbar()
        {
            GUIView v = GetMouseOverView();
            if (v != null)
            {
                string name = GetGUIViewName(v) + "Toolbar";
                Rect r = v.screenPosition;
                r.y += 19;
                r.height = 16;
                r.width -= 2;
                SaveScreenShotWithBorder(r, kToolbarBorderColor, name);
            }
        }

        [MenuItem("Window/Screenshot/Snap View Extended Right %&k", false, 1000, true)]
        public static void ScreenshotExtendedRight()
        {
            GUIView v = GetMouseOverView();
            if (v != null)
            {
                string name = GetGUIViewName(v) + "Extended";
                var main = Resources.FindObjectsOfTypeAll<MainView>()[0];
                Rect r = v.screenPosition;
                r.xMax = main.window.position.xMax;
                r.y -= 1;
                r.height += 2;
                SaveScreenShot(r, name);
            }
        }

        [MenuItem("Window/Screenshot/Snap Component", false, 1000, true)]
        public static void ScreenShotComponent()
        {
            s_TakeComponentScreenshot = true;
        }

        public static void ScreenShotComponent(Rect contentRect, Object target)
        {
            s_TakeComponentScreenshot = false;

            contentRect.yMax += 2;
            contentRect.xMin += 1;
            ScreenShots.SaveScreenShotWithBorder(contentRect, kWindowBorderColor, target.GetType().Name + "Inspector");
        }

        [MenuItem("Window/Screenshot/Snap Game View Content", false, 1000, true)]
        public static void ScreenGameViewContent()
        {
            string path = GetUniquePathForName("ContentExample");
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log(string.Format("Saved screenshot at {0}", path));
        }

        [MenuItem("Window/Screenshot/Toggle DeveloperBuild", false, 1000, true)]
        public static void ToggleFakeNonDeveloperBuild()
        {
            Unsupported.fakeNonDeveloperBuild = !Unsupported.fakeNonDeveloperBuild;
            InternalEditorUtility.RequestScriptReload();
            InternalEditorUtility.RepaintAllViews();
        }

        static GUIView GetMouseOverView()
        {
            GUIView v = GUIView.mouseOverView;
            if (v == null)
            {
                EditorApplication.Beep();
                Debug.LogWarning("Could not take screenshot.");
            }
            return v;
        }

        static string GetGUIViewName(GUIView view)
        {
            HostView host = view as HostView;
            if (host != null)
                return host.actualView.GetType().Name;
            return "Window";
        }

        public static void SaveScreenShot(Rect r, string name)
        {
            SaveScreenShot((int)r.width, (int)r.height, InternalEditorUtility.ReadScreenPixel(new Vector2(r.x, r.y), (int)r.width, (int)r.height), name);
        }

        // Adds a gray border around the screenshot
        // Useful for e.g. toolbars because they don't have a nice border all the way round due to the tabs
        public static string SaveScreenShotWithBorder(Rect r, Color borderColor, string name)
        {
            int w = (int)r.width;
            int h = (int)r.height;
            Color[] colors1 = InternalEditorUtility.ReadScreenPixel(new Vector2(r.x, r.y), w, h);
            Color[] colors2 = new Color[(w + 2) * (h + 2)];
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    colors2[x + 1 + (w + 2) * (y + 1)] = colors1[x + w * y];
                }
            }
            for (int x = 0; x < w + 2; x++)
            {
                colors2[x] = borderColor;
                colors2[x + (w + 2) * (h + 1)] = borderColor;
            }
            for (int y = 0; y < h + 2; y++)
            {
                colors2[y * (w + 2)] = borderColor;
                colors2[y * (w + 2) + (w + 1)] = borderColor;
            }

            return SaveScreenShot((int)(r.width + 2), (int)(r.height + 2), colors2, name);
        }

        static string SaveScreenShot(int width, int height, Color[] pixels, string name)
        {
            Texture2D t = new Texture2D(width, height);
            t.SetPixels(pixels, 0);
            t.Apply(true);

            byte[] bytes = t.EncodeToPNG();
            Object.DestroyImmediate(t, true);

            string path = GetUniquePathForName(name);
            System.IO.File.WriteAllBytes(path, bytes);
            Debug.Log(string.Format("Saved screenshot at {0}", path));
            return path;
        }

        static string GetUniquePathForName(string name)
        {
            string path = string.Format("{0}/../../{1}.png", Application.dataPath, name);
            int i = 0;
            while (System.IO.File.Exists(path))
            {
                path = string.Format("{0}/../../{1}{2:000}.png", Application.dataPath, name, i);
                i++;
            }
            return path;
        }
    }
}
