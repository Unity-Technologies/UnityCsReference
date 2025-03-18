// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEditorInternal;
using UnityEngine;

namespace UnityEditor
{
    class PresetLibraryGradientEditor : PresetLibraryEditor<GradientPresetLibrary>
    {
        public PresetLibraryGradientEditor(ScriptableObjectSaveLoadHelper<GradientPresetLibrary> helper,
                                           PresetLibraryEditorState state,
                                           System.Action<int, object> itemClickedCallback
        ) : base(helper, state, itemClickedCallback)
        {}

        public ColorSpace colorSpace { get; set; }

        protected override void DrawPreset(PresetLibrary lib, Rect rect, object presetObject)
        {
            ((GradientPresetLibrary)lib).Draw(rect, presetObject, colorSpace);
        }
    }
    internal class GradientPicker : EditorWindow
    {
        private static GradientPicker s_GradientPicker;
        public static string presetsEditorPrefID { get { return "Gradient"; } }

        private GradientEditor m_GradientEditor;
        private PresetLibraryGradientEditor m_GradientLibraryEditor;
        [SerializeField]
        private PresetLibraryEditorState m_GradientLibraryEditorState;
        private Gradient m_Gradient;
        private const int k_DefaultNumSteps = 0;
        private GUIView m_DelegateView;
        private System.Action<Gradient> m_Delegate;
        private bool m_HDR;
        private ColorSpace m_ColorSpace;
        private bool gradientChanged { get; set; }

        // Static methods
        public static void Show(Gradient newGradient, bool hdr, ColorSpace colorSpace = ColorSpace.Gamma)
        {
            Show(newGradient, hdr, colorSpace, null, GUIView.current);
        }

        public static void Show(Gradient newGradient, bool hdr, System.Action<Gradient> onGradientChanged)
        {
            Show(newGradient, hdr, ColorSpace.Gamma, onGradientChanged, null);
        }

        public static void Show(Gradient newGradient, bool hdr, ColorSpace colorSpace, System.Action<Gradient> onGradientChanged)
        {
            Show(newGradient, hdr, colorSpace, onGradientChanged, null);
        }

        private static void Show(Gradient newGradient, bool hdr, ColorSpace colorSpace, System.Action<Gradient> onGradientChanged, GUIView currentView)
        {
            PrepareShow(hdr, colorSpace);
            s_GradientPicker.m_DelegateView = currentView;
            s_GradientPicker.m_Delegate = onGradientChanged;
            s_GradientPicker.Init(newGradient, hdr, colorSpace);

            GradientPreviewCache.ClearCache();
        }

        static void PrepareShow(bool hdr, ColorSpace colorSpace)
        {
            if (s_GradientPicker == null)
            {
                string title = hdr ? "HDR Gradient Editor" : "Gradient Editor";
                if (colorSpace == ColorSpace.Linear)
                {
                    title += " (linear)";
                }
                s_GradientPicker = (GradientPicker)GetWindow(typeof(GradientPicker), true, title, false);
                Vector2 minSize = new Vector2(360, 224);
                Vector2 maxSize = new Vector2(1900, 3000);
                s_GradientPicker.minSize = minSize;
                s_GradientPicker.maxSize = maxSize;
                s_GradientPicker.wantsMouseMove = true;
                Undo.undoRedoEvent += s_GradientPicker.OnUndoPerformed;
            }

            s_GradientPicker.ShowAuxWindow(); // Use this if auto close on lost focus is wanted.
        }

        public static GradientPicker instance
        {
            get
            {
                if (!s_GradientPicker)
                    Debug.LogError("Gradient Picker not initalized, did you call Show first?");
                return s_GradientPicker;
            }
        }

        public string currentPresetLibrary
        {
            get
            {
                InitIfNeeded();
                return m_GradientLibraryEditor.currentLibraryWithoutExtension;
            }
            set
            {
                InitIfNeeded();
                m_GradientLibraryEditor.currentLibraryWithoutExtension = value;
            }
        }

        private void Init(Gradient newGradient, bool hdr, ColorSpace colorSpace)
        {
            m_Gradient = newGradient;
            m_HDR = hdr;
            m_ColorSpace = colorSpace;
            if (m_GradientEditor != null)
                m_GradientEditor.Init(newGradient, k_DefaultNumSteps, m_HDR, m_ColorSpace);
            Repaint();
        }

        private void SetGradientData(Gradient gradient)
        {
            m_Gradient.colorKeys = gradient.colorKeys;
            m_Gradient.alphaKeys = gradient.alphaKeys;
            m_Gradient.mode = gradient.mode;
            Init(m_Gradient, m_HDR, m_ColorSpace);
        }

        public static bool visible
        {
            get { return s_GradientPicker != null; }
        }

        public static Gradient gradient
        {
            get
            {
                if (s_GradientPicker != null)
                    return s_GradientPicker.m_Gradient;
                return null;
            }
        }

        public void OnEnable()
        {
            hideFlags = HideFlags.DontSave;
            // Use these if window is not an aux window for auto closing on play/stop
            //EditorApplication.playmodeStateChanged += OnPlayModeStateChanged;
        }

