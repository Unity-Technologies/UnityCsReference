// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEditorInternal;

namespace UnityEditor
{
    internal class LookDevConfig
        : ScriptableObject
    {
        [SerializeField] private LookDevContext[]       m_LookDevContexts = new LookDevContext[2];
        [SerializeField] private LookDevPropertyInfo[]  m_LookDevProperties = new LookDevPropertyInfo[(int)LookDevProperty.Count];
        [SerializeField] private GizmoInfo              m_Gizmo = new GizmoInfo();
        [SerializeField] private LookDevMode            m_LookDevMode = LookDevMode.Single1;
        [SerializeField] private bool                   m_EnableToneMap = true;
        [SerializeField] private bool                   m_EnableShadowCubemap = true;
        [SerializeField] private float                  m_ExposureRange = 8.0f;
        [SerializeField] private float                  m_ShadowDistance = 0.0f;
        [SerializeField] private bool                   m_ShowBalls = false;
        [SerializeField] private bool                   m_ShowControlWindows = true;
        [SerializeField] private bool                   m_RotateObjectMode = false;
        [SerializeField] private float                  m_EnvRotationSpeed = 1.0f;
        [SerializeField] private bool                   m_RotateEnvMode = false;
        [SerializeField] private float                  m_ObjRotationSpeed = 1.0f;
        [SerializeField] private bool                   m_AllowDifferentObjects = false;
        [SerializeField] private GameObject[][]         m_PreviewObjects =
        {
            new GameObject[(int)LookDevView.PreviewContext.PreviewContextPass.kCount],
            new GameObject[(int)LookDevView.PreviewContext.PreviewContextPass.kCount],
        };// Those are the actual preview objects
        [SerializeField] private LookDevEditionContext  m_CurrentContextEdition = LookDevEditionContext.Left;
        [SerializeField] private int                    m_CurrentEditionContextIndex = 0;
        [SerializeField] private float                  m_DualViewBlendFactor = 0.0f;
        [SerializeField] private GameObject[]           m_OriginalGameObject = new GameObject[2];
        [SerializeField] private CameraState[]          m_CameraState = new CameraState[2];
        [SerializeField] private bool                   m_SideBySideCameraLinked = false;
        [SerializeField] private CameraState            m_CameraStateCommon = new CameraState();
        [SerializeField] private CameraState            m_CameraStateLeft = new CameraState();
        [SerializeField] private CameraState            m_CameraStateRight = new CameraState();
        GameObject[][] m_CurrentObjectInstances =
        {
            new GameObject[(int)LookDevView.PreviewContext.PreviewContextPass.kCount],
            new GameObject[(int)LookDevView.PreviewContext.PreviewContextPass.kCount],
        };

        private LookDevView m_LookDevView = null;

        public bool enableShadowCubemap
        {
            get { return m_EnableShadowCubemap; }
            set { m_EnableShadowCubemap = value; m_LookDevView.Repaint(); }
        }

        public bool sideBySideCameraLinked
        {
            get { return m_SideBySideCameraLinked; }
            set { m_SideBySideCameraLinked = value; }
        }

        public int currentEditionContextIndex
        {
            get { return m_CurrentEditionContextIndex; }
        }


        public LookDevEditionContext currentEditionContext
        {
            get { return m_CurrentContextEdition; }
        }

        public float dualViewBlendFactor
        {
            get { return m_DualViewBlendFactor; }
            set { m_DualViewBlendFactor = value; }
        }

        public GizmoInfo gizmo
        {
            get { return m_Gizmo; }
            set { m_Gizmo = value; }
        }

        public LookDevContext[] lookDevContexts
        {
            get { return m_LookDevContexts; }
        }

        public LookDevContext currentLookDevContext
        {
            get { return m_LookDevContexts[m_CurrentEditionContextIndex]; }
        }

        public GameObject[][] currentObjectInstances
        {
            get { return m_CurrentObjectInstances; }
        }
        public GameObject[][] previewObjects
        {
            get { return m_PreviewObjects; }
        }

        public CameraState[] cameraState
        {
            get { return m_CameraState; }
        }

        public CameraState cameraStateCommon
        {
            get { return m_CameraStateCommon; }
            set { m_CameraStateCommon = value; }
        }
        public CameraState cameraStateLeft
        {
            get { return m_CameraStateLeft; }
            set { m_CameraStateLeft = value; }
        }
        public CameraState cameraStateRight
        {
            get { return m_CameraStateRight; }
            set { m_CameraStateRight = value; }
        }

        public LookDevMode lookDevMode
        {
            get { return m_LookDevMode; }
            set
            {
                m_LookDevMode = value;
                UpdateCameraArray();
                UpdateCurrentObjectArray();
            }
        }

        public bool enableToneMap
        {
            get { return m_EnableToneMap; }
            set { m_EnableToneMap = value; m_LookDevView.Repaint(); }
        }

        public bool allowDifferentObjects
        {
            get { return m_AllowDifferentObjects; }
            set { m_AllowDifferentObjects = value; ResynchronizeObjects(); m_LookDevView.Repaint(); }
        }

        public float exposureRange
        {
            get { return m_ExposureRange; }
            set { m_ExposureRange = value; m_LookDevView.Repaint(); }
        }

        public float shadowDistance
        {
            get { return m_ShadowDistance; }
            set { m_ShadowDistance = value; m_LookDevView.Repaint(); }
        }

        public bool showBalls
        {
            get { return m_ShowBalls; }
            set
            {
                m_ShowBalls = value;
                m_LookDevView.Repaint();
            }
        }

        public bool showControlWindows
        {
            get { return m_ShowControlWindows; }
            set
            {
                m_ShowControlWindows = value;
                m_LookDevView.Repaint();
            }
        }

        public bool rotateObjectMode
        {
            get { return m_RotateObjectMode; }
            set { m_RotateObjectMode = value; }
        }

        public float objRotationSpeed
        {
            get { return m_ObjRotationSpeed; }
            set { m_ObjRotationSpeed = value; m_LookDevView.Repaint(); }
        }

        public bool rotateEnvMode
        {
            get { return m_RotateEnvMode; }
            set { m_RotateEnvMode = value; }
        }

        public float envRotationSpeed
        {
            get { return m_EnvRotationSpeed; }
            set { m_EnvRotationSpeed = value; m_LookDevView.Repaint(); }
        }

        public LookDevConfig()
        {
            m_LookDevProperties[(int)LookDevProperty.ExposureValue] = new LookDevPropertyInfo(LookDevPropertyType.Float);
            m_LookDevProperties[(int)LookDevProperty.EnvRotation] = new LookDevPropertyInfo(LookDevPropertyType.Float);
            m_LookDevProperties[(int)LookDevProperty.HDRI] = new LookDevPropertyInfo(LookDevPropertyType.Int);
            m_LookDevProperties[(int)LookDevProperty.LoDIndex] = new LookDevPropertyInfo(LookDevPropertyType.Int);
            m_LookDevProperties[(int)LookDevProperty.ShadingMode] = new LookDevPropertyInfo(LookDevPropertyType.Int);
        }

        // Can't use generics properly for the life of me... if anyone has a better suggestion than copy/paste...I'll take it!
        public void UpdateFloatProperty(LookDevProperty type, float value)
        {
            UpdateFloatProperty(type, value, true, false);
        }

        public void UpdateFloatProperty(LookDevProperty type, float value, bool recordUndo)
        {
            UpdateFloatProperty(type, value, recordUndo, false);
        }

        public void UpdateIntProperty(LookDevProperty property, int value)
        {
            UpdateIntProperty(property, value, true, false);
        }

        public void UpdateIntProperty(LookDevProperty property, int value, bool recordUndo)
        {
            UpdateIntProperty(property, value, recordUndo, false);
        }

        public float GetFloatProperty(LookDevProperty property, LookDevEditionContext context)
        {
            return m_LookDevContexts[(int)context].GetProperty(property).floatValue;
        }

        public int GetIntProperty(LookDevProperty property, LookDevEditionContext context)
        {
            return m_LookDevContexts[(int)context].GetProperty(property).intValue;
        }

        public void UpdateFloatProperty(LookDevProperty property, float value, bool recordUndo, bool forceLinked)
        {
            if (recordUndo)
            {
                Undo.RecordObject(this, "Update Float property for " + property + " with value " + value);
            }

            lookDevContexts[m_CurrentEditionContextIndex].UpdateProperty(property, value);

            if (m_LookDevProperties[(int)property].linked || forceLinked)
            {
                lookDevContexts[(m_CurrentEditionContextIndex + 1) % 2].UpdateProperty(property, value);
            }
            m_LookDevView.Repaint();
        }

        public void UpdateIntProperty(LookDevProperty property, int value, bool recordUndo, bool forceLinked)
        {
            if (recordUndo)
            {
                Undo.RecordObject(this, "Update Int property for " + property + " with value " + value);
            }

            lookDevContexts[m_CurrentEditionContextIndex].UpdateProperty(property, value);

            if (m_LookDevProperties[(int)property].linked || forceLinked)
            {
                lookDevContexts[(m_CurrentEditionContextIndex + 1) % 2].UpdateProperty(property, value);
            }
            m_LookDevView.Repaint();
        }

        public bool IsPropertyLinked(LookDevProperty type)
        {
            return m_LookDevProperties[(int)type].linked;
        }

        public void UpdatePropertyLink(LookDevProperty property, bool value)
        {
            Undo.RecordObject(this, "Update Link for property " + property);

            m_LookDevProperties[(int)property].linked = value;

            switch (m_LookDevProperties[(int)property].propertyType)
            {
                case LookDevPropertyType.Int:
                    UpdateIntProperty(property, lookDevContexts[m_CurrentEditionContextIndex].GetProperty(property).intValue, true, false);
                    break;
                case LookDevPropertyType.Float:
                    UpdateFloatProperty(property, lookDevContexts[m_CurrentEditionContextIndex].GetProperty(property).floatValue, true, false);
                    break;
            }
            m_LookDevView.Repaint();
        }

        public int GetObjectLoDCount(LookDevEditionContext context)
        {
            if (m_CurrentObjectInstances[(int)context][0] != null)
            {
                LODGroup lodGroup = m_CurrentObjectInstances[(int)context][0].GetComponent(typeof(LODGroup)) as LODGroup;
                if (lodGroup != null)
                {
                    return lodGroup.lodCount;
                }
            }

            return 1;
        }

        public void UpdateFocus(LookDevEditionContext context)
        {
            // Caution: m_CurrentContextEditionMode should never be set to ContextEditionMode.kNone it is used only for drag context.
            Debug.Assert(context != LookDevEditionContext.None);

            if (context != LookDevEditionContext.None)
            {
                m_CurrentContextEdition = context;
                m_CurrentEditionContextIndex = (int)m_CurrentContextEdition;
                m_LookDevView.Repaint();
            }
        }

        private void DestroyCurrentPreviewObject(LookDevEditionContext context)
        {
            var index = (int)context;
            for (var j = 0; j < m_PreviewObjects[index].Length; j++)
            {
                if (m_PreviewObjects[index][j] != null)
                {
                    DestroyImmediate(m_PreviewObjects[index][j]);
                    m_PreviewObjects[index][j] = null;
                }
            }
        }

        internal static void DisableRendererProperties(GameObject go)
        {
            foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>())
            {
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.allowOcclusionWhenDynamic = false;
            }
        }

