// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Scripting;

namespace Unity.Properties.Internal
{
    [VisibleToOtherModules("UnityEditor.PropertiesModule")]
    static class PropertiesInitialization
    {
        [RequiredByNativeCode(optional:false)]
        public static void InitializeProperties()
        {
            PropertyBagStore.CreatePropertyBagProvider();
            PropertyBag.Register(new ColorPropertyBag());
            PropertyBag.Register(new Vector2PropertyBag());
            PropertyBag.Register(new Vector3PropertyBag());
            PropertyBag.Register(new Vector4PropertyBag());
            PropertyBag.Register(new Vector2IntPropertyBag());
            PropertyBag.Register(new Vector3IntPropertyBag());
            PropertyBag.Register(new RectPropertyBag());
            PropertyBag.Register(new RectIntPropertyBag());
            PropertyBag.Register(new BoundsPropertyBag());
            PropertyBag.Register(new BoundsIntPropertyBag());
            PropertyBag.Register(new SystemVersionPropertyBag());
        }
    }

    class ColorPropertyBag : ContainerPropertyBag<Color>
    {
        public ColorPropertyBag()
        {
            AddProperty(new RProperty());
            AddProperty(new GProperty());
            AddProperty(new BProperty());
            AddProperty(new AProperty());
        }

        class RProperty : Property<Color, float>
        {
            public override string Name => nameof(Color.r);
            public override bool IsReadOnly => false;
            public override float GetValue(ref Color container) => container.r;
            public override void SetValue(ref Color container, float value) => container.r = value;
        }

        class GProperty : Property<Color, float>
        {
            public override string Name => nameof(Color.g);
            public override bool IsReadOnly => false;
            public override float GetValue(ref Color container) => container.g;
            public override void SetValue(ref Color container, float value) => container.g = value;
        }

        class BProperty : Property<Color, float>
        {
            public override string Name => nameof(Color.b);
            public override bool IsReadOnly => false;
            public override float GetValue(ref Color container) => container.b;
            public override void SetValue(ref Color container, float value) => container.b = value;
        }

        class AProperty : Property<Color, float>
        {
            public override string Name => nameof(Color.a);
            public override bool IsReadOnly => false;
            public override float GetValue(ref Color container) => container.a;
            public override void SetValue(ref Color container, float value) => container.a = value;
        }
    }

    class Vector2PropertyBag : ContainerPropertyBag<Vector2>
    {
        public Vector2PropertyBag()
        {
            AddProperty(new XProperty());
            AddProperty(new YProperty());
        }

        class XProperty : Property<Vector2, float>
        {
            public override string Name => nameof(Vector2.x);
            public override bool IsReadOnly => false;
            public override float GetValue(ref Vector2 container) => container.x;
            public override void SetValue(ref Vector2 container, float value) => container.x = value;
        }

        class YProperty : Property<Vector2, float>
        {
            public override string Name => nameof(Vector2.y);
            public override bool IsReadOnly => false;
            public override float GetValue(ref Vector2 container) => container.y;
            public override void SetValue(ref Vector2 container, float value) => container.y = value;
        }
    }

    class Vector3PropertyBag : ContainerPropertyBag<Vector3>
    {
        public Vector3PropertyBag()
        {
            AddProperty(new XProperty());
            AddProperty(new YProperty());
            AddProperty(new ZProperty());
        }

        class XProperty : Property<Vector3, float>
        {
            public override string Name => nameof(Vector3.x);
            public override bool IsReadOnly => false;
            public override float GetValue(ref Vector3 container) => container.x;
            public override void SetValue(ref Vector3 container, float value) => container.x = value;
        }

        class YProperty : Property<Vector3, float>
        {
            public override string Name => nameof(Vector3.y);
            public override bool IsReadOnly => false;
            public override float GetValue(ref Vector3 container) => container.y;
            public override void SetValue(ref Vector3 container, float value) => container.y = value;
        }

        class ZProperty : Property<Vector3, float>
        {
            public override string Name => nameof(Vector3.z);
            public override bool IsReadOnly => false;
            public override float GetValue(ref Vector3 container) => container.z;
            public override void SetValue(ref Vector3 container, float value) => container.z = value;
        }
    }

    class Vector4PropertyBag : ContainerPropertyBag<Vector4>
    {
        public Vector4PropertyBag()
        {
            AddProperty(new XProperty());
            AddProperty(new YProperty());
            AddProperty(new ZProperty());
            AddProperty(new WProperty());
        }

        class XProperty : Property<Vector4, float>
        {
            public override string Name => nameof(Vector4.x);
            public override bool IsReadOnly => false;
            public override float GetValue(ref Vector4 container) => container.x;
            public override void SetValue(ref Vector4 container, float value) => container.x = value;
        }

