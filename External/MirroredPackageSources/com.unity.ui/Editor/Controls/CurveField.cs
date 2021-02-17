using System;
using UnityEngine;
using UnityEditorInternal;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;
using Object = UnityEngine.Object;

namespace UnityEditor.UIElements
{
    /// <summary>
    /// Makes a field for editing an <see cref="AnimationCurve"/>.
    /// </summary>
    public class CurveField : BaseField<AnimationCurve>
    {
        /// <summary>
        /// Instantiates a <see cref="CurveField"/> using the data read from a UXML file.
        /// </summary>
        public new class UxmlFactory : UxmlFactory<CurveField, UxmlTraits> {}

        /// <summary>
        /// Defines <see cref="UxmlTraits"/> for the <see cref="CurveField"/>.
        /// </summary>
        public new class UxmlTraits : BaseField<AnimationCurve>.UxmlTraits {}

        /// <summary>
        /// USS class name of elements of this type.
        /// </summary>
        public new static readonly string ussClassName = "unity-curve-field";
        /// <summary>
        /// USS class name of labels in elements of this type.
        /// </summary>
        public new static readonly string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// USS class name of input elements in elements of this type.
        /// </summary>
        public new static readonly string inputUssClassName = ussClassName + "__input";

        /// <summary>
        /// USS class name of content elements in elements of this type.
        /// </summary>
        public static readonly string contentUssClassName = ussClassName + "__content";
        /// <summary>
        /// USS class name of border elements in elements of this type.
        /// </summary>
        public static readonly string borderUssClassName = ussClassName + "__border";

        private static CustomStyleProperty<Color> s_CurveColorProperty = new CustomStyleProperty<Color>("--unity-curve-color");
        /// <summary>
        /// Optional rectangle that the curve is restrained within. If the range width or height is < 0 then CurveField computes an automatic range, which encompasses the whole curve.
        /// </summary>
        public Rect ranges { get; set; }

        Color m_CurveColor = Color.green;
        private Color curveColor
        {
            get { return m_CurveColor; }
        }

        private bool m_ValueNull;
        private bool m_TextureDirty;
        private Texture2D m_Texture; // The curve rasterized in a texture

        /// <summary>
        /// Render mode of CurveFields
        /// </summary>
        public enum RenderMode
        {
            /// <summary>
            /// Renders the curve with a generated texture, like with Unity’s Immediate Mode GUI system (IMGUI).
            /// </summary>
            Texture,
            /// <summary>
            /// Renders the curve with an anti-aliased mesh.
            /// </summary>
            Mesh,
            /// <summary>
            /// Renders the curve with the default mode. Currently Texture.
            /// </summary>
            Default = Texture
        }

        RenderMode m_RenderMode = RenderMode.Default;

        /// <summary>
        /// The RenderMode of CurveField. The default is RenderMode.Default.
        /// </summary>
        public RenderMode renderMode
        {
            get { return m_RenderMode; }
            set
            {
                if (m_RenderMode != value)
                {
                    m_RenderMode = value;

                    if (renderMode == RenderMode.Mesh)
                    {
                        m_ContentParent = new VisualElement();
                        m_ContentParent.AddToClassList(contentUssClassName);
                        visualInput.Insert(0, m_ContentParent);

                        m_Content = new CurveFieldContent();
                        m_ZeroIndicator = new VisualElement() {style = {height = 1, backgroundColor = Color.black}};
                        m_ContentParent.Add(m_ZeroIndicator);
                        m_ContentParent.Add(m_Content);
                        m_Content.StretchToParentSize();
                        m_ZeroIndicator.StretchToParentWidth();
                    }
                    else
                    {
                        m_ZeroIndicator.RemoveFromHierarchy();
                        m_ZeroIndicator = null;

                        m_Content.RemoveFromHierarchy();
                        m_Content = null;

                        m_ContentParent.RemoveFromHierarchy();
                        m_ContentParent = null;
                    }

                    m_TextureDirty = true;
                }
            }
        }