        public void OnDisable()
        {
            //EditorApplication.playmodeStateChanged -= OnPlayModeStateChanged;
            if (m_GradientLibraryEditorState != null)
                m_GradientLibraryEditorState.TransferEditorPrefsState(false);

            s_GradientPicker?.UnregisterEvents();
            s_GradientPicker = null;
        }

        public void OnDestroy()
        {
            this.UnregisterEvents();
            m_GradientLibraryEditor.UnloadUsedLibraries();
        }

        void OnPlayModeStateChanged()
        {
            Close();
        }

        void InitIfNeeded()
        {
            // Init editor when needed
            if (m_GradientEditor == null)
            {
                m_GradientEditor = new GradientEditor();
                m_GradientEditor.Init(m_Gradient, k_DefaultNumSteps, m_HDR, m_ColorSpace);
            }

            if (m_GradientLibraryEditorState == null)
            {
                m_GradientLibraryEditorState = new PresetLibraryEditorState(presetsEditorPrefID);
                m_GradientLibraryEditorState.TransferEditorPrefsState(true);
            }

            if (m_GradientLibraryEditor == null)
            {
                var saveLoadHelper = new ScriptableObjectSaveLoadHelper<GradientPresetLibrary>("gradients", SaveType.Text);
                m_GradientLibraryEditor = new PresetLibraryGradientEditor(saveLoadHelper, m_GradientLibraryEditorState, PresetClickedCallback);
                m_GradientLibraryEditor.showHeader = true;
                m_GradientLibraryEditor.colorSpace = m_ColorSpace;
                m_GradientLibraryEditor.minMaxPreviewHeight = new Vector2(14f, 14f);
            }
        }

        void PresetClickedCallback(int clickCount, object presetObject)
        {
            Gradient gradient = presetObject as Gradient;
            if (gradient == null)
                Debug.LogError("Incorrect object passed " + presetObject);

            SetCurrentGradient(gradient);
            UnityEditorInternal.GradientPreviewCache.ClearCache();
            gradientChanged = true;
        }

        public void OnGUI()
        {
            // When we start play (using shortcut keys) we get two OnGui calls and m_Gradient is null: so early out.
            if (m_Gradient == null)
                return;

            InitIfNeeded();

            float gradientEditorHeight = Mathf.Min(position.height, 146);
            float distBetween = 10f;
            float presetLibraryHeight = position.height - gradientEditorHeight - distBetween;

            Rect gradientEditorRect = new Rect(10, 10, position.width - 20, gradientEditorHeight - 20);
            Rect gradientLibraryRect = new Rect(0, gradientEditorHeight + distBetween, position.width, presetLibraryHeight);

            // Separator
            EditorGUI.DrawRect(new Rect(gradientLibraryRect.x, gradientLibraryRect.y - 1, gradientLibraryRect.width, 1), new Color(0, 0, 0, 0.3f));
            EditorGUI.DrawRect(new Rect(gradientLibraryRect.x, gradientLibraryRect.y, gradientLibraryRect.width, 1), new Color(1, 1, 1, 0.1f));

            // The meat
            EditorGUI.BeginChangeCheck();
            m_GradientEditor.OnGUI(gradientEditorRect);
            if (EditorGUI.EndChangeCheck())
                gradientChanged = true;
            m_GradientLibraryEditor.OnGUI(gradientLibraryRect, m_Gradient);
            if (gradientChanged)
            {
                gradientChanged = false;
                SendEvent(true);
            }
        }

        public const string GradientPickerChangedCommand = "GradientPickerChanged";

        void SendEvent(bool exitGUI)
        {
            if (m_DelegateView)
            {
                Event e = EditorGUIUtility.CommandEvent(GradientPickerChangedCommand);
                Repaint();
                m_DelegateView.SendEvent(e);
                if (exitGUI)
                    GUIUtility.ExitGUI();
            }
            if (m_Delegate != null)
            {
                m_Delegate(gradient);
            }
        }

        public static void SetCurrentGradient(Gradient gradient)
        {
            if (s_GradientPicker == null)
                return;

            s_GradientPicker.SetGradientData(gradient);
            GUI.changed = true;
        }

        public static void RefreshGradientData()
        {
            s_GradientPicker?.m_GradientEditor?.RefreshGradientData();
            RepaintWindow();
        }

        public static void CloseWindow(bool throwExitGUI = true)
        {
            if (s_GradientPicker == null)
                return;

            s_GradientPicker.UnregisterEvents();
            s_GradientPicker.Close();

            if (throwExitGUI)
                GUIUtility.ExitGUI();
        }

        public static void RepaintWindow()
        {
            if (s_GradientPicker == null)
                return;
            s_GradientPicker.Repaint();
        }

        private void UnregisterEvents()
        {
            Undo.undoRedoEvent -= this.OnUndoPerformed;
        }

        private void OnUndoPerformed(in UndoRedoInfo info)
        {
            this.Init(this.m_Gradient, this.m_HDR, this.m_ColorSpace);
        }
    }
} // namespace
