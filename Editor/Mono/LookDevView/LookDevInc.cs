// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.AnimatedValues;
using System.Collections.Generic;
using UnityEditorInternal;

namespace UnityEditor
{
    internal enum LookDevPropertyType
    {
        Int = 0,
        Float,
    }

    internal enum LookDevProperty
    {
        ExposureValue = 0,
        HDRI,
        ShadingMode,
        EnvRotation,
        LoDIndex,
        Count
    }

    internal enum LookDevMode
    {
        Single1 = 0,
        Single2,
        SideBySide,
        Split,
        Zone,
        Count
    }

    internal enum LookDevEditionContext
    {
        Left = 0,
        Right = 1,
        None = 2
    }

    enum LookDevOperationType
    {
        None = 0,
        GizmoTranslation,
        GizmoRotationZone1,
        GizmoRotationZone2,
        GizmoAll,   // Used in shader to highlight all gizmo parts
        BlendFactor,
        RotateLight,
        RotateEnvironment
    }

    [Serializable]
    internal class GizmoInfo
    {
        [SerializeField]
        private Vector2 m_Point1;
        [SerializeField]
        private Vector2 m_Point2;
        [SerializeField]
        private Vector2 m_Center = new Vector2(0.0f, 0.0f);
        [SerializeField]
        private float m_Angle = 0.0f;
        [SerializeField]
        private float m_Length = 0.2f;
        [SerializeField]
        private Vector4 m_Plane;
        [SerializeField]
        private Vector4 m_PlaneOrtho;

        public GizmoInfo()
        {
            Update(m_Center, m_Length, m_Angle);
        }

        public Vector2 point1
        {
            get { return m_Point1; }
        }

        public Vector2 point2
        {
            get { return m_Point2; }
        }

        public Vector2 center
        {
            get { return m_Center; }
        }

        public float angle
        {
            get { return m_Angle; }
        }

        public float length
        {
            get { return m_Length; }
        }

        public Vector4 plane
        {
            get { return m_Plane; }
        }

        public Vector4 planeOrtho
        {
            get { return m_PlaneOrtho; }
        }

        private Vector4 Get2DPlane(Vector2 firstPoint, float angle)
        {
            Vector4 result = new Vector4();
            angle = angle % (2.0f * (float)Math.PI);
            Vector2 secondPoint = new Vector2(firstPoint.x + Mathf.Sin(angle), firstPoint.y + Mathf.Cos(angle));
            Vector2 diff = secondPoint - firstPoint;
            if (Mathf.Abs(diff.x) < 1e-5)
            {
                result.Set(-1.0f, 0.0f, firstPoint.x, 0.0f);
                float sign = Mathf.Cos(angle) > 0.0f ? 1.0f : -1.0f;
                result *= sign;
            }
            else
            {
                float slope = diff.y / diff.x;
                result.Set(-slope, 1.0f, -(firstPoint.y - slope * firstPoint.x), 0.0f);
            }

            if (angle > Mathf.PI)
                result = -result;

            float length = Mathf.Sqrt(result.x * result.x + result.y * result.y);
            result = result / length;
            return result;
        }

        public void Update(Vector2 point1, Vector2 point2)
        {
            m_Point1 = point1;
            m_Point2 = point2;
            m_Center = (point1 + point2) * 0.5f;
            m_Length = (point2 - point1).magnitude * 0.5f;

            Vector3 verticalPlane = Get2DPlane(m_Center, 0.0f);
            float side = Vector3.Dot(new Vector3(point1.x, point1.y, 1.0f), verticalPlane);
            m_Angle = (Mathf.Deg2Rad * Vector2.Angle(new Vector2(0.0f, 1.0f), (point1 - point2).normalized));
            if (side > 0.0f)
                m_Angle = 2.0f * Mathf.PI - m_Angle;

            m_Plane = Get2DPlane(m_Center, m_Angle);
            m_PlaneOrtho = Get2DPlane(m_Center, m_Angle + 0.5f * (float)Mathf.PI);
        }

        public void Update(Vector2 center, float length, float angle)
        {
            m_Center = center;
            m_Length = length;
            m_Angle = angle;

            m_Plane = Get2DPlane(m_Center, m_Angle);
            m_PlaneOrtho = Get2DPlane(m_Center, m_Angle + 0.5f * (float)Mathf.PI);

            Vector2 dir = new Vector2(m_PlaneOrtho.x, m_PlaneOrtho.y);
            m_Point1 = m_Center + dir * m_Length;
            m_Point2 = m_Center - dir * m_Length;
        }
    }

    [Serializable]
    internal class LookDevPropertyInfo
    {
        [SerializeField]
        private bool m_Linked = false;
        [SerializeField]
        private LookDevPropertyType m_PropertyType;