        class YProperty : Property<Vector4, float>
        {
            public override string Name => nameof(Vector4.y);
            public override bool IsReadOnly => false;
            public override float GetValue(ref Vector4 container) => container.y;
            public override void SetValue(ref Vector4 container, float value) => container.y = value;
        }

        class ZProperty : Property<Vector4, float>
        {
            public override string Name => nameof(Vector4.z);
            public override bool IsReadOnly => false;
            public override float GetValue(ref Vector4 container) => container.z;
            public override void SetValue(ref Vector4 container, float value) => container.z = value;
        }

        class WProperty : Property<Vector4, float>
        {
            public override string Name => nameof(Vector4.w);
            public override bool IsReadOnly => false;
            public override float GetValue(ref Vector4 container) => container.w;
            public override void SetValue(ref Vector4 container, float value) => container.w = value;
        }
    }

    class Vector2IntPropertyBag : ContainerPropertyBag<Vector2Int>
    {
        public Vector2IntPropertyBag()
        {
            AddProperty(new XProperty());
            AddProperty(new YProperty());
        }

        class XProperty : Property<Vector2Int, int>
        {
            public override string Name => nameof(Vector2Int.x);
            public override bool IsReadOnly => false;
            public override int GetValue(ref Vector2Int container) => container.x;
            public override void SetValue(ref Vector2Int container, int value) => container.x = value;
        }

        class YProperty : Property<Vector2Int, int>
        {
            public override string Name => nameof(Vector2Int.y);
            public override bool IsReadOnly => false;
            public override int GetValue(ref Vector2Int container) => container.y;
            public override void SetValue(ref Vector2Int container, int value) => container.y = value;
        }
    }

    class Vector3IntPropertyBag : ContainerPropertyBag<Vector3Int>
    {
        public Vector3IntPropertyBag()
        {
            AddProperty(new XProperty());
            AddProperty(new YProperty());
            AddProperty(new ZProperty());
        }

        class XProperty : Property<Vector3Int, int>
        {
            public override string Name => nameof(Vector3Int.x);
            public override bool IsReadOnly => false;
            public override int GetValue(ref Vector3Int container) => container.x;
            public override void SetValue(ref Vector3Int container, int value) => container.x = value;
        }

        class YProperty : Property<Vector3Int, int>
        {
            public override string Name => nameof(Vector3Int.y);
            public override bool IsReadOnly => false;
            public override int GetValue(ref Vector3Int container) => container.y;
            public override void SetValue(ref Vector3Int container, int value) => container.y = value;
        }

        class ZProperty : Property<Vector3Int, int>
        {
            public override string Name => nameof(Vector3Int.z);
            public override bool IsReadOnly => false;
            public override int GetValue(ref Vector3Int container) => container.z;
            public override void SetValue(ref Vector3Int container, int value) => container.z = value;
        }
    }

    class RectPropertyBag : ContainerPropertyBag<Rect>
    {
        public RectPropertyBag()
        {
            AddProperty(new XProperty());
            AddProperty(new YProperty());
            AddProperty(new WidthProperty());
            AddProperty(new HeightProperty());
        }

        class XProperty : Property<Rect, float>
        {
            public override string Name => nameof(Rect.x);
            public override bool IsReadOnly => false;
            public override float GetValue(ref Rect container) => container.x;
            public override void SetValue(ref Rect container, float value) => container.x = value;
        }

        class YProperty : Property<Rect, float>
        {
            public override string Name => nameof(Rect.y);
            public override bool IsReadOnly => false;
            public override float GetValue(ref Rect container) => container.y;
            public override void SetValue(ref Rect container, float value) => container.y = value;
        }

        class WidthProperty : Property<Rect, float>
        {
            public override string Name => nameof(Rect.width);
            public override bool IsReadOnly => false;
            public override float GetValue(ref Rect container) => container.width;
            public override void SetValue(ref Rect container, float value) => container.width = value;
        }

        class HeightProperty : Property<Rect, float>
        {
            public override string Name => nameof(Rect.height);
            public override bool IsReadOnly => false;
            public override float GetValue(ref Rect container) => container.height;
            public override void SetValue(ref Rect container, float value) => container.height = value;
        }
    }

    class RectIntPropertyBag : ContainerPropertyBag<RectInt>
    {
        public RectIntPropertyBag()
        {
            AddProperty(new XProperty());
            AddProperty(new YProperty());
            AddProperty(new WidthProperty());
            AddProperty(new HeightProperty());
        }

