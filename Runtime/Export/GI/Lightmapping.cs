// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;
using Unity.Collections;
using NativeArrayUnsafeUtility = Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility;
using Unity.Collections.LowLevel.Unsafe;
using RequiredByNativeCodeAttribute = UnityEngine.Scripting.RequiredByNativeCodeAttribute;

namespace UnityEngine
{
    namespace Experimental.GlobalIllumination
    {
        // This LightType enum contains the lights known to the baking backends.
        // It is separate from the UnityEngine's light type, as scriptable pipelines
        // can define their own lights and types, which need not map to the Unity types.
        // Having a separate enum for the global illumination backend also allows to only expose
        // light types in the baking backends that can then be used by specific SRP implementations.
        public enum LightType : byte
        {
            Directional,
            Point,
            Spot,
            Rectangle,
            Disc,
            SpotPyramidShape,
            SpotBoxShape
        }

        public enum LightMode : byte
        {
            Realtime,
            Mixed,
            Baked,
            Unknown
        }

        public enum FalloffType : byte
        {
            InverseSquared,
            InverseSquaredNoRangeAttenuation,
            Linear,
            Legacy,
            Undefined
        }

        public enum AngularFalloffType : byte
        {
            LUT,
            AnalyticAndInnerAngle
        }

        // The linear color struct contains 4 values. 3 values are for specifying normalized RGB values, in other words they're between 0 and 1.
        // The fourth value is an positive unbounded intensity value. Color information in this struct must always be in linear space.
        // When converting a Unity color that's either in linear or gamma space, the Convert function will make sure to account for all relevant
        // state so that the resulting color is in linear space, before being passed down to the baking backends. No further gamma handling is
        // performed in the backends.
        [StructLayout(LayoutKind.Sequential)]
        public struct LinearColor
        {
            public float red
            {
                get { return m_red; }
                set
                {
                    if (value < 0.0f || value > 1.0f)
                        throw new System.ArgumentOutOfRangeException("Red color (" + value + ") must be in range [0;1].");
                    m_red = value;
                }
            }
            public float green
            {
                get { return m_green; }
                set
                {
                    if (value < 0.0f || value > 1.0f)
                        throw new System.ArgumentOutOfRangeException("Green color (" + value + ") must be in range [0;1].");
                    m_green = value;
                }
            }
            public float blue
            {
                get { return m_blue; }
                set
                {
                    if (value < 0.0f || value > 1.0f)
                        throw new System.ArgumentOutOfRangeException("Blue color (" + value + ") must be in range [0;1].");
                    m_blue = value;
                }
            }
            public float intensity
            {
                get { return m_intensity; }
                set
                {
                    if (value < 0.0f)
                        throw new System.ArgumentOutOfRangeException("Intensity (" + value + ") must be positive.");
                    m_intensity = value;
                }
            }
            public static LinearColor Convert(UnityEngine.Color color, float intensity)
            {
                var lc        = GraphicsSettings.lightsUseLinearIntensity ? color.linear.RGBMultiplied(intensity) : color.RGBMultiplied(intensity).linear;
                float mcc     = lc.maxColorComponent;

                if (mcc <= 0.0f)
                    return LinearColor.Black();

                float mcc_rcp = 1.0f / lc.maxColorComponent;
                LinearColor c;
                c.m_red         = lc.r * mcc_rcp;
                c.m_green       = lc.g * mcc_rcp;
                c.m_blue        = lc.b * mcc_rcp;
                c.m_intensity   = mcc;
                return c;
            }

            public static LinearColor Black() { LinearColor c; c.m_red = c.m_green = c.m_blue = c.m_intensity = 0.0f; return c; }

            private float m_red;
            private float m_green;
            private float m_blue;
            private float m_intensity;
        }