        public LookDevPropertyType propertyType
        {
            get { return m_PropertyType; }
        }
        public bool linked
        {
            get { return m_Linked; }
            set { m_Linked = value; }
        }

        public LookDevPropertyInfo(LookDevPropertyType type)
        {
            m_PropertyType = type;
        }
    }

    [Serializable]
    internal class ShadowInfo
    {
        // Setup default position to be on the sun in the default HDRI.
        // This is important as the defaultHDRI don't call the set brightest spot funciton on first call.
        [SerializeField]
        private float m_Latitude = 60.0f; // [-90..90]
        [SerializeField]
        private float m_Longitude = 299.0f; // [0..360]
        [SerializeField]
        private float m_ShadowIntensity = 1.0f;
        [SerializeField]
        private Color m_ShadowColor = Color.white;

        public float shadowIntensity
        {
            get { return m_ShadowIntensity; }
            set { m_ShadowIntensity = value; }
        }

        public Color shadowColor
        {
            get { return m_ShadowColor; }
            set { m_ShadowColor = value; }
        }

        public float latitude
        {
            get { return m_Latitude; }
            set { m_Latitude = value; ConformLatLong(); }
        }

        public float longitude
        {
            get { return m_Longitude; }
            set { m_Longitude = value; ConformLatLong(); }
        }

        private void ConformLatLong()
        {
            // Clamp latitude to [-90..90]
            if (m_Latitude < -90.0f)
                m_Latitude = -90.0f;
            if (m_Latitude > 89.0f)
                m_Latitude = 89.0f;

            // wrap longitude around
            m_Longitude = m_Longitude % 360.0f;
            if (m_Longitude < 0.0)
                m_Longitude = 360.0f + m_Longitude;
        }
    }

    [Serializable]
    internal class CubemapInfo
    {
        const float kDefaultShadowIntensity = 0.3f;

        public void SetCubemapShadowInfo(CubemapInfo newCubemapShadowInfo)
        {
            cubemapShadowInfo = newCubemapShadowInfo;
            shadowInfo.shadowIntensity = newCubemapShadowInfo == this ? kDefaultShadowIntensity : 1.0f;
            shadowInfo.shadowColor = Color.white;
        }

        public void ResetEnvInfos()
        {
            angleOffset = 0.0f;
        }

        public Cubemap cubemap;
        public CubemapInfo cubemapShadowInfo;
        public float angleOffset = 0.0f;
        public SphericalHarmonicsL2 ambientProbe;
        public ShadowInfo shadowInfo = new ShadowInfo();

        // Dedicated to serialization workaround
        // We can't serialize CubemapInfo inside a CubemapInfo, so before serializing we will 'flatten' the shadow cubemap in a new list and save index into this list.
        // THis also allow to manage case of sahdow cubemap without matching HDRI in the main list.
        public int serialIndexMain;
        public int serialIndexShadow;

        [NonSerialized]
        public bool alreadyComputed; // this is not serialized because SH are not serialized so we need to compute them again after deserialization
    }

    internal class LookDevResources
    {
        static public SphericalHarmonicsL2  m_ZeroAmbientProbe;
        static public Material              m_SkyboxMaterial = null;
        static public Material              m_GBufferPatchMaterial = null;
        static public Material              m_DrawBallsMaterial = null;
        static public Mesh                  m_ScreenQuadMesh = null;
        static public Material              m_LookDevCompositing = null;
        static public Material              m_DeferredOverlayMaterial = null;
        static public Cubemap               m_DefaultHDRI = null;
        static public Material              m_LookDevCubeToLatlong = null;
        static public RenderTexture         m_SelectionTexture = null;
        static public RenderTexture         m_BrightestPointRT = null;
        static public Texture2D             m_BrightestPointTexture = null;