        class XProperty : Property<RectInt, int>
        {
            public override string Name => nameof(RectInt.x);
            public override bool IsReadOnly => false;
            public override int GetValue(ref RectInt container) => container.x;
            public override void SetValue(ref RectInt container, int value) => container.x = value;
        }

        class YProperty : Property<RectInt, int>
        {
            public override string Name => nameof(RectInt.y);
            public override bool IsReadOnly => false;
            public override int GetValue(ref RectInt container) => container.y;
            public override void SetValue(ref RectInt container, int value) => container.y = value;
        }

        class WidthProperty : Property<RectInt, int>
        {
            public override string Name => nameof(RectInt.width);
            public override bool IsReadOnly => false;
            public override int GetValue(ref RectInt container) => container.width;
            public override void SetValue(ref RectInt container, int value) => container.width = value;
        }

        class HeightProperty : Property<RectInt, int>
        {
            public override string Name => nameof(RectInt.height);
            public override bool IsReadOnly => false;
            public override int GetValue(ref RectInt container) => container.height;
            public override void SetValue(ref RectInt container, int value) => container.height = value;
        }
    }

    class BoundsPropertyBag : ContainerPropertyBag<Bounds>
    {
        public BoundsPropertyBag()
        {
            AddProperty(new CenterProperty());
            AddProperty(new ExtentsProperty());
        }

        class CenterProperty : Property<Bounds, Vector3>
        {
            public override string Name => nameof(Bounds.center);
            public override bool IsReadOnly => false;
            public override Vector3 GetValue(ref Bounds container) => container.center;
            public override void SetValue(ref Bounds container, Vector3 value) => container.center = value;
        }

        class ExtentsProperty : Property<Bounds, Vector3>
        {
            public override string Name => nameof(Bounds.extents);
            public override bool IsReadOnly => false;
            public override Vector3 GetValue(ref Bounds container) => container.extents;
            public override void SetValue(ref Bounds container, Vector3 value) => container.extents = value;
        }
    }

    class BoundsIntPropertyBag : ContainerPropertyBag<BoundsInt>
    {
        public BoundsIntPropertyBag()
        {
            AddProperty(new PositionProperty());
            AddProperty(new SizeProperty());
        }

        class PositionProperty : Property<BoundsInt, Vector3Int>
        {
            public override string Name => nameof(BoundsInt.position);
            public override bool IsReadOnly => false;
            public override Vector3Int GetValue(ref BoundsInt container) => container.position;
            public override void SetValue(ref BoundsInt container, Vector3Int value) => container.position = value;
        }

        class SizeProperty : Property<BoundsInt, Vector3Int>
        {
            public override string Name => nameof(BoundsInt.size);
            public override bool IsReadOnly => false;
            public override Vector3Int GetValue(ref BoundsInt container) => container.size;
            public override void SetValue(ref BoundsInt container, Vector3Int value) => container.size = value;
        }
    }

    class SystemVersionPropertyBag : ContainerPropertyBag<System.Version>
    {
        public SystemVersionPropertyBag()
        {
            AddProperty(new MajorProperty());
            AddProperty(new MinorProperty());
            AddProperty(new BuildProperty());
            AddProperty(new RevisionProperty());
        }

        class MajorProperty : Property<System.Version, int>
        {
            public MajorProperty()
            {
                AddAttribute(new MinAttribute(0));
            }

            public override string Name => nameof(System.Version.Major);
            public override bool IsReadOnly => true;
            public override int GetValue(ref System.Version container) => container.Major;
            public override void SetValue(ref System.Version container, int value) {}
        }

        class MinorProperty : Property<System.Version, int>
        {
            public MinorProperty()
            {
                AddAttribute(new MinAttribute(0));
            }

            public override string Name => nameof(System.Version.Minor);
            public override bool IsReadOnly => true;
            public override int GetValue(ref System.Version container) => container.Minor;
            public override void SetValue(ref System.Version container, int value) {}
        }

        class BuildProperty : Property<System.Version, int>
        {
            public BuildProperty()
            {
                AddAttribute(new MinAttribute(0));
            }

            public override string Name => nameof(System.Version.Build);
            public override bool IsReadOnly => true;
            public override int GetValue(ref System.Version container) => container.Build;
            public override void SetValue(ref System.Version container, int value) {}
        }

        class RevisionProperty : Property<System.Version, int>
        {
            public RevisionProperty()
            {
                AddAttribute(new MinAttribute(0));
            }

            public override string Name => nameof(System.Version.Revision);
            public override bool IsReadOnly => true;
            public override int GetValue(ref System.Version container) => container.Revision;
            public override void SetValue(ref System.Version container, int value) {}
        }
    }
}