        public void ResynchronizeObjects()
        {
            Undo.RecordObject(this, "Resync objects");
            for (int i = 0; i < 2; i++)
                SetCurrentPreviewObject(m_OriginalGameObject[m_CurrentEditionContextIndex], (LookDevEditionContext)i);
            m_LookDevView.Frame(false);
        }

        private void UpdateCameraArray()
        {
            // When we go from side by side or single view to zone or split, we want to copy the last selected camera as common.
            if (m_LookDevMode == LookDevMode.SideBySide || m_LookDevMode == LookDevMode.Single1 || m_LookDevMode == LookDevMode.Single2)
            {
                m_CameraState[0] = m_CameraStateLeft;
                m_CameraState[1] = m_CameraStateRight;
            }
            else
            {
                m_CameraState[0] = m_CameraStateCommon;
                m_CameraState[1] = m_CameraStateCommon;

                CameraState currentState = m_CurrentContextEdition == LookDevEditionContext.Left ? m_CameraStateLeft : m_CameraStateRight;
                m_CameraStateCommon.Copy(currentState);
            }
        }

        public void UpdateCurrentObjectArray()
        {
            if (allowDifferentObjects)
            {
                for (var i = 0; i < m_PreviewObjects[0].Length; ++i)
                {
                    m_CurrentObjectInstances[(int)LookDevEditionContext.Left][i] = m_PreviewObjects[(int)LookDevEditionContext.Left][i];
                    m_CurrentObjectInstances[(int)LookDevEditionContext.Right][i] = m_PreviewObjects[(int)LookDevEditionContext.Right][i];
                }
            }
            else
            {
                for (var i = 0; i < m_PreviewObjects[0].Length; ++i)
                {
                    m_CurrentObjectInstances[m_CurrentEditionContextIndex][i] = m_PreviewObjects[m_CurrentEditionContextIndex][i];
                    m_CurrentObjectInstances[(m_CurrentEditionContextIndex + 1) % 2][i] = m_PreviewObjects[m_CurrentEditionContextIndex][i];
                }
            }
        }