        static public void Initialize()
        {
            m_ZeroAmbientProbe.Clear();

            // For some reason, a few frames after LoadRenderDoc reset the gfx device, the pointers turn null. Is there a better way to handle that?
            if (m_SkyboxMaterial == null)
                m_SkyboxMaterial = new Material(Shader.Find("Skybox/Cubemap"));

            if (m_ScreenQuadMesh == null)
            {
                // Draw a full screen quad with GBuffer patch material
                m_ScreenQuadMesh = new Mesh();
                m_ScreenQuadMesh.vertices = new Vector3[]
                {
                    // Note: Invert Z or not should not have influence here.
                    new Vector3(-1, -1, 0),
                    new Vector3(1, 1, 0),
                    new Vector3(1, -1, 0),
                    new Vector3(-1, 1, 0)
                };

                m_ScreenQuadMesh.triangles = new int[]
                {
                    0, 1, 2, 1, 0, 3
                };
            }

            // Material can be null if we switch API device or when we init the first time. We can safely re-allocate everything if this is null
            if (m_GBufferPatchMaterial == null)
            {
                m_GBufferPatchMaterial = new Material(EditorGUIUtility.LoadRequired("LookDevView/GBufferWhitePatch.shader") as Shader);
                m_DrawBallsMaterial = new Material(EditorGUIUtility.LoadRequired("LookDevView/GBufferBalls.shader") as Shader);
            }

            if (m_LookDevCompositing == null)
                m_LookDevCompositing = new Material(EditorGUIUtility.LoadRequired("LookDevView/LookDevCompositing.shader") as Shader);

            if (m_DeferredOverlayMaterial == null)
                m_DeferredOverlayMaterial = EditorGUIUtility.LoadRequired("SceneView/SceneViewDeferredMaterial.mat") as Material;

            if (m_DefaultHDRI == null)
            {
                m_DefaultHDRI = EditorGUIUtility.Load("LookDevView/DefaultHDRI.exr") as Cubemap;
                if (m_DefaultHDRI == null)
                {
                    m_DefaultHDRI = EditorGUIUtility.Load("LookDevView/DefaultHDRI.asset") as Cubemap;
                }
            }

            if (m_LookDevCubeToLatlong == null)
            {
                m_LookDevCubeToLatlong = new Material(EditorGUIUtility.LoadRequired("LookDevView/LookDevCubeToLatlong.shader") as Shader);
            }

            /*
            // Debug code to remove
            bool tutu = false;
            if (tutu)
            {
                Shader devShader = Shader.Find("Custom/DevShader2") as Shader;
                if (devShader != null)
                    m_LookDevCubeToLatlong = new Material(devShader);
            }
             */


            if (m_SelectionTexture == null)
                m_SelectionTexture = new RenderTexture((int)LookDevEnvironmentWindow.m_HDRIWidth, (int)LookDevEnvironmentWindow.m_latLongHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);

            if (m_BrightestPointRT == null)
                m_BrightestPointRT = new RenderTexture((int)LookDevEnvironmentWindow.m_HDRIWidth, (int)LookDevEnvironmentWindow.m_latLongHeight, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Default);

            if (m_BrightestPointTexture == null)
                m_BrightestPointTexture = new Texture2D((int)LookDevEnvironmentWindow.m_HDRIWidth, (int)LookDevEnvironmentWindow.m_latLongHeight, TextureFormat.RGBAHalf, false);
        }

        static public void Cleanup()
        {
            m_SkyboxMaterial = null;

            if (m_LookDevCompositing)
            {
                UnityEngine.Object.DestroyImmediate(m_LookDevCompositing);
                m_LookDevCompositing = null;
            }
        }

        // Find brightest spot of the cubemap
        static public void UpdateShadowInfoWithBrightestSpot(CubemapInfo cubemapInfo)
        {
            m_LookDevCubeToLatlong.SetTexture("_MainTex", cubemapInfo.cubemap);
            m_LookDevCubeToLatlong.SetVector("_WindowParams", new Vector4(10000, -1000.0f, 2, 0.0f)); // Neutral value to not clip
            m_LookDevCubeToLatlong.SetVector("_CubeToLatLongParams", new Vector4(Mathf.Deg2Rad * cubemapInfo.angleOffset, 0.5f, 1.0f, 3.0f)); // We use LOD 3 to take a region rather than a single pixel in the map
            m_LookDevCubeToLatlong.SetPass(0);

            int width = (int)LookDevEnvironmentWindow.m_HDRIWidth;
            int height = (int)LookDevEnvironmentWindow.m_latLongHeight;

            // Convert cubemap to a 2D LatLong to read on CPU
            Graphics.Blit(cubemapInfo.cubemap, m_BrightestPointRT, m_LookDevCubeToLatlong);
            m_BrightestPointTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
            m_BrightestPointTexture.Apply();

            // CPU read back
            // From Doc: The returned array is a flattened 2D array, where pixels are laid out left to right, bottom to top (i.e. row after row)
            Color[] color = m_BrightestPointTexture.GetPixels();

            float maxLum = 0.0f;
            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    Vector3 rgb = new Vector3(color[y * width + x].r, color[y * width + x].g, color[y * width + x].b);

                    float lum = rgb.x * 0.2126729f + rgb.y * 0.7151522f + rgb.z * 0.0721750f;

                    if (maxLum < lum)
                    {
                        Vector2 vec = LookDevEnvironmentWindow.PositionToLatLong(new Vector2(((float)x / (float)(width - 1)) * 2.0f - 1.0f, ((float)y / (float)(height - 1)) * 2.0f - 1.0f));
                        cubemapInfo.shadowInfo.latitude = vec.x;
                        cubemapInfo.shadowInfo.longitude = vec.y - cubemapInfo.angleOffset;

                        maxLum = lum;
                    }
                }
            }
        }
    }
}