        // Each light type known to the baking backends along with its parameters gets its own struct.
        // This way it is clear what type supports what parameters. Helper functions are provided
        // to convert Unity lights to these types, but a render pipeline may choose to map their lights
        // directly to these structs. They are then used to initialize the LightDataGI struct, which is the actual
        // interop struct.
        public struct DirectionalLight
        {
            // light id
            public int          instanceID;
            // shadow
            public bool         shadow;
            // light mode
            public LightMode    mode;
            // light
            public Vector3      position;
            public Quaternion   orientation;
            public LinearColor  color;
            public LinearColor  indirectColor;
            // shadow
            public float        penumbraWidthRadian;

            [System.Obsolete("Directional lights support cookies now. In order to position the cookie projection in the world, a position and full orientation are necessary. Use the position and orientation members instead of the direction parameter.", true)]
            public Vector3 direction;
        }
        public struct PointLight
        {
            // light id
            public int          instanceID;
            // shadow
            public bool         shadow;
            // light mode
            public LightMode    mode;
            // light
            public Vector3      position;
            public LinearColor  color;
            public LinearColor  indirectColor;
            public float        range;
            public float        sphereRadius;
            public FalloffType  falloff;
        }
        public struct SpotLight
        {
            // light id
            public int          instanceID;
            // shadow
            public bool         shadow;
            // light mode
            public LightMode    mode;
            // light
            public Vector3      position;
            public Quaternion   orientation;
            public LinearColor  color;
            public LinearColor  indirectColor;
            public float        range;
            public float        sphereRadius;
            public float        coneAngle;
            public float        innerConeAngle;
            public FalloffType  falloff;
            public AngularFalloffType angularFalloff;
        }
        public struct RectangleLight
        {
            // light id
            public int          instanceID;
            // shadow
            public bool         shadow;
            // light mode
            public LightMode    mode;
            // light
            public Vector3      position;
            public Quaternion   orientation;
            public LinearColor  color;
            public LinearColor  indirectColor;
            public float        range;
            public float        width;
            public float        height;
            public FalloffType  falloff;
        }
        public struct DiscLight
        {
            // light id
            public int          instanceID;
            // shadow
            public bool         shadow;
            // light mode
            public LightMode    mode;
            // light
            public Vector3      position;
            public Quaternion   orientation;
            public LinearColor  color;
            public LinearColor  indirectColor;
            public float        range;
            public float        radius;
            public FalloffType  falloff;
        }
        public struct SpotLightBoxShape
        {
            // light id
            public int          instanceID;
            // shadow
            public bool         shadow;
            // light mode
            public LightMode    mode;
            // light
            public Vector3      position;
            public Quaternion   orientation;
            public LinearColor  color;
            public LinearColor  indirectColor;
            public float        range;
            //box dimensions
            public float        width;
            public float        height;
        }
        public struct SpotLightPyramidShape
        {
            // light id
            public int          instanceID;
            // shadow
            public bool         shadow;
            // light mode
            public LightMode    mode;
            // light
            public Vector3      position;
            public Quaternion   orientation;
            public LinearColor  color;
            public LinearColor  indirectColor;
            public float        range;
            // pyramid parameters
            public float        angle;
            public float        aspectRatio;
            public FalloffType  falloff;
        }

        public struct Cookie
        {
            public int     instanceID;
            public float   scale;
            public Vector2 sizes; // directional lights only

            public static Cookie Defaults() { Cookie c; c.instanceID = 0; c.scale = 1.0f; c.sizes = new Vector2(1.0f, 1.0f); return c; }
        }

        // This struct must be kept in sync with its counterpart in LightDataGI.h
        [StructLayout(LayoutKind.Sequential)]
        [UnityEngine.Scripting.UsedByNativeCode]
        public struct LightDataGI
        {
            // light id
            public int          instanceID;
            // cookie id
            public int          cookieID;
            public float        cookieScale;
            // shared
            public LinearColor  color;
            public LinearColor  indirectColor;
            public Quaternion   orientation;
            public Vector3      position;
            // non-dir light only
            public float        range;
            // spot light only
            public float        coneAngle;
            public float        innerConeAngle;
            // area light and box light parameters
            //for area light (interpretation depends on the type, can affect shadow softness) it is width and height
            //for box light it is sizeX and sizeY
            public float        shape0;
            public float        shape1;
            // types
            public LightType    type;
            public LightMode    mode;
            public byte         shadow;
            public FalloffType  falloff;