        // return true if both view have been updated (i.e the object is loaded in both view)
        public bool SetCurrentPreviewObject(GameObject go)
        {
            SetCurrentPreviewObject(go, m_CurrentContextEdition);
            // Set other window to the same objects if there isn't one already
            int otherIndex = (m_CurrentEditionContextIndex + 1) % 2;
            if (m_PreviewObjects[otherIndex][0] == null || !m_AllowDifferentObjects)
            {
                SetCurrentPreviewObject(go, (LookDevEditionContext)otherIndex);
                return true;
            }

            return false;
        }

        public void SetCurrentPreviewObject(GameObject go, LookDevEditionContext context)
        {
            DestroyCurrentPreviewObject(context);

            if (go != null)
            {
                int index = (int)context;
                if (m_LookDevView == null
                    || m_LookDevView.previewUtilityContexts == null
                    || m_LookDevView.previewUtilityContexts[index] == null)
                    return;

                m_OriginalGameObject[index] = go;

                for (var i = 0; i < m_PreviewObjects[index].Length; ++i)
                {
                    m_PreviewObjects[index][i] = m_LookDevView.previewUtilityContexts[index].m_PreviewUtility[i].InstantiatePrefabInScene(m_OriginalGameObject[index]);
                    m_PreviewObjects[index][i].transform.position = new Vector3(0.0f, 0.0f, 0.0f);
                    m_PreviewObjects[index][i].transform.rotation = Quaternion.identity;
                    EditorUtility.InitInstantiatedPreviewRecursive(m_PreviewObjects[index][i]);
                    DisableRendererProperties(m_PreviewObjects[index][i]); // Avoid light probe influence from the main scene (but still have the default probe lighting)
                    PreviewRenderUtility.SetEnabledRecursive(m_PreviewObjects[index][i], false);
                }

                UpdateCurrentObjectArray();
            }
        }