        internal static AnimationCurve CopyCurve(AnimationCurve other)
        {
            AnimationCurve curveCopy = new AnimationCurve();
            curveCopy.keys = other.keys;
            curveCopy.preWrapMode = other.preWrapMode;
            curveCopy.postWrapMode = other.postWrapMode;
            return curveCopy;
        }

        public override AnimationCurve value
        {
            get
            {
                if (m_ValueNull) return null;

                return CopyCurve(rawValue);
            }
            set
            {
                //I need to have total ownership of the curve, I won't be able to know if it is changed outside. so I'm duplicating it.
                if (value != null || !m_ValueNull) // let's not reinitialize an initialized curve
                {
                    if (panel != null)
                    {
                        using (ChangeEvent<AnimationCurve> evt = ChangeEvent<AnimationCurve>.GetPooled(rawValue, value))
                        {
                            evt.target = this;
                            SetValueWithoutNotify(value);
                            SendEvent(evt);
                        }
                    }
                    else
                    {
                        SetValueWithoutNotify(value);
                    }
                }
            }
        }
        CurveFieldContent m_Content;
        VisualElement m_ZeroIndicator;
        VisualElement m_ContentParent;

        public CurveField()
            : this(null) {}

        public CurveField(string label)
            : base(label, null)
        {
            AddToClassList(ussClassName);
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);

            ranges = Rect.zero;

            rawValue = new AnimationCurve(new Keyframe[0]);

            VisualElement borderElement = new VisualElement() { name = "unity-border", pickingMode = PickingMode.Ignore };
            borderElement.AddToClassList(borderUssClassName);
            visualInput.Add(borderElement);

            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);

