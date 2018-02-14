// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;
using Unity.Collections;
using NativeArrayUnsafeUtility = Unity.Collections.LowLevel.Unsafe.NativeArrayUnsafeUtility;

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
            Rectangle
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
            public Vector3      direction;
            public LinearColor  color;
            public LinearColor  indirectColor;
            // shadow
            public float        penumbraWidthRadian;
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
        }

        // This struct must be kept in sync with its counterpart in LightDataGI.h
        [StructLayout(LayoutKind.Sequential)]
        [UnityEngine.Scripting.UsedByNativeCode]
        public struct LightDataGI
        {
            // light id
            public int          instanceID;
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
            // area light parameters (interpretation depends on the type, can affect shadow softness)
            public float        shape0;
            public float        shape1;
            // types
            public LightType    type;
            public LightMode    mode;
            public byte         shadow;
            public FalloffType  falloff;

            public void Init(ref DirectionalLight light)
            {
                instanceID     = light.instanceID;
                color          = light.color;
                indirectColor  = light.indirectColor;
                orientation.SetLookRotation(light.direction, Vector3.up);
                position       = Vector3.zero;
                range          = 0.0f;
                coneAngle      = 0.0f;
                innerConeAngle = 0.0f;
                shape0         = light.penumbraWidthRadian;
                shape1         = 0.0f;
                type           = LightType.Directional;
                mode           = light.mode;
                shadow         = (byte)(light.shadow ? 1 : 0);
                falloff        = FalloffType.Undefined;
            }

            public void Init(ref PointLight light)
            {
                instanceID     = light.instanceID;
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

            public void Init(ref SpotLight light)
            {
                instanceID     = light.instanceID;
                color          = light.color;
                indirectColor  = light.indirectColor;
                orientation    = light.orientation;
                position       = light.position;
                range          = light.range;
                coneAngle      = light.coneAngle;
                innerConeAngle = light.innerConeAngle;
                shape0         = light.sphereRadius;
                shape1         = 0.0f;
                type           = LightType.Spot;
                mode           = light.mode;
                shadow         = (byte)(light.shadow ? 1 : 0);
                falloff        = light.falloff;
            }

            public void Init(ref RectangleLight light)
            {
                instanceID     = light.instanceID;
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
                falloff        = FalloffType.Undefined;
            }

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
            }

            public static void Extract(Light l, ref DirectionalLight dir)
            {
                dir.instanceID          = l.GetInstanceID();
                dir.mode                = Extract(l.lightmapBakeType);
                dir.shadow              = l.shadows != LightShadows.None;
                dir.direction           = l.transform.forward;
                dir.color               = LinearColor.Convert(l.color, l.intensity);
                dir.indirectColor       = ExtractIndirect(l);
                dir.penumbraWidthRadian = l.shadows == LightShadows.Soft ? (Mathf.Deg2Rad * l.shadowAngle) : 0.0f;
            }

            public static void Extract(Light l, ref PointLight point)
            {
                point.instanceID    = l.GetInstanceID();
                point.mode          = Extract(l.lightmapBakeType);
                point.shadow        = l.shadows != LightShadows.None;
                point.position      = l.transform.position;
                point.color         = LinearColor.Convert(l.color, l.intensity);
                point.indirectColor = ExtractIndirect(l);
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
                spot.color         = LinearColor.Convert(l.color, l.intensity);
                spot.indirectColor = ExtractIndirect(l);
                spot.range         = l.range;
                spot.sphereRadius  = l.shadows == LightShadows.Soft ? l.shadowRadius : 0.0f;
                spot.coneAngle     = l.spotAngle * Mathf.Deg2Rad;
                spot.innerConeAngle = ExtractInnerCone(l);
                spot.falloff       = FalloffType.Legacy;
            }

            public static void Extract(Light l, ref RectangleLight rect)
            {
                rect.instanceID     = l.GetInstanceID();
                rect.mode           = Extract(l.lightmapBakeType);
                rect.shadow         = l.shadows != LightShadows.None;
                rect.position       = l.transform.position;
                rect.orientation    = l.transform.rotation;
                rect.color          = LinearColor.Convert(l.color, l.intensity);
                rect.indirectColor  = ExtractIndirect(l);
                rect.range          = l.range;
                rect.width          = l.areaSize.x;
                rect.height         = l.areaSize.y;
            }
        }

        public static class Lightmapping
        {
            public delegate void RequestLightsDelegate(Light[] requests, NativeArray<LightDataGI> lightsOutput);
            public static void SetDelegate(RequestLightsDelegate del)   { s_RequestLightsDelegate = del != null ? del : s_DefaultDelegate; }
            public static void ResetDelegate()                          { s_RequestLightsDelegate = s_DefaultDelegate; }

            [UnityEngine.Scripting.UsedByNativeCode]
            internal unsafe static void RequestLights(Light[] lights, System.IntPtr outLightsPtr, int outLightsCount)
            {
                var outLights = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<LightDataGI>((void*)outLightsPtr, outLightsCount, Allocator.None);
                s_RequestLightsDelegate(lights, outLights);
            }


            private static readonly RequestLightsDelegate s_DefaultDelegate   = (Light[] requests, NativeArray<LightDataGI> lightsOutput) =>
                {
                    // get all lights in the scene
                    DirectionalLight dir   = new DirectionalLight();
                    PointLight       point = new PointLight();
                    SpotLight        spot  = new SpotLight();
                    RectangleLight   rect  = new RectangleLight();
                    LightDataGI      ld    = new LightDataGI();
                    for (int i = 0; i < requests.Length; i++)
                    {
                        Light l = requests[i];
                        switch (l.type)
                        {
                            case UnityEngine.LightType.Directional: LightmapperUtils.Extract(l, ref dir); ld.Init(ref dir); break;
                            case UnityEngine.LightType.Point: LightmapperUtils.Extract(l, ref point); ld.Init(ref point); break;
                            case UnityEngine.LightType.Spot: LightmapperUtils.Extract(l, ref spot); ld.Init(ref spot); break;
                            case UnityEngine.LightType.Area: LightmapperUtils.Extract(l, ref rect); ld.Init(ref rect); break;
                            default: ld.InitNoBake(l.GetInstanceID()); break;
                        }
                        lightsOutput[i] = ld;
                    }
                };
            private static RequestLightsDelegate s_RequestLightsDelegate = s_DefaultDelegate;
        }
    }
}
