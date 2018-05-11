// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    internal class AvatarPreview
    {
        const string kIkPref = "AvatarpreviewShowIK";
        const string k2DPref = "Avatarpreview2D";
        const string kReferencePref = "AvatarpreviewShowReference";
        const string kSpeedPref = "AvatarpreviewSpeed";
        const float kTimeControlRectHeight = 21;

        public delegate void OnAvatarChange();
        OnAvatarChange m_OnAvatarChangeFunc = null;

        public OnAvatarChange OnAvatarChangeFunc
        {
            set { m_OnAvatarChangeFunc = value; }
        }

        public bool IKOnFeet
        {
            get { return m_IKOnFeet; }
        }

        public bool ShowIKOnFeetButton
        {
            get { return m_ShowIKOnFeetButton; }
            set { m_ShowIKOnFeetButton = value; }
        }

        public bool is2D
        {
            get { return m_2D; }
            set
            {
                m_2D = value;
                if (m_2D)
                {
                    m_PreviewDir = new Vector2();
                }
            }
        }

        public Animator Animator
        {
            get { return m_PreviewInstance != null ? m_PreviewInstance.GetComponent(typeof(Animator)) as Animator : null; }
        }

        public GameObject PreviewObject
        {
            get { return m_PreviewInstance; }
        }

        public ModelImporterAnimationType animationClipType
        {
            get { return GetAnimationType(m_SourcePreviewMotion); }
        }

        public Vector3 bodyPosition
        {
            get
            {
                if (Animator && Animator.isHuman)
                    return Animator.GetBodyPositionInternal();

                if (m_PreviewInstance != null)
                    return GameObjectInspector.GetRenderableCenterRecurse(m_PreviewInstance, 1, 8);

                return Vector3.zero;
            }
        }

        public Vector3 rootPosition
        {
            get { return m_PreviewInstance ? m_PreviewInstance.transform.position : Vector3.zero; }
        }


        public TimeControl timeControl;

        // 60 is the default framerate for animations created inside Unity so as good a default as any.
        public int fps = 60;

        private Material     m_FloorMaterial;
        private Material     m_FloorMaterialSmall;
        private Material     m_ShadowMaskMaterial;
        private Material     m_ShadowPlaneMaterial;

        PreviewRenderUtility        m_PreviewUtility;
        GameObject                  m_PreviewInstance;
        GameObject                  m_ReferenceInstance;
        GameObject                  m_DirectionInstance;
        GameObject                  m_PivotInstance;
        GameObject                  m_RootInstance;
        float                       m_BoundingVolumeScale;
        Motion                      m_SourcePreviewMotion;
        Animator                    m_SourceScenePreviewAnimator;

        const string                s_PreviewStr = "Preview";
        int                         m_PreviewHint = s_PreviewStr.GetHashCode();

        const string                s_PreviewSceneStr = "PreviewSene";
        int                         m_PreviewSceneHint = s_PreviewSceneStr.GetHashCode();

        Texture2D                   m_FloorTexture;
        Mesh                        m_FloorPlane;

        bool                        m_ShowReference = false;

        bool                        m_IKOnFeet = false;
        bool                        m_ShowIKOnFeetButton = true;

        bool                        m_2D;

        bool                        m_IsValid;
        int                         m_ModelSelectorId = EditorGUIUtility.GetPermanentControlID();


        private const float kFloorFadeDuration = 0.2f;
        private const float kFloorScale = 5;
        private const float kFloorScaleSmall = 0.2f;
        private const float kFloorTextureScale = 4;
        private const float kFloorAlpha = 0.5f;
        private const float kFloorShadowAlpha = 0.3f;

        private float m_PrevFloorHeight = 0;
        private float m_NextFloorHeight = 0;

        private Vector2 m_PreviewDir = new Vector2(120, -20);
        private float m_AvatarScale = 1.0f;
        private float m_ZoomFactor = 1.0f;
        private Vector3 m_PivotPositionOffset = Vector3.zero;

        private class Styles
        {
            public GUIContent speedScale = EditorGUIUtility.IconContent("SpeedScale", "|Changes animation preview speed");
            public GUIContent pivot = EditorGUIUtility.IconContent("AvatarPivot", "|Displays avatar's pivot and mass center");
            public GUIContent ik = new GUIContent("IK", "Toggles feet IK preview");
            public GUIContent is2D = new GUIContent("2D", "Toggles 2D preview mode");
            public GUIContent avatarIcon = EditorGUIUtility.IconContent("Avatar Icon", "|Changes the model to use for previewing.");

            public GUIStyle preButton = "preButton";
            public GUIStyle preSlider = "preSlider";
            public GUIStyle preSliderThumb = "preSliderThumb";
            public GUIStyle preLabel = "preLabel";
        }
        private static Styles s_Styles;

        void SetPreviewCharacterEnabled(bool enabled, bool showReference)
        {
            if (m_PreviewInstance != null)
                PreviewRenderUtility.SetEnabledRecursive(m_PreviewInstance, enabled);
            PreviewRenderUtility.SetEnabledRecursive(m_ReferenceInstance, showReference && enabled);
            PreviewRenderUtility.SetEnabledRecursive(m_DirectionInstance, showReference && enabled);
            PreviewRenderUtility.SetEnabledRecursive(m_PivotInstance, showReference && enabled);
            PreviewRenderUtility.SetEnabledRecursive(m_RootInstance, showReference && enabled);
        }

        static AnimationClip GetFirstAnimationClipFromMotion(Motion motion)
        {
            AnimationClip clip = motion as AnimationClip;
            if (clip)
                return clip;

            Animations.BlendTree blendTree = motion as Animations.BlendTree;
            if (blendTree)
            {
                AnimationClip[] clips = blendTree.GetAnimationClipsFlattened();
                if (clips.Length > 0)
                    return clips[0];
            }

            return null;
        }

        static public ModelImporterAnimationType GetAnimationType(GameObject go)
        {
            Animator animator = go.GetComponent<Animator>();
            if (animator)
            {
                Avatar avatar = animator.avatar;
                if (avatar && avatar.isHuman)
                    return ModelImporterAnimationType.Human;
                else
                    return ModelImporterAnimationType.Generic;
            }
            else if (go.GetComponent<Animation>() != null)
            {
                return ModelImporterAnimationType.Legacy;
            }
            else
                return ModelImporterAnimationType.None;
        }

        static public ModelImporterAnimationType GetAnimationType(Motion motion)
        {
            AnimationClip clip = GetFirstAnimationClipFromMotion(motion);
            if (clip)
            {
                if (clip.legacy)
                    return ModelImporterAnimationType.Legacy;
                else if (clip.humanMotion)
                    return ModelImporterAnimationType.Human;
                else
                    return ModelImporterAnimationType.Generic;
            }
            else
                return ModelImporterAnimationType.None;
        }

        static public bool IsValidPreviewGameObject(GameObject target, ModelImporterAnimationType requiredClipType)
        {
            if (target != null && !target.activeSelf)
                Debug.LogWarning("Can't preview inactive object, using fallback object");

            return target != null && target.activeSelf && GameObjectInspector.HasRenderableParts(target) &&
                !(requiredClipType != ModelImporterAnimationType.None && GetAnimationType(target) != requiredClipType);
        }

        static public GameObject FindBestFittingRenderableGameObjectFromModelAsset(Object asset, ModelImporterAnimationType animationType)
        {
            if (asset == null)
                return null;

            ModelImporter importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(asset)) as ModelImporter;
            if (importer == null)
                return null;

            string assetPath = importer.CalculateBestFittingPreviewGameObject();
            GameObject tempGO = AssetDatabase.LoadMainAssetAtPath(assetPath) as GameObject;

            // We should also check for isHumanClip matching the animationclip requiremenets...
            if (IsValidPreviewGameObject(tempGO, ModelImporterAnimationType.None))
                return tempGO;
            else
                return null;
        }

        static GameObject CalculatePreviewGameObject(Animator selectedAnimator, Motion motion, ModelImporterAnimationType animationType)
        {
            AnimationClip sourceClip = GetFirstAnimationClipFromMotion(motion);

            // Use selected preview
            GameObject selected = AvatarPreviewSelection.GetPreview(animationType);
            if (IsValidPreviewGameObject(selected, ModelImporterAnimationType.None))
                return selected;

            if (selectedAnimator != null && IsValidPreviewGameObject(selectedAnimator.gameObject, animationType))
                return selectedAnimator.gameObject;

            // Find the best fitting preview game object for the asset we are viewing (Handles @ convention, will pick base path for you)
            selected = FindBestFittingRenderableGameObjectFromModelAsset(sourceClip, animationType);
            if (selected != null)
                return selected;

            if (animationType == ModelImporterAnimationType.Human)
                return GetHumanoidFallback();
            else if (animationType == ModelImporterAnimationType.Generic)
                return GetGenericAnimationFallback();

            return null;
        }

        static GameObject GetGenericAnimationFallback()
        {
            return (GameObject)EditorGUIUtility.Load("Avatar/DefaultGeneric.fbx");
        }

        static GameObject GetHumanoidFallback()
        {
            return (GameObject)EditorGUIUtility.Load("Avatar/DefaultAvatar.fbx");
        }

        public void ResetPreviewInstance()
        {
            Object.DestroyImmediate(m_PreviewInstance);
            GameObject go = CalculatePreviewGameObject(m_SourceScenePreviewAnimator, m_SourcePreviewMotion, animationClipType);
            SetupBounds(go);
        }

        void SetupBounds(GameObject go)
        {
            m_IsValid = go != null && go != GetGenericAnimationFallback();

            if (go != null)
            {
                m_PreviewInstance = EditorUtility.InstantiateForAnimatorPreview(go);
                previewUtility.AddSingleGO(m_PreviewInstance);

                Bounds bounds = new Bounds(m_PreviewInstance.transform.position, Vector3.zero);
                GameObjectInspector.GetRenderableBoundsRecurse(ref bounds, m_PreviewInstance);

                m_BoundingVolumeScale = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));


                if (Animator && Animator.isHuman)
                    m_AvatarScale = m_ZoomFactor = Animator.humanScale;
                else
                    m_AvatarScale = m_ZoomFactor = m_BoundingVolumeScale / 2;
            }
        }

        void InitInstance(Animator scenePreviewObject, Motion motion)
        {
            m_SourcePreviewMotion = motion;
            m_SourceScenePreviewAnimator = scenePreviewObject;

            if (m_PreviewInstance == null)
            {
                GameObject go = CalculatePreviewGameObject(scenePreviewObject, motion, animationClipType);
                SetupBounds(go);
            }

            if (timeControl == null)
            {
                timeControl = new TimeControl();
            }

            if (m_ReferenceInstance == null)
            {
                GameObject referenceGO = (GameObject)EditorGUIUtility.Load("Avatar/dial_flat.prefab");
                m_ReferenceInstance = (GameObject)Object.Instantiate(referenceGO, Vector3.zero, Quaternion.identity);
                EditorUtility.InitInstantiatedPreviewRecursive(m_ReferenceInstance);
                previewUtility.AddSingleGO(m_ReferenceInstance);
            }

            if (m_DirectionInstance == null)
            {
                GameObject directionGO = (GameObject)EditorGUIUtility.Load("Avatar/arrow.fbx");
                m_DirectionInstance = (GameObject)Object.Instantiate(directionGO, Vector3.zero, Quaternion.identity);
                EditorUtility.InitInstantiatedPreviewRecursive(m_DirectionInstance);
                previewUtility.AddSingleGO(m_DirectionInstance);
            }

            if (m_PivotInstance == null)
            {
                GameObject pivotGO = (GameObject)EditorGUIUtility.Load("Avatar/root.fbx");
                m_PivotInstance = (GameObject)Object.Instantiate(pivotGO, Vector3.zero, Quaternion.identity);
                EditorUtility.InitInstantiatedPreviewRecursive(m_PivotInstance);
                previewUtility.AddSingleGO(m_PivotInstance);
            }

            if (m_RootInstance == null)
            {
                GameObject rootGO = (GameObject)EditorGUIUtility.Load("Avatar/root.fbx");
                m_RootInstance = (GameObject)Object.Instantiate(rootGO, Vector3.zero, Quaternion.identity);
                EditorUtility.InitInstantiatedPreviewRecursive(m_RootInstance);
                previewUtility.AddSingleGO(m_RootInstance);
            }

            // Load preview settings from prefs
            m_IKOnFeet = EditorPrefs.GetBool(kIkPref, false);
            m_ShowReference = EditorPrefs.GetBool(kReferencePref, true);
            is2D = EditorPrefs.GetBool(k2DPref, EditorSettings.defaultBehaviorMode == EditorBehaviorMode.Mode2D);
            timeControl.playbackSpeed = EditorPrefs.GetFloat(kSpeedPref, 1f);

            SetPreviewCharacterEnabled(false, false);

            m_PivotPositionOffset = Vector3.zero;
        }

        private PreviewRenderUtility previewUtility
        {
            get
            {
                if (m_PreviewUtility == null)
                {
                    m_PreviewUtility = new PreviewRenderUtility();
                    m_PreviewUtility.camera.fieldOfView = 30.0f;
                    m_PreviewUtility.camera.allowHDR = false;
                    m_PreviewUtility.camera.allowMSAA = false;
                    m_PreviewUtility.ambientColor = new Color(.1f, .1f, .1f, 0);
                    m_PreviewUtility.lights[0].intensity = 1.4f;
                    m_PreviewUtility.lights[0].transform.rotation = Quaternion.Euler(40f, 40f, 0);
                    m_PreviewUtility.lights[1].intensity = 1.4f;
                }
                return m_PreviewUtility;
            }
        }

        private void Init()
        {
            if (s_Styles == null)
                s_Styles = new Styles();

            if (m_FloorPlane == null)
            {
                m_FloorPlane = Resources.GetBuiltinResource(typeof(Mesh), "New-Plane.fbx") as Mesh;
            }

            if (m_FloorTexture == null)
            {
                m_FloorTexture = (Texture2D)EditorGUIUtility.Load("Avatar/Textures/AvatarFloor.png");
            }

            if (m_FloorMaterial == null)
            {
                Shader shader = EditorGUIUtility.LoadRequired("Previews/PreviewPlaneWithShadow.shader") as Shader;
                m_FloorMaterial = new Material(shader);
                m_FloorMaterial.mainTexture = m_FloorTexture;
                m_FloorMaterial.mainTextureScale = Vector2.one * kFloorScale * kFloorTextureScale;
                m_FloorMaterial.SetVector("_Alphas", new Vector4(kFloorAlpha, kFloorShadowAlpha, 0, 0));
                m_FloorMaterial.hideFlags = HideFlags.HideAndDontSave;

                m_FloorMaterialSmall = new Material(m_FloorMaterial);
                m_FloorMaterialSmall.mainTextureScale = Vector2.one * kFloorScaleSmall * kFloorTextureScale;
                m_FloorMaterialSmall.hideFlags = HideFlags.HideAndDontSave;
            }

            if (m_ShadowMaskMaterial == null)
            {
                Shader shader = EditorGUIUtility.LoadRequired("Previews/PreviewShadowMask.shader") as Shader;
                m_ShadowMaskMaterial = new Material(shader);
                m_ShadowMaskMaterial.hideFlags = HideFlags.HideAndDontSave;
            }

            if (m_ShadowPlaneMaterial == null)
            {
                Shader shader = EditorGUIUtility.LoadRequired("Previews/PreviewShadowPlaneClip.shader") as Shader;
                m_ShadowPlaneMaterial = new Material(shader);
                m_ShadowPlaneMaterial.hideFlags = HideFlags.HideAndDontSave;
            }
        }

        public void OnDestroy()
        {
            if (m_PreviewUtility != null)
            {
                m_PreviewUtility.Cleanup();
                m_PreviewUtility = null;
            }

            if (timeControl != null)
                timeControl.OnDisable();
        }

        public void DoSelectionChange()
        {
            m_OnAvatarChangeFunc();
        }

        public AvatarPreview(Animator previewObjectInScene, Motion objectOnSameAsset)
        {
            InitInstance(previewObjectInScene, objectOnSameAsset);
        }

        float PreviewSlider(float val, float snapThreshold)
        {
            val = GUILayout.HorizontalSlider(val, 0.1f, 2.0f, s_Styles.preSlider, s_Styles.preSliderThumb, GUILayout.MaxWidth(64));
            if (val > 0.25f - snapThreshold && val < 0.25f + snapThreshold)
                val = 0.25f;
            else if (val > 0.5f - snapThreshold && val < 0.5f + snapThreshold)
                val = 0.5f;
            else if (val > 0.75f - snapThreshold && val < 0.75f + snapThreshold)
                val = 0.75f;
            else if (val > 1.0f - snapThreshold && val < 1.0f + snapThreshold)
                val = 1.0f;
            else if (val > 1.25f - snapThreshold && val < 1.25f + snapThreshold)
                val = 1.25f;
            else if (val > 1.5f - snapThreshold && val < 1.5f + snapThreshold)
                val = 1.5f;
            else if (val > 1.75f - snapThreshold && val < 1.75f + snapThreshold)
                val = 1.75f;

            return val;
        }

        public void DoPreviewSettings()
        {
            Init();

            if (m_ShowIKOnFeetButton)
            {
                EditorGUI.BeginChangeCheck();
                m_IKOnFeet = GUILayout.Toggle(m_IKOnFeet, s_Styles.ik, s_Styles.preButton);
                if (EditorGUI.EndChangeCheck())
                    EditorPrefs.SetBool(kIkPref, m_IKOnFeet);
            }

            EditorGUI.BeginChangeCheck();
            GUILayout.Toggle(is2D, s_Styles.is2D, s_Styles.preButton);
            if (EditorGUI.EndChangeCheck())
            {
                is2D = !is2D;
                EditorPrefs.SetBool(k2DPref, is2D);
            }

            EditorGUI.BeginChangeCheck();
            m_ShowReference = GUILayout.Toggle(m_ShowReference, s_Styles.pivot, s_Styles.preButton);
            if (EditorGUI.EndChangeCheck())
                EditorPrefs.SetBool(kReferencePref, m_ShowReference);

            GUILayout.Box(s_Styles.speedScale, s_Styles.preLabel);
            EditorGUI.BeginChangeCheck();
            timeControl.playbackSpeed = PreviewSlider(timeControl.playbackSpeed, 0.03f);
            if (EditorGUI.EndChangeCheck())
                EditorPrefs.SetFloat(kSpeedPref, timeControl.playbackSpeed);
            GUILayout.Label(timeControl.playbackSpeed.ToString("f2"), s_Styles.preLabel);
        }

        private RenderTexture RenderPreviewShadowmap(Light light, float scale, Vector3 center, Vector3 floorPos, out Matrix4x4 outShadowMatrix)
        {
            Assert.IsTrue(Event.current.type == EventType.Repaint);

            // Set ortho camera and position it
            var cam = previewUtility.camera;
            cam.orthographic = true;
            cam.orthographicSize = scale * 2.0f;
            cam.nearClipPlane = 1 * scale;
            cam.farClipPlane = 25 * scale;
            cam.transform.rotation = is2D ? Quaternion.identity : light.transform.rotation;
            cam.transform.position = center - light.transform.forward * (scale * 5.5f);

            // Clear to black
            CameraClearFlags oldFlags = cam.clearFlags;
            cam.clearFlags = CameraClearFlags.SolidColor;
            Color oldColor = cam.backgroundColor;
            cam.backgroundColor = new Color(0, 0, 0, 0);

            // Create render target for shadow map
            const int kShadowSize = 256;
            RenderTexture oldRT = cam.targetTexture;
            RenderTexture rt = RenderTexture.GetTemporary(kShadowSize, kShadowSize, 16);
            rt.isPowerOfTwo = true;
            rt.wrapMode = TextureWrapMode.Clamp;
            rt.filterMode = FilterMode.Bilinear;
            cam.targetTexture = rt;

            // Enable character and render with camera into the shadowmap
            SetPreviewCharacterEnabled(true, false);
            m_PreviewUtility.camera.Render();

            // Draw a quad, with shader that will produce white color everywhere
            // where something was rendered (via inverted depth test)
            RenderTexture.active = rt;
            GL.PushMatrix();
            GL.LoadOrtho();
            m_ShadowMaskMaterial.SetPass(0);
            GL.Begin(GL.QUADS);
            GL.Vertex3(0, 0, -99.0f);
            GL.Vertex3(1, 0, -99.0f);
            GL.Vertex3(1, 1, -99.0f);
            GL.Vertex3(0, 1, -99.0f);
            GL.End();

            // Render floor with black color, to mask out any shadow from character
            // parts that are under the preview plane
            GL.LoadProjectionMatrix(cam.projectionMatrix);
            GL.LoadIdentity();
            GL.MultMatrix(cam.worldToCameraMatrix);
            m_ShadowPlaneMaterial.SetPass(0);
            GL.Begin(GL.QUADS);
            float sc = kFloorScale * scale;
            GL.Vertex(floorPos + new Vector3(-sc, 0, -sc));
            GL.Vertex(floorPos + new Vector3(sc, 0, -sc));
            GL.Vertex(floorPos + new Vector3(sc, 0, sc));
            GL.Vertex(floorPos + new Vector3(-sc, 0, sc));
            GL.End();

            GL.PopMatrix();

            // Shadowmap sampling matrix, from world space into shadowmap space
            Matrix4x4 texMatrix = Matrix4x4.TRS(new Vector3(0.5f, 0.5f, 0.5f), Quaternion.identity,
                    new Vector3(0.5f, 0.5f, 0.5f));
            outShadowMatrix = texMatrix * cam.projectionMatrix * cam.worldToCameraMatrix;

            // Restore previous camera parameters
            cam.orthographic = false;
            cam.clearFlags = oldFlags;
            cam.backgroundColor = oldColor;
            cam.targetTexture = oldRT;

            return rt;
        }

        public void DoRenderPreview(Rect previewRect, GUIStyle background)
        {
            var probe = RenderSettings.ambientProbe;
            previewUtility.BeginPreview(previewRect, background);

            Quaternion bodyRot;
            Quaternion rootRot;
            Vector3 rootPos;
            Vector3 bodyPos = rootPosition;
            Vector3 pivotPos;

            if (Animator && Animator.isHuman)
            {
                rootRot = Animator.rootRotation;
                rootPos = Animator.rootPosition;

                bodyRot = Animator.bodyRotation;

                pivotPos = Animator.pivotPosition;
            }
            else if (Animator && Animator.hasRootMotion)
            {
                rootRot = Animator.rootRotation;
                rootPos = Animator.rootPosition;

                bodyRot = Quaternion.identity;

                pivotPos = Vector3.zero;
            }
            else
            {
                rootRot = Quaternion.identity;
                rootPos = Vector3.zero;

                bodyRot = Quaternion.identity;

                pivotPos = Vector3.zero;
            }

            SetupPreviewLightingAndFx(probe);

            Vector3 direction = bodyRot * Vector3.forward;
            direction[1] = 0;
            Quaternion directionRot = Quaternion.LookRotation(direction);
            Vector3 directionPos = rootPos;

            Quaternion pivotRot = rootRot;

            // Scale all Preview Objects to fit avatar size.
            PositionPreviewObjects(pivotRot, pivotPos, bodyRot, bodyPosition, directionRot, rootRot, rootPos, directionPos, m_AvatarScale);

            bool dynamicFloorHeight = is2D ? false : Mathf.Abs(m_NextFloorHeight - m_PrevFloorHeight) > m_ZoomFactor * 0.01f;

            // Calculate floor height and alpha
            float mainFloorHeight, mainFloorAlpha;
            if (dynamicFloorHeight)
            {
                float fadeMoment = m_NextFloorHeight < m_PrevFloorHeight ? kFloorFadeDuration : (1 - kFloorFadeDuration);
                mainFloorHeight = timeControl.normalizedTime < fadeMoment ? m_PrevFloorHeight : m_NextFloorHeight;
                mainFloorAlpha = Mathf.Clamp01(Mathf.Abs(timeControl.normalizedTime - fadeMoment) / kFloorFadeDuration);
            }
            else
            {
                mainFloorHeight = m_PrevFloorHeight;
                mainFloorAlpha = is2D ? 0.5f : 1;
            }

            Quaternion floorRot = is2D ? Quaternion.Euler(-90, 0, 0) : Quaternion.identity;
            Vector3 floorPos = new Vector3(0, 0, 0);
            floorPos = m_ReferenceInstance.transform.position;
            floorPos.y = mainFloorHeight;

            // Render shadow map
            Matrix4x4 shadowMatrix;
            RenderTexture shadowMap = RenderPreviewShadowmap(previewUtility.lights[0], m_BoundingVolumeScale / 2, bodyPosition, floorPos, out shadowMatrix);

            float tempZoomFactor = (is2D ? 1.0f : m_ZoomFactor);
            // Position camera
            previewUtility.camera.orthographic = is2D;
            previewUtility.camera.nearClipPlane = 0.5f * tempZoomFactor;
            previewUtility.camera.farClipPlane = 100.0f * m_AvatarScale;
            Quaternion camRot = Quaternion.Euler(-m_PreviewDir.y, -m_PreviewDir.x, 0);

            // Add panning offset
            Vector3 camPos = camRot * (Vector3.forward * -5.5f * tempZoomFactor) + bodyPos + m_PivotPositionOffset;
            previewUtility.camera.transform.position = camPos;
            previewUtility.camera.transform.rotation = camRot;

            // Texture offset - negative in order to compensate the floor movement.
            Vector2 textureOffset = -new Vector2(floorPos.x, is2D ? floorPos.y : floorPos.z);

            if (is2D)
                previewUtility.camera.orthographicSize = 2.0f * m_ZoomFactor;
            // Render main floor
            {
                if (!is2D)
                    floorPos.y = mainFloorHeight;

                Material mat = m_FloorMaterial;
                Matrix4x4 matrix = Matrix4x4.TRS(floorPos, floorRot, Vector3.one * kFloorScale * m_AvatarScale);

                mat.mainTextureOffset = textureOffset * kFloorScale * 0.08f * (1.0f / m_AvatarScale);
                mat.SetTexture("_ShadowTexture", shadowMap);
                mat.SetMatrix("_ShadowTextureMatrix", shadowMatrix);
                mat.SetVector("_Alphas", new Vector4(kFloorAlpha * mainFloorAlpha, kFloorShadowAlpha * mainFloorAlpha, 0, 0));
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Background;

                Graphics.DrawMesh(m_FloorPlane, matrix, mat, Camera.PreviewCullingLayer, previewUtility.camera, 0);
            }

            // Render small floor
            if (dynamicFloorHeight)
            {
                bool topIsNext = m_NextFloorHeight > m_PrevFloorHeight;
                float floorHeight = topIsNext ? m_NextFloorHeight : m_PrevFloorHeight;
                float otherFloorHeight = topIsNext ? m_PrevFloorHeight : m_NextFloorHeight;
                float floorAlpha = (floorHeight == mainFloorHeight ? 1 - mainFloorAlpha : 1) * Mathf.InverseLerp(otherFloorHeight, floorHeight, rootPos.y);
                floorPos.y = floorHeight;

                Material mat = m_FloorMaterialSmall;
                mat.mainTextureOffset = textureOffset * kFloorScaleSmall * 0.08f;
                mat.SetTexture("_ShadowTexture", shadowMap);
                mat.SetMatrix("_ShadowTextureMatrix", shadowMatrix);
                mat.SetVector("_Alphas", new Vector4(kFloorAlpha * floorAlpha, 0, 0, 0));
                Matrix4x4 matrix = Matrix4x4.TRS(floorPos, floorRot, Vector3.one * kFloorScaleSmall * m_AvatarScale);
                Graphics.DrawMesh(m_FloorPlane, matrix, mat, Camera.PreviewCullingLayer, previewUtility.camera, 0);
            }

            SetPreviewCharacterEnabled(true, m_ShowReference);
            previewUtility.Render(m_Option != PreviewPopupOptions.DefaultModel);
            SetPreviewCharacterEnabled(false, false);

            RenderTexture.ReleaseTemporary(shadowMap);
        }

        private void SetupPreviewLightingAndFx(SphericalHarmonicsL2 probe)
        {
            previewUtility.lights[0].intensity = 1.4f;
            previewUtility.lights[0].transform.rotation = Quaternion.Euler(40f, 40f, 0);
            previewUtility.lights[1].intensity = 1.4f;
            RenderSettings.ambientMode = AmbientMode.Custom;
            RenderSettings.ambientLight = new Color(0.1f, 0.1f, 0.1f, 1.0f);
            RenderSettings.ambientProbe = probe;
        }

        private float m_LastNormalizedTime = -1000;
        private float m_LastStartTime = -1000;
        private float m_LastStopTime = -1000;
        private bool m_NextTargetIsForward = true;
        private void PositionPreviewObjects(Quaternion pivotRot, Vector3 pivotPos, Quaternion bodyRot, Vector3 bodyPos,
            Quaternion directionRot, Quaternion rootRot, Vector3 rootPos, Vector3 directionPos,
            float scale)
        {
            m_ReferenceInstance.transform.position = rootPos;
            m_ReferenceInstance.transform.rotation = rootRot;
            m_ReferenceInstance.transform.localScale = Vector3.one * scale * 1.25f;

            m_DirectionInstance.transform.position = directionPos;
            m_DirectionInstance.transform.rotation = directionRot;
            m_DirectionInstance.transform.localScale = Vector3.one * scale * 2;

            m_PivotInstance.transform.position = pivotPos;
            m_PivotInstance.transform.rotation = pivotRot;
            m_PivotInstance.transform.localScale = Vector3.one * scale * 0.1f;

            m_RootInstance.transform.position = bodyPos;
            m_RootInstance.transform.rotation = bodyRot;
            m_RootInstance.transform.localScale = Vector3.one * scale * 0.25f;

            if (Animator)
            {
                float normalizedTime = timeControl.normalizedTime;
                float normalizedDelta = timeControl.deltaTime / (timeControl.stopTime - timeControl.startTime);

                // Always set last height to next height after wrapping the time.
                if (normalizedTime - normalizedDelta < 0 || normalizedTime - normalizedDelta >= 1)
                    m_PrevFloorHeight = m_NextFloorHeight;

                // Check that AvatarPreview is getting reliable info about time and deltaTime.
                if (m_LastNormalizedTime != -1000 && timeControl.startTime == m_LastStartTime && timeControl.stopTime == m_LastStopTime)
                {
                    float difference = normalizedTime - normalizedDelta - m_LastNormalizedTime;
                    if (difference > 0.5f)
                        difference -= 1;
                    else if (difference < -0.5f)
                        difference += 1;
                }
                m_LastNormalizedTime = normalizedTime;
                m_LastStartTime = timeControl.startTime;
                m_LastStopTime = timeControl.stopTime;

                // Alternate getting the height for next time and previous time.
                if (m_NextTargetIsForward)
                    m_NextFloorHeight = Animator.targetPosition.y;
                else
                    m_PrevFloorHeight = Animator.targetPosition.y;

                // Flip next target time.
                m_NextTargetIsForward = !m_NextTargetIsForward;
                Animator.SetTarget(AvatarTarget.Root, m_NextTargetIsForward ? 1 : 0);
            }
        }

        public void AvatarTimeControlGUI(Rect rect)
        {
            Rect timeControlRect = rect;
            timeControlRect.height = kTimeControlRectHeight;

            timeControl.DoTimeControl(timeControlRect);

            // Show current time in seconds:frame and in percentage
            rect.y = rect.yMax - 20;
            float time = timeControl.currentTime - timeControl.startTime;
            EditorGUI.DropShadowLabel(new Rect(rect.x, rect.y, rect.width, 20),
                string.Format("{0,2}:{1:00} ({2:000.0%}) Frame {3}", (int)time, Repeat(Mathf.FloorToInt(time * fps), fps), timeControl.normalizedTime, Mathf.FloorToInt(timeControl.currentTime * fps))
                );
        }

        enum PreviewPopupOptions { Auto, DefaultModel, Other }

        protected enum ViewTool { None, Pan, Zoom, Orbit }
        protected ViewTool m_ViewTool = ViewTool.None;
        protected ViewTool viewTool
        {
            get
            {
                Event evt = Event.current;
                if (m_ViewTool == ViewTool.None)
                {
                    bool controlKeyOnMac = (evt.control && Application.platform == RuntimePlatform.OSXEditor);

                    // actionKey could be command key on mac or ctrl on windows
                    bool actionKey = EditorGUI.actionKey;

                    bool noModifiers = (!actionKey && !controlKeyOnMac && !evt.alt);

                    if ((evt.button <= 0 && noModifiers) || (evt.button <= 0 && actionKey) || evt.button == 2)
                        m_ViewTool = ViewTool.Pan;
                    else if ((evt.button <= 0 && controlKeyOnMac) || (evt.button == 1 && evt.alt))
                        m_ViewTool = ViewTool.Zoom;
                    else if (evt.button <= 0 && evt.alt || evt.button == 1)
                        m_ViewTool = ViewTool.Orbit;
                }
                return m_ViewTool;
            }
        }

        protected MouseCursor currentCursor
        {
            get
            {
                switch (m_ViewTool)
                {
                    case ViewTool.Orbit: return MouseCursor.Orbit;
                    case ViewTool.Pan: return MouseCursor.Pan;
                    case ViewTool.Zoom: return MouseCursor.Zoom;
                    default: return MouseCursor.Arrow;
                }
            }
        }


        protected void HandleMouseDown(Event evt, int id, Rect previewRect)
        {
            if (viewTool != ViewTool.None && previewRect.Contains(evt.mousePosition))
            {
                EditorGUIUtility.SetWantsMouseJumping(1);
                evt.Use();
                GUIUtility.hotControl = id;
            }
        }

        protected void HandleMouseUp(Event evt, int id)
        {
            if (GUIUtility.hotControl == id)
            {
                m_ViewTool = ViewTool.None;

                GUIUtility.hotControl = 0;
                EditorGUIUtility.SetWantsMouseJumping(0);
                evt.Use();
            }
        }

        protected void HandleMouseDrag(Event evt, int id, Rect previewRect)
        {
            if (m_PreviewInstance == null)
                return;

            if (GUIUtility.hotControl == id)
            {
                switch (m_ViewTool)
                {
                    case ViewTool.Orbit:    DoAvatarPreviewOrbit(evt, previewRect); break;
                    case ViewTool.Pan:      DoAvatarPreviewPan(evt); break;

                    // case 605415 invert zoom delta to match scene view zooming
                    case ViewTool.Zoom:     DoAvatarPreviewZoom(evt, -HandleUtility.niceMouseDeltaZoom * (evt.shift ? 2.0f : 0.5f)); break;
                    default:                Debug.Log("Enum value not handled"); break;
                }
            }
        }

        protected void HandleViewTool(Event evt, EventType eventType, int id, Rect previewRect)
        {
            switch (eventType)
            {
                case EventType.ScrollWheel: DoAvatarPreviewZoom(evt, HandleUtility.niceMouseDeltaZoom * (evt.shift ? 2.0f : 0.5f)); break;
                case EventType.MouseDown:   HandleMouseDown(evt, id, previewRect); break;
                case EventType.MouseUp:     HandleMouseUp(evt, id); break;
                case EventType.MouseDrag:   HandleMouseDrag(evt, id, previewRect); break;
            }
        }

        public void DoAvatarPreviewDrag(EventType type)
        {
            if (type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
            }
            else if (type == EventType.DragPerform)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Link;
                GameObject newPreviewObject = DragAndDrop.objectReferences[0] as GameObject;

                if (newPreviewObject)
                {
                    DragAndDrop.AcceptDrag();
                    SetPreview(newPreviewObject);
                }
            }
        }

        public void DoAvatarPreviewOrbit(Event evt, Rect previewRect)
        {
            //Reset 2D on Orbit
            if (is2D)
            {
                is2D = false;
            }
            m_PreviewDir -= evt.delta * (evt.shift ? 3 : 1) / Mathf.Min(previewRect.width, previewRect.height) * 140.0f;
            m_PreviewDir.y = Mathf.Clamp(m_PreviewDir.y, -90, 90);
            evt.Use();
        }

        public void DoAvatarPreviewPan(Event evt)
        {
            Camera cam = previewUtility.camera;
            Vector3 screenPos = cam.WorldToScreenPoint(bodyPosition + m_PivotPositionOffset);
            Vector3 delta = new Vector3(-evt.delta.x, evt.delta.y, 0);
            // delta panning is scale with the zoom factor to allow fine tuning when user is zooming closely.
            screenPos += delta * Mathf.Lerp(0.25f, 2.0f, m_ZoomFactor * 0.5f);
            Vector3 worldDelta = cam.ScreenToWorldPoint(screenPos) - (bodyPosition + m_PivotPositionOffset);
            m_PivotPositionOffset += worldDelta;
            evt.Use();
        }

        public void ResetPreviewFocus()
        {
            m_PivotPositionOffset = bodyPosition - rootPosition;
        }

        public void DoAvatarPreviewFrame(Event evt, EventType type, Rect previewRect)
        {
            if (type == EventType.KeyDown && evt.keyCode == KeyCode.F)
            {
                ResetPreviewFocus();
                m_ZoomFactor = m_AvatarScale;
                evt.Use();
            }

            if (type == EventType.KeyDown && Event.current.keyCode == KeyCode.G)
            {
                m_PivotPositionOffset = GetCurrentMouseWorldPosition(evt, previewRect) - bodyPosition;
                evt.Use();
            }
        }

        protected Vector3 GetCurrentMouseWorldPosition(Event evt, Rect previewRect)
        {
            Camera cam = previewUtility.camera;

            float scaleFactor = previewUtility.GetScaleFactor(previewRect.width, previewRect.height);
            Vector3 mouseLocal = new Vector3((evt.mousePosition.x - previewRect.x) * scaleFactor, (previewRect.height - (evt.mousePosition.y - previewRect.y)) * scaleFactor, 0);
            mouseLocal.z = Vector3.Distance(bodyPosition, cam.transform.position);
            return cam.ScreenToWorldPoint(mouseLocal);
        }

        public void DoAvatarPreviewZoom(Event evt, float delta)
        {
            float zoomDelta = -delta * 0.05f;
            m_ZoomFactor += m_ZoomFactor * zoomDelta;

            // zoom is clamp too 10 time closer than the original zoom
            m_ZoomFactor = Mathf.Max(m_ZoomFactor, m_AvatarScale / 10.0f);
            evt.Use();
        }

        public void DoAvatarPreview(Rect rect, GUIStyle background)
        {
            Init();

            Rect choserRect = new Rect(rect.xMax - 16, rect.yMax - 16, 16, 16);
            if (EditorGUI.DropdownButton(choserRect, GUIContent.none, FocusType.Passive, GUIStyle.none))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Auto"), false, SetPreviewAvatarOption, PreviewPopupOptions.Auto);
                menu.AddItem(new GUIContent("Unity Model"), false, SetPreviewAvatarOption, PreviewPopupOptions.DefaultModel);
                menu.AddItem(new GUIContent("Other..."), false, SetPreviewAvatarOption, PreviewPopupOptions.Other);
                menu.ShowAsContext();
            }

            Rect previewRect = rect;
            previewRect.yMin += kTimeControlRectHeight;
            previewRect.height = Mathf.Max(previewRect.height, 64f);

            int previewID = GUIUtility.GetControlID(m_PreviewHint, FocusType.Passive, previewRect);
            Event evt = Event.current;
            EventType type = evt.GetTypeForControl(previewID);

            if (type == EventType.Repaint && m_IsValid)
            {
                DoRenderPreview(previewRect, background);
                previewUtility.EndAndDrawPreview(previewRect);
            }

            AvatarTimeControlGUI(rect);

            GUI.DrawTexture(choserRect, s_Styles.avatarIcon.image);

            int previewSceneID = GUIUtility.GetControlID(m_PreviewSceneHint, FocusType.Passive);
            type = evt.GetTypeForControl(previewSceneID);

            DoAvatarPreviewDrag(type);
            HandleViewTool(evt, type, previewSceneID, previewRect);
            DoAvatarPreviewFrame(evt, type, previewRect);

            if (!m_IsValid)
            {
                Rect warningRect = previewRect;
                warningRect.yMax -= warningRect.height / 2 - 16;
                EditorGUI.DropShadowLabel(
                    warningRect,
                    "No model is available for preview.\nPlease drag a model into this Preview Area.");
            }

            // Check for model selected from ObjectSelector
            if (evt.type == EventType.ExecuteCommand)
            {
                string commandName = evt.commandName;
                if (commandName == "ObjectSelectorUpdated" && ObjectSelector.get.objectSelectorID == m_ModelSelectorId)
                {
                    SetPreview(ObjectSelector.GetCurrentObject() as GameObject);
                    evt.Use();
                }
            }

            // Apply the current cursor
            if (evt.type == EventType.Repaint)
                EditorGUIUtility.AddCursorRect(previewRect, currentCursor);
        }

        private PreviewPopupOptions m_Option;
        void SetPreviewAvatarOption(object obj)
        {
            m_Option = (PreviewPopupOptions)obj;
            if (m_Option == PreviewPopupOptions.Auto)
            {
                SetPreview(null);
            }
            else if (m_Option == PreviewPopupOptions.DefaultModel)
            {
                SetPreview(GetHumanoidFallback());
            }
            else if (m_Option == PreviewPopupOptions.Other)
            {
                ObjectSelector.get.Show(null, typeof(GameObject), null, false);
                ObjectSelector.get.objectSelectorID = m_ModelSelectorId;
            }
        }

        void SetPreview(GameObject gameObject)
        {
            AvatarPreviewSelection.SetPreview(animationClipType, gameObject);

            Object.DestroyImmediate(m_PreviewInstance);
            InitInstance(m_SourceScenePreviewAnimator, m_SourcePreviewMotion);

            if (m_OnAvatarChangeFunc != null)
                m_OnAvatarChangeFunc();
        }

        int Repeat(int t, int length)
        {
            // Have to do double modulo in order to work for negative numbers.
            // This is quicker than a branch to test for negative number.
            return ((t % length) + length) % length;
        }
    } // class AvatarPreview
} // namespace UnityEditor