            visualInput.generateVisualContent += OnGenerateVisualContent;
        }

        void OnDetach()
        {
            if (m_Mesh != null)
                Object.DestroyImmediate(m_Mesh);
            if (m_Texture != null)
                Object.DestroyImmediate(m_Texture);
            m_Mesh = null;
            m_Texture = null;
            m_TextureDirty = true;
        }

        public override void SetValueWithoutNotify(AnimationCurve newValue)
        {
            m_ValueNull = newValue == null;
            if (newValue != null)
            {
                rawValue.keys = newValue.keys;
                rawValue.preWrapMode = newValue.preWrapMode;
                rawValue.postWrapMode = newValue.postWrapMode;
            }
            else
            {
                rawValue.keys = new Keyframe[0];
                rawValue.preWrapMode = WrapMode.Once;
                rawValue.postWrapMode = WrapMode.Once;
            }
            m_TextureDirty = true;
            if (CurveEditorWindow.visible && Object.ReferenceEquals(CurveEditorWindow.curve, rawValue))
            {
                CurveEditorWindow.curve = rawValue;
                CurveEditorWindow.instance.Repaint();
            }

            visualInput.IncrementVersion(VersionChangeType.Repaint);
            m_Content?.IncrementVersion(VersionChangeType.Repaint);
        }

        private void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            Color oldColor = m_CurveColor;
            Color colorValue = Color.clear;
            if (e.customStyle.TryGetValue(s_CurveColorProperty, out colorValue))
                m_CurveColor = colorValue;

            if (m_CurveColor != oldColor && renderMode == RenderMode.Texture)
            {
                // The mesh texture is updated at each repaint, the standard texture should however be regenerated
                m_TextureDirty = true;
            }
        }

        void ShowCurveEditor()
        {
            if (!enabledInHierarchy)
                return;

            CurveEditorSettings settings = new CurveEditorSettings();
            if (rawValue == null)
                rawValue = new AnimationCurve();
            CurveEditorWindow.instance.Show(OnCurveChanged, settings);
            CurveEditorWindow.curve = rawValue;

            CurveEditorWindow.color = curveColor;
        }

        protected override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            if (evt == null)
            {
                return;
            }

            var showCurveEditor = false;
            KeyDownEvent kde = (evt as KeyDownEvent);
            if (kde != null)
            {
                if ((kde.keyCode == KeyCode.Space) ||
                    (kde.keyCode == KeyCode.KeypadEnter) ||
                    (kde.keyCode == KeyCode.Return))
                {
                    showCurveEditor = true;
                }
            }
            else if ((evt as PointerDownEvent)?.button == (int)MouseButton.LeftMouse)
            {
                var mde = (PointerDownEvent)evt;
                if (visualInput.ContainsPoint(visualInput.WorldToLocal(mde.position)))
                {
                    showCurveEditor = true;
                }
            }

            if (showCurveEditor)
                ShowCurveEditor();
            else if (evt.eventTypeId == DetachFromPanelEvent.TypeId())
                OnDetach();
            if (evt.eventTypeId == GeometryChangedEvent.TypeId())
                m_TextureDirty = true;
        }

        void OnCurveChanged(AnimationCurve curve)
        {
            CurveEditorWindow.curve = rawValue;
            value = rawValue;
        }

        internal override void OnViewDataReady()
        {
            base.OnViewDataReady();
            m_TextureDirty = true;
        }

        // Must be the same with AACurveField.shader
        const float k_EdgeWidth = 2;
        const float k_MinEdgeWidth = 1.75f;
        const float k_HalfWidth = k_EdgeWidth * 0.5f;
        const float k_VertexHalfWidth = k_HalfWidth + 1;

        const int k_HorizontalCurveResolution = 256;

        void FillCurveData()
        {
            AnimationCurve curve = value;

            if (m_Mesh == null)
            {
                m_Mesh = new Mesh();
                m_Mesh.hideFlags = HideFlags.HideAndDontSave;

                m_Content.SetMesh(m_Mesh);
            }

            if (curve.keys.Length < 2)
                return;
            Vector3[] vertices = m_Mesh.vertices;
            Vector3[] normals = m_Mesh.normals;
            if (vertices == null || vertices.Length != k_HorizontalCurveResolution * 2)
            {
                vertices = new Vector3[k_HorizontalCurveResolution * 2];
                normals = new Vector3[k_HorizontalCurveResolution * 2];
            }

            float startTime = curve.keys[0].time;
            float endTime = curve.keys[curve.keys.Length - 1].time;
            float duration = endTime - startTime;

            float minValue = Mathf.Infinity;
            float maxValue = -Mathf.Infinity;

            float[] timeCache = new float[k_HorizontalCurveResolution];
            int keyCount = curve.keys.Length;
            int noKeySampleCount = k_HorizontalCurveResolution - keyCount;

            timeCache[0] = curve.keys[0].time;

            int usedSamples = 1;
            for (int k = 1; k < keyCount; ++k)
            {
                float sliceStartTime = timeCache[usedSamples - 1];
                float sliceEndTime = curve.keys[k].time;
                float sliceDuration = sliceEndTime - sliceStartTime;
                int sliceSampleCount = Mathf.FloorToInt((float)noKeySampleCount * sliceDuration / duration);
                if (k == keyCount - 1)
                {
                    sliceSampleCount = k_HorizontalCurveResolution - usedSamples - 1;
                }

                for (int i = 1; i < sliceSampleCount + 1; ++i)
                {
                    float time = sliceStartTime + i * sliceDuration / (sliceSampleCount + 1);
                    timeCache[usedSamples + i - 1] = time;
                }

                timeCache[usedSamples + sliceSampleCount] = curve.keys[k].time;
                usedSamples += sliceSampleCount + 1;
            }

            float[] valueCache = new float[k_HorizontalCurveResolution];

            for (int i = 0; i < k_HorizontalCurveResolution; ++i)
            {
                float ct = timeCache[i];

                float currentValue = curve.Evaluate(ct);

                if (currentValue > maxValue)
                {
                    maxValue = currentValue;
                }
                if (currentValue < minValue)
                {
                    minValue = currentValue;
                }

                valueCache[i] = currentValue;
            }

            Vector3 scale = new Vector3(m_Content.layout.width, m_Content.layout.height);

            var yStartValue = (!Mathf.Approximately(minValue, maxValue)) ? 1.0f : 0.5f;
            vertices[0] = vertices[1] = Vector3.Scale(new Vector3(0, yStartValue - Mathf.InverseLerp(minValue, maxValue, valueCache[0]), 0), scale);

            Vector3 secondPoint = Vector3.Scale(new Vector3(1.0f / k_HorizontalCurveResolution, yStartValue - Mathf.InverseLerp(minValue, maxValue, valueCache[1]), 0), scale);
            Vector3 prevDir = (secondPoint - vertices[0]).normalized;

            Vector3 norm = new Vector3(prevDir.y, -prevDir.x, 1);

            normals[0] = -norm * k_VertexHalfWidth;
            normals[1] = norm * k_VertexHalfWidth;

            Vector3 currentPoint = secondPoint;

            for (int i = 1; i < k_HorizontalCurveResolution - 1; ++i)
            {
                vertices[i * 2] = vertices[i * 2 + 1] = currentPoint;

                Vector3 nextPoint = Vector3.Scale(new Vector3(Mathf.InverseLerp(startTime, endTime, timeCache[i + 1]), yStartValue - Mathf.InverseLerp(minValue, maxValue, valueCache[i + 1]), 0), scale);

                Vector3 nextDir = (nextPoint - currentPoint).normalized;
                Vector3 dir = (prevDir + nextDir).normalized;
                norm = new Vector3(dir.y, -dir.x, 1);
                normals[i * 2] = -norm * k_VertexHalfWidth;
                normals[i * 2 + 1] = norm * k_VertexHalfWidth;

                currentPoint = nextPoint;
                prevDir = nextDir;
            }

            vertices[(k_HorizontalCurveResolution - 1) * 2] = vertices[(k_HorizontalCurveResolution - 1) * 2 + 1] = currentPoint;

            norm = new Vector3(prevDir.y, -prevDir.x, 1);
            normals[(k_HorizontalCurveResolution - 1) * 2] = -norm * k_VertexHalfWidth;
            normals[(k_HorizontalCurveResolution - 1) * 2 + 1] = norm * k_VertexHalfWidth;

            m_Mesh.vertices = vertices;
            m_Mesh.normals = normals;

            //fill triangle indices as it is a triangle strip
            int[] indices = new int[(k_HorizontalCurveResolution * 2 - 2) * 3];

            for (int i = 0; i < k_HorizontalCurveResolution * 2 - 2; ++i)
            {
                if ((i % 2) == 0)
                {
                    indices[i * 3] = i;
                    indices[i * 3 + 1] = i + 1;
                    indices[i * 3 + 2] = i + 2;
                }
                else
                {
                    indices[i * 3] = i + 1;
                    indices[i * 3 + 1] = i;
                    indices[i * 3 + 2] = i + 2;
                }
            }

            m_Mesh.triangles = indices;

            if (Mathf.Approximately(minValue, maxValue))
            {
                m_ZeroIndicator.style.top = m_Content.layout.height * Mathf.InverseLerp(-1, 1, minValue);
            }
            else
            {
                m_ZeroIndicator.style.top = m_Content.layout.height * (yStartValue - Mathf.InverseLerp(minValue, maxValue, 0));
            }
        }

        void SetupMeshRepaint()
        {
            if (m_TextureDirty || m_Mesh == null)
            {
                m_TextureDirty = false;
                m_Texture = null;
                FillCurveData();
            }
            m_Content.curveColor = curveColor;
        }

        void SetupStandardRepaint()
        {
            if (!m_TextureDirty) return;

            m_TextureDirty = false;

            int previewWidth = (int)visualInput.layout.width;
            int previewHeight = (int)visualInput.layout.height;

            // The default range is (0,0,-1,-1), see AnimationCurvePreviewCache.cpp
            // This will mimic the IMGUI curve since the range will be calculated by the CurvePreview if the range is at the default value...
            Rect rangeRect = new Rect(0, 0, -1, -1);

            // We assign the ranges if different than the Rect() default value
            if (ranges.width > 0 && ranges.height > 0)
            {
                rangeRect = ranges;
            }

            if (previewHeight > 1 && previewWidth > 1)
            {
                if (!m_ValueNull)
                {
                    m_Texture = AnimationCurvePreviewCache.GenerateCurvePreview(
                        previewWidth,
                        previewHeight,
                        rangeRect,
                        rawValue,
                        curveColor,
                        m_Texture);
                }
                else
                {
                    m_Texture = null;
                }
            }
        }

        Mesh m_Mesh = null;

        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            if (renderMode == RenderMode.Mesh)
            {
                SetupMeshRepaint();
            }
            else
            {
                SetupStandardRepaint();
                if (m_Texture != null)
                {
                    var rectParams = MeshGenerationContextUtils.RectangleParams.MakeTextured(
                        new Rect(0, 0, m_Texture.width, m_Texture.height), new Rect(0, 0, 1, 1), m_Texture, ScaleMode.StretchToFill, panel.contextType);
                    MeshGenerationContextUtils.Rectangle(mgc, rectParams);
                }
            }
        }

        protected override void UpdateMixedValueContent()
        {
            if (showMixedValue)
            {
                visualInput.Add(mixedValueLabel);
                m_ContentParent?.RemoveFromHierarchy();
                visualInput.generateVisualContent -= OnGenerateVisualContent;
            }
            else
            {
                visualInput.Add(m_ContentParent);
                visualInput.generateVisualContent += OnGenerateVisualContent;
                mixedValueLabel.RemoveFromHierarchy();
            }
        }

        class CurveFieldContent : ImmediateModeElement
        {
            Material m_Mat;
            Mesh m_Mesh;

            public Color curveColor { get; set; }

            public void SetMesh(Mesh mesh)
            {
                m_Mesh = mesh;
            }

            public CurveFieldContent()
            {
                pickingMode = PickingMode.Ignore;
            }

            protected override void ExecuteDefaultAction(EventBase evt)
            {
                base.ExecuteDefaultAction(evt);

                if (evt?.eventTypeId == DetachFromPanelEvent.TypeId())
                    OnDetach();
            }

            void OnDetach()
            {
                Object.DestroyImmediate(m_Mat);
                m_Mat = null;
            }

            protected override void ImmediateRepaint()
            {
                if (m_Mesh != null)
                {
                    if (m_Mat == null)
                    {
                        m_Mat = new Material(EditorGUIUtility.LoadRequired("Shaders/UIElements/AACurveField.shader") as Shader);
                        m_Mat.hideFlags = HideFlags.HideAndDontSave;
                    }

                    DrawMesh();
                }
            }

            void DrawMesh()
            {
                float scale = worldTransform.MultiplyVector(Vector3.one).x;

                float realWidth = k_EdgeWidth;
                if (realWidth * scale < k_MinEdgeWidth)
                {
                    realWidth = k_MinEdgeWidth / scale;
                }

                Color finalColor = (QualitySettings.activeColorSpace == ColorSpace.Linear) ? curveColor.gamma : curveColor;
                finalColor *= UIElementsUtility.editorPlayModeTintColor;

                // Send the view zoom factor so that the antialias width do not grow when zooming in.
                m_Mat.SetFloat("_ZoomFactor", scale * realWidth / CurveField.k_EdgeWidth * EditorGUIUtility.pixelsPerPoint);

                // Send the view zoom correction so that the vertex shader can scale the edge triangles when below m_MinWidth.
                m_Mat.SetFloat("_ZoomCorrection", realWidth / CurveField.k_EdgeWidth);

                m_Mat.SetColor("_Color", finalColor);
                m_Mat.SetPass(0);

                Graphics.DrawMeshNow(m_Mesh, Matrix4x4.identity);
            }
        }
    }
}