            public void Init(ref DirectionalLight light, ref Cookie cookie)
            {
                instanceID     = light.instanceID;
                cookieID       = cookie.instanceID;
                cookieScale    = cookie.scale;
                color          = light.color;
                indirectColor  = light.indirectColor;
                orientation    = light.orientation;
                position       = light.position;
                range          = 0.0f;
                coneAngle      = cookie.sizes.x;
                innerConeAngle = cookie.sizes.y;
                shape0         = light.penumbraWidthRadian;
                shape1         = 0.0f;
                type           = LightType.Directional;
                mode           = light.mode;
                shadow         = (byte)(light.shadow ? 1 : 0);
                falloff        = FalloffType.Undefined;
            }

            public void Init(ref PointLight light, ref Cookie cookie)
            {
                instanceID     = light.instanceID;
                cookieID       = cookie.instanceID;
                cookieScale    = cookie.scale;
                color          = light.color;
                indirectColor  = light.indirectColor;
                orientation    = Quaternion.identity;
                position       = light.position;
                range          = light.range;
                coneAngle      = 0.0f;
                innerConeAngle = 0.0f;
                shape0         = light.sphereRadius;
                shape1         = 0.0f;
                type           = LightType.Point;
                mode           = light.mode;
                shadow         = (byte)(light.shadow ? 1 : 0);
                falloff        = light.falloff;
            }

            public void Init(ref SpotLight light, ref Cookie cookie)
            {
                instanceID     = light.instanceID;
                cookieID       = cookie.instanceID;
                cookieScale    = cookie.scale;
                color          = light.color;
                indirectColor  = light.indirectColor;
                orientation    = light.orientation;
                position       = light.position;
                range          = light.range;
                coneAngle      = light.coneAngle;
                innerConeAngle = light.innerConeAngle;
                shape0         = light.sphereRadius;
                shape1         = (float)light.angularFalloff;
                type           = LightType.Spot;
                mode           = light.mode;
                shadow         = (byte)(light.shadow ? 1 : 0);
                falloff        = light.falloff;
            }

            public void Init(ref RectangleLight light, ref Cookie cookie)
            {
                instanceID     = light.instanceID;
                cookieID       = cookie.instanceID;
                cookieScale    = cookie.scale;
                color          = light.color;
                indirectColor  = light.indirectColor;
                orientation    = light.orientation;
                position       = light.position;
                range          = light.range;
                coneAngle      = 0.0f;
                innerConeAngle = 0.0f;
                shape0         = light.width;
                shape1         = light.height;
                type           = LightType.Rectangle;
                mode           = light.mode;
                shadow         = (byte)(light.shadow ? 1 : 0);
                falloff        = light.falloff;
            }

            public void Init(ref DiscLight light, ref Cookie cookie)
            {
                instanceID     = light.instanceID;
                cookieID       = cookie.instanceID;
                cookieScale    = cookie.scale;
                color          = light.color;
                indirectColor  = light.indirectColor;
                orientation    = light.orientation;
                position       = light.position;
                range          = light.range;
                coneAngle      = 0.0f;
                innerConeAngle = 0.0f;
                shape0         = light.radius;
                shape1         = 0.0f;
                type           = LightType.Disc;
                mode           = light.mode;
                shadow         = (byte)(light.shadow ? 1 : 0);
                falloff        = light.falloff;
            }

            public void Init(ref SpotLightBoxShape light, ref Cookie cookie)
            {
                instanceID     = light.instanceID;
                cookieID       = cookie.instanceID;
                cookieScale    = cookie.scale;
                color          = light.color;
                indirectColor  = light.indirectColor;
                orientation    = light.orientation;
                position       = light.position;
                range          = light.range;
                coneAngle      = 0.0f;
                innerConeAngle = 0.0f;
                shape0         = light.width;
                shape1         = light.height;
                type           = LightType.SpotBoxShape;
                mode           = light.mode;
                shadow         = (byte)(light.shadow ? 1 : 0);
                falloff        = FalloffType.Undefined;
            }