        public void OnEnable()
        {
            if (m_LookDevContexts[0] == null)
            {
                for (int i = 0; i < 2; ++i)
                {
                    m_LookDevContexts[i] = new LookDevContext();
                }
            }

            InitializeCurrentObjects();

            UpdateCameraArray();

            Undo.undoRedoPerformed += OnUndoRedo;
        }

        void InitializeCurrentObjects()
        {
            for (int i = 0; i < 2; ++i)
            {
                if (m_OriginalGameObject[i] != null)
                    SetCurrentPreviewObject(m_OriginalGameObject[i], (LookDevEditionContext)i);
            }
        }

        void OnUndoRedo()
        {
            for (int i = 0; i < 2; ++i)
            {
                if (m_OriginalGameObject[i] != null)
                {
                    SetCurrentPreviewObject(m_OriginalGameObject[i], (LookDevEditionContext)i);
                }
            }
        }

        public void OnDestroy()
        {
            DestroyCurrentPreviewObject(LookDevEditionContext.Left);
            DestroyCurrentPreviewObject(LookDevEditionContext.Right);
        }

        public void Cleanup()
        {
            m_CurrentEditionContextIndex = 0;

            DestroyCurrentPreviewObject(LookDevEditionContext.Left);
            DestroyCurrentPreviewObject(LookDevEditionContext.Right);
        }

        public void SetLookDevView(LookDevView lookDevView)
        {
            if (m_LookDevView != lookDevView)
            {
                m_LookDevView = lookDevView;
                InitializeCurrentObjects();
            }
        }
    }
}
