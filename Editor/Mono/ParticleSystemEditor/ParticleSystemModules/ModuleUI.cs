// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;


namespace UnityEditor
{
    abstract partial class ModuleUI : SerializedModule
    {
        public ParticleSystemUI m_ParticleSystemUI; // owner
        private string m_DisplayName;
        protected string m_ToolTip = "";
        private SerializedProperty m_Enabled;
        private VisibilityState m_VisibilityState;
        public List<SerializedProperty> m_ModuleCurves = new List<SerializedProperty>();
        private List<SerializedProperty> m_CurvesRemovedWhenFolded = new List<SerializedProperty>();

        public enum VisibilityState
        {
            NotVisible = 0,
            VisibleAndFolded = 1,
            VisibleAndFoldedOut = 2
        };

        public bool visibleUI
        {
            get { return m_VisibilityState != VisibilityState.NotVisible; }
            set { SetVisibilityState(value ? VisibilityState.VisibleAndFolded : VisibilityState.NotVisible); }
        }
        public bool foldout
        {
            get { return m_VisibilityState == VisibilityState.VisibleAndFoldedOut; }
            set { SetVisibilityState(value ? VisibilityState.VisibleAndFoldedOut : VisibilityState.VisibleAndFolded); }
        }

        public bool enabled
        {
            get
            {
                return m_Enabled.boolValue;
            }
            set
            {
                if (m_Enabled.boolValue != value)
                {
                    m_Enabled.boolValue = value;
                    if (value)
                        OnModuleEnable();
                    else
                        OnModuleDisable();
                }
            }
        }

        public bool enabledHasMultipleDifferentValues
        {
            get { return m_Enabled.hasMultipleDifferentValues; }
        }

        public string displayName
        {
            get { return m_DisplayName; }
        }

        public string toolTip
        {
            get { return m_ToolTip; }
        }

        public bool isWindowView
        {
            get { return m_ParticleSystemUI.m_ParticleEffectUI.m_Owner is ParticleSystemWindow; }
        }

        public ModuleUI(ParticleSystemUI owner, SerializedObject o, string name, string displayName)
            : base(o, name)
        {
            Setup(owner, o, displayName, VisibilityState.NotVisible);
        }

        public ModuleUI(ParticleSystemUI owner, SerializedObject o, string name, string displayName, VisibilityState initialVisibilityState)
            : base(o, name)
        {
            Setup(owner, o, displayName, initialVisibilityState);
        }

        private void Setup(ParticleSystemUI owner, SerializedObject o, string displayName, VisibilityState defaultVisibilityState)
        {
            m_ParticleSystemUI = owner;
            m_DisplayName = displayName;

            if (this is RendererModuleUI)
                m_Enabled = GetProperty0("m_Enabled");
            else
                m_Enabled = GetProperty("enabled");

            m_VisibilityState = VisibilityState.NotVisible;
            foreach (Object obj in o.targetObjects)
            {
                VisibilityState state = (VisibilityState)SessionState.GetInt(GetUniqueModuleName(obj), (int)defaultVisibilityState);
                m_VisibilityState = (VisibilityState)Mathf.Max((int)state, (int)m_VisibilityState); // use most visible state
            }

            CheckVisibilityState();

            if (foldout)
                Init();
        }

        protected abstract void Init();
        public virtual void Validate() {}
        public virtual float GetXAxisScalar() {return 1f; }
        public abstract void OnInspectorGUI(InitialModuleUI initial);
        public virtual void OnSceneViewGUI() {} // Like OnSceneGUI but only called once when in multi edit mode where OnSceneGUI is called for each target.
        public virtual void UpdateCullingSupportedString(ref string text) {}

        protected virtual void OnModuleEnable()
        {
            Undo.undoRedoPerformed += UndoRedoPerformed;
            Init(); // ensure initialized
        }

        public virtual void UndoRedoPerformed()
        {
            // If the undo operation has changed the module to now be disabled then we should remove any of its curves from the curve editor. (case 861424)
            if (!enabled)
                OnModuleDisable();
        }

        protected virtual void OnModuleDisable()
        {
            Undo.undoRedoPerformed -= UndoRedoPerformed;
            ParticleSystemCurveEditor psce = m_ParticleSystemUI.m_ParticleEffectUI.GetParticleSystemCurveEditor();
            foreach (SerializedProperty curveProp in m_ModuleCurves)
            {
                if (psce.IsAdded(curveProp))
                    psce.RemoveCurve(curveProp);
            }
        }

        internal void CheckVisibilityState()
        {
            bool isRendererModule = this is RendererModuleUI;

            // Ensure disabled modules are only visible if show all modules is true. Except the renderer module, we want that
            // to be shown always if the module is there which means that we have a ParticleSystemRenderer
            if (!isRendererModule && !m_Enabled.boolValue && !ParticleEffectUI.GetAllModulesVisible())
                SetVisibilityState(VisibilityState.NotVisible);

            // Ensure enabled modules are visible
            if (m_Enabled.boolValue && !visibleUI)
                SetVisibilityState(VisibilityState.VisibleAndFolded);
        }

        protected virtual void SetVisibilityState(VisibilityState newState)
        {
            if (newState != m_VisibilityState)
            {
                if (newState == VisibilityState.VisibleAndFolded)
                {
                    // Remove curves from the curveeditor when closing modules (and put them back when folding out again)
                    ParticleSystemCurveEditor psce = m_ParticleSystemUI.m_ParticleEffectUI.GetParticleSystemCurveEditor();
                    foreach (SerializedProperty curveProp in m_ModuleCurves)
                    {
                        if (psce.IsAdded(curveProp))
                        {
                            m_CurvesRemovedWhenFolded.Add(curveProp);
                            psce.SetVisible(curveProp, false);
                        }
                    }
                    psce.Refresh();
                }
                else if (newState == VisibilityState.VisibleAndFoldedOut)
                {
                    ParticleSystemCurveEditor psce = m_ParticleSystemUI.m_ParticleEffectUI.GetParticleSystemCurveEditor();
                    foreach (SerializedProperty curveProp in m_CurvesRemovedWhenFolded)
                    {
                        psce.SetVisible(curveProp, true);
                    }
                    m_CurvesRemovedWhenFolded.Clear();
                    psce.Refresh();
                }

                m_VisibilityState = newState;
                foreach (Object obj in serializedObject.targetObjects)
                {
                    SessionState.SetInt(GetUniqueModuleName(obj), (int)m_VisibilityState);
                }

                if (newState == VisibilityState.VisibleAndFoldedOut)
                    Init();
            }
        }

        protected ParticleSystem GetParticleSystem()
        {
            return m_Enabled.serializedObject.targetObject as ParticleSystem;
        }

        public ParticleSystemCurveEditor GetParticleSystemCurveEditor()
        {
            return m_ParticleSystemUI.m_ParticleEffectUI.GetParticleSystemCurveEditor();
        }

        public void AddToModuleCurves(SerializedProperty curveProp)
        {
            m_ModuleCurves.Add(curveProp);
            if (!foldout)
                m_CurvesRemovedWhenFolded.Add(curveProp);
        }

        // See ParticleSystemGUI.cs for more ModuleUI GUI helper functions...
    }
} // namespace UnityEditor