            public void Init(ref SpotLightPyramidShape light, ref Cookie cookie)
            {
                instanceID     = light.instanceID;
                cookieID       = cookie.instanceID;
                cookieScale    = cookie.scale;
                color          = light.color;
                indirectColor  = light.indirectColor;
                orientation    = light.orientation;
                position       = light.position;
                range          = light.range;
                coneAngle      = light.angle;
                innerConeAngle = 0.0f;
                shape0         = light.aspectRatio;
                shape1         = 0.0f;
                type           = LightType.SpotPyramidShape;
                mode           = light.mode;
                shadow         = (byte)(light.shadow ? 1 : 0);
                falloff        = light.falloff;
            }

            public void Init(ref DirectionalLight light)        { Cookie cookie = Cookie.Defaults(); Init(ref light, ref cookie); }
            public void Init(ref PointLight light)              { Cookie cookie = Cookie.Defaults(); Init(ref light, ref cookie); }
            public void Init(ref SpotLight light)               { Cookie cookie = Cookie.Defaults(); Init(ref light, ref cookie); }
            public void Init(ref RectangleLight light)          { Cookie cookie = Cookie.Defaults(); Init(ref light, ref cookie); }
            public void Init(ref DiscLight light)               { Cookie cookie = Cookie.Defaults(); Init(ref light, ref cookie); }
            public void Init(ref SpotLightBoxShape light)       { Cookie cookie = Cookie.Defaults(); Init(ref light, ref cookie); }
            public void Init(ref SpotLightPyramidShape light)   { Cookie cookie = Cookie.Defaults(); Init(ref light, ref cookie); }

            public void InitNoBake(int lightInstanceID)
            {
                instanceID = lightInstanceID;
                mode       = LightMode.Unknown;
            }
        }


        public static class LightmapperUtils
        {
            public static LightMode Extract(LightmapBakeType baketype)
            {
                return baketype == LightmapBakeType.Realtime ? LightMode.Realtime : (baketype == LightmapBakeType.Mixed ? LightMode.Mixed : LightMode.Baked);
            }

            public static LinearColor ExtractIndirect(Light l)
            {
                LinearColor result = LinearColor.Convert(l.color, l.intensity * l.bounceIntensity);
                return result;
            }

            public static float ExtractInnerCone(Light l)
            {
                return 2.0f * Mathf.Atan(Mathf.Tan(l.spotAngle * 0.5f * Mathf.Deg2Rad) * (64.0f - 18.0f) / 64.0f);

                //TODO(GI) The line below would provide support for innerConeAngle with baked enlighten,
                //however this feature should come as a whole with baked lighting on all backend so it is commented for now.
                //see card : https://favro.com/organization/c564ede4ed3337f7b17986b6/0ae5422e2f78207a0998ab80?card=Uni-68012
                //return l.innerSpotAngle * Mathf.Deg2Rad;
            }

            private static Color ExtractColorTemperature(Light l)
            {
                Color cct = new Color(1.0f, 1.0f, 1.0f);
                if (l.useColorTemperature && GraphicsSettings.lightsUseLinearIntensity)
                    cct = Mathf.CorrelatedColorTemperatureToRGB(l.colorTemperature);
                return cct;
            }

            private static void ApplyColorTemperature(Color cct, ref LinearColor lightColor)
            {
                lightColor.red *= cct.r;
                lightColor.green *= cct.g;
                lightColor.blue *= cct.b;
            }

            public static void Extract(Light l, ref DirectionalLight dir)
            {
                dir.instanceID          = l.GetInstanceID();
                dir.mode                = Extract(l.lightmapBakeType);
                dir.shadow              = l.shadows != LightShadows.None;
                dir.position            = l.transform.position;
                dir.orientation         = l.transform.rotation;

                Color cct = ExtractColorTemperature(l);
                LinearColor directColor = LinearColor.Convert(l.color, l.intensity);
                LinearColor indirectColor = ExtractIndirect(l);
                ApplyColorTemperature(cct, ref directColor);
                ApplyColorTemperature(cct, ref indirectColor);
                dir.color = directColor;
                dir.indirectColor = indirectColor;
                dir.penumbraWidthRadian = l.shadows == LightShadows.Soft ? (Mathf.Deg2Rad * l.shadowAngle) : 0.0f;
            }

            public static void Extract(Light l, ref PointLight point)
            {
                point.instanceID    = l.GetInstanceID();
                point.mode          = Extract(l.lightmapBakeType);
                point.shadow        = l.shadows != LightShadows.None;
                point.position      = l.transform.position;

                Color cct = ExtractColorTemperature(l);
                LinearColor directColor = LinearColor.Convert(l.color, l.intensity);
                LinearColor indirectColor = ExtractIndirect(l);
                ApplyColorTemperature(cct, ref directColor);
                ApplyColorTemperature(cct, ref indirectColor);
                point.color         = directColor;
                point.indirectColor = indirectColor;

                point.range         = l.range;
                point.sphereRadius = l.shadows == LightShadows.Soft ? l.shadowRadius : 0.0f;
                point.falloff      = FalloffType.Legacy;
            }

            public static void Extract(Light l, ref SpotLight spot)
            {
                spot.instanceID    = l.GetInstanceID();
                spot.mode          = Extract(l.lightmapBakeType);
                spot.shadow        = l.shadows != LightShadows.None;
                spot.position      = l.transform.position;
                spot.orientation   = l.transform.rotation;

                Color cct = ExtractColorTemperature(l);
                LinearColor directColor = LinearColor.Convert(l.color, l.intensity);
                LinearColor indirectColor = ExtractIndirect(l);
                ApplyColorTemperature(cct, ref directColor);
                ApplyColorTemperature(cct, ref indirectColor);
                spot.color = directColor;
                spot.indirectColor = indirectColor;

                spot.range         = l.range;
                spot.sphereRadius  = l.shadows == LightShadows.Soft ? l.shadowRadius : 0.0f;
                spot.coneAngle      = l.spotAngle * Mathf.Deg2Rad;
                spot.innerConeAngle = ExtractInnerCone(l);
                spot.falloff        = FalloffType.Legacy;
                spot.angularFalloff = AngularFalloffType.LUT;
            }

            public static void Extract(Light l, ref RectangleLight rect)
            {
                rect.instanceID     = l.GetInstanceID();
                rect.mode           = Extract(l.lightmapBakeType);
                rect.shadow         = l.shadows != LightShadows.None;
                rect.position       = l.transform.position;
                rect.orientation    = l.transform.rotation;

                Color cct = ExtractColorTemperature(l);
                LinearColor directColor = LinearColor.Convert(l.color, l.intensity);
                LinearColor indirectColor = ExtractIndirect(l);
                ApplyColorTemperature(cct, ref directColor);
                ApplyColorTemperature(cct, ref indirectColor);
                rect.color = directColor;
                rect.indirectColor = indirectColor;

                rect.range          = l.range;
                rect.width          = l.areaSize.x;
                rect.height         = l.areaSize.y;
                rect.falloff        = FalloffType.Legacy;
            }

            public static void Extract(Light l, ref DiscLight disc)
            {
                disc.instanceID     = l.GetInstanceID();
                disc.mode           = Extract(l.lightmapBakeType);
                disc.shadow         = l.shadows != LightShadows.None;
                disc.position       = l.transform.position;
                disc.orientation    = l.transform.rotation;

                Color cct = ExtractColorTemperature(l);
                LinearColor directColor = LinearColor.Convert(l.color, l.intensity);
                LinearColor indirectColor = ExtractIndirect(l);
                ApplyColorTemperature(cct, ref directColor);
                ApplyColorTemperature(cct, ref indirectColor);
                disc.color = directColor;
                disc.indirectColor = indirectColor;

                disc.range          = l.range;
                disc.radius         = l.areaSize.x;
                disc.falloff        = FalloffType.Legacy;
            }

            /*
             * Builtin Unity does not support the following light types, so no extraction utility function will be provided.
             * The following definitions are only here so it doesn't look like they have been forgotten.
             *
            public static void Extract(Light l, ref SpotLightBoxShape box)
            {
                Debug.Assert(false, "Builtin Unity does not support the LightType.SpotBoxShape.");
            }

            public static void Extract(Light l, ref SpotLightPyramidShape pyramid)
            {
                Debug.Assert(false, "Builtin Unity does not support the LightType.SpotPyramidShape.");
            }
            */

            public static void Extract(Light l, out Cookie cookie)
            {
                cookie.instanceID = l.cookie ? l.cookie.GetInstanceID() : 0;
                cookie.scale      = 1.0f;
                cookie.sizes      = (l.type == UnityEngine.LightType.Directional && l.cookie) ? new Vector2(l.cookieSize, l.cookieSize) : new Vector2(1.0f, 1.0f);
            }
        }

        public static class Lightmapping
        {
            public delegate void RequestLightsDelegate(Light[] requests, NativeArray<LightDataGI> lightsOutput);

            [RequiredByNativeCode]
            public static void SetDelegate(RequestLightsDelegate del)   { s_RequestLightsDelegate = del != null ? del : s_DefaultDelegate; }

            [RequiredByNativeCode]
            public static RequestLightsDelegate GetDelegate()           { return s_RequestLightsDelegate; }

            [RequiredByNativeCode]
            public static void ResetDelegate()                          { s_RequestLightsDelegate = s_DefaultDelegate; }

            [RequiredByNativeCode]
            internal unsafe static void RequestLights(Light[] lights, System.IntPtr outLightsPtr, int outLightsCount, AtomicSafetyHandle safetyHandle)
            {
                var outLights = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<LightDataGI>((void*)outLightsPtr, outLightsCount, Allocator.None);
                NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref outLights, safetyHandle);
                s_RequestLightsDelegate(lights, outLights);
            }

            [RequiredByNativeCode]
            private static readonly RequestLightsDelegate s_DefaultDelegate   = (Light[] requests, NativeArray<LightDataGI> lightsOutput) =>
            {
                // get all lights in the scene
                DirectionalLight    dir    = new DirectionalLight();
                PointLight          point  = new PointLight();
                SpotLight           spot   = new SpotLight();
                RectangleLight      rect   = new RectangleLight();
                DiscLight           disc   = new DiscLight();
                Cookie              cookie = new Cookie();
                LightDataGI         ld     = new LightDataGI();
                for (int i = 0; i < requests.Length; i++)
                {
                    Light l = requests[i];
                    switch (l.type)
                    {
                        case UnityEngine.LightType.Directional: LightmapperUtils.Extract(l, ref dir);   LightmapperUtils.Extract(l, out cookie); ld.Init(ref dir,   ref cookie); break;
                        case UnityEngine.LightType.Point:       LightmapperUtils.Extract(l, ref point); LightmapperUtils.Extract(l, out cookie); ld.Init(ref point, ref cookie); break;
                        case UnityEngine.LightType.Spot:        LightmapperUtils.Extract(l, ref spot);  LightmapperUtils.Extract(l, out cookie); ld.Init(ref spot,  ref cookie); break;
                        case UnityEngine.LightType.Rectangle:   LightmapperUtils.Extract(l, ref rect);  LightmapperUtils.Extract(l, out cookie); ld.Init(ref rect,  ref cookie); break;
                        case UnityEngine.LightType.Disc:        LightmapperUtils.Extract(l, ref disc);  LightmapperUtils.Extract(l, out cookie); ld.Init(ref disc,  ref cookie); break;
                        default: ld.InitNoBake(l.GetInstanceID()); break;
                    }
                    lightsOutput[i] = ld;
                }
            };
            [RequiredByNativeCode]
            private static RequestLightsDelegate s_RequestLightsDelegate = s_DefaultDelegate;
        }
    }
}
