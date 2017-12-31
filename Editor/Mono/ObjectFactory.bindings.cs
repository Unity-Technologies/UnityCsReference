// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Bindings;
using Object = UnityEngine.Object;

namespace UnityEditor
{
    [NativeHeader("Editor/Src/ObjectFactory.h")]
    public static class ObjectFactory
    {
        [FreeFunction(ThrowsException = true)]
        static extern Object CreateDefaultInstance([NotNull] Type type);

        [FreeFunction(ThrowsException = true)]
        static extern Component AddDefaultComponent([NotNull] GameObject gameObject, [NotNull] Type type);

        [FreeFunction]
        static extern GameObject CreateDefaultGameObject(string name);

        static void CheckTypeValidity(Type type)
        {
            if (type.IsAbstract)
            {
                throw new ArgumentException("Abstract types can't be used in the ObjectFactory : " + type.FullName);
            }
            if (Attribute.GetCustomAttribute(type, typeof(ExcludeFromObjectFactoryAttribute)) != null)
            {
                throw new ArgumentException("The type " + type.FullName + " is not supported by the ObjectFactory.");
            }
        }

        public static T CreateInstance<T>() where T : Object
        {
            return (T)CreateInstance(typeof(T));
        }

        public static Object CreateInstance(Type type)
        {
            CheckTypeValidity(type);
            if (type == typeof(GameObject))
            {
                throw new ArgumentException("GameObject type must be created using ObjectFactory.CreateGameObject instead : " + type.FullName);
            }
            if (type.IsSubclassOf(typeof(Component)))
            {
                throw new ArgumentException("Component type must be created using ObjectFactory.AddComponent instead : " + type.FullName);
            }
            if (type.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null) != null)
            {
                throw new ArgumentException(type.FullName + " constructor is not accessible which prevent this type from being used in ObjectFactory.");
            }
            var obj = CreateDefaultInstance(type);
            return obj;
        }

        public static T AddComponent<T>(GameObject gameObject) where T : Component
        {
            return (T)AddComponent(gameObject, typeof(T));
        }

        public static Component AddComponent(GameObject gameObject, Type type)
        {
            CheckTypeValidity(type);
            if (!type.IsSubclassOf(typeof(Component)))
            {
                throw new ArgumentException("Non-Component type must use ObjectFactory.CreateInstance instead : " + type.FullName);
            }
            return AddDefaultComponent(gameObject, type);
        }

        public static GameObject CreateGameObject(string name, params Type[] types)
        {
            var go = CreateDefaultGameObject(name);
            go.SetActive(false);
            foreach (var type in types)
            {
                AddComponent(go, type);
            }
            go.SetActive(true);
            return go;
        }

        public static GameObject CreatePrimitive(PrimitiveType type)
        {
            var go = CreateGameObject(type.ToString(), typeof(MeshFilter), typeof(MeshRenderer));
            go.SetActive(false);
            switch (type)
            {
                case PrimitiveType.Sphere:
                    go.GetComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");
                    AddComponent<SphereCollider>(go);
                    break;
                case PrimitiveType.Capsule:
                    go.GetComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("New-Capsule.fbx");
                    var capsule = AddComponent<CapsuleCollider>(go);
                    capsule.height = 2f;
                    break;
                case PrimitiveType.Cylinder:
                    go.GetComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("New-Cylinder.fbx");
                    var cylinder = AddComponent<CapsuleCollider>(go);
                    cylinder.height = 2f;
                    break;
                case PrimitiveType.Cube:
                    go.GetComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
                    AddComponent<BoxCollider>(go);
                    break;
                case PrimitiveType.Plane:
                    go.GetComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("New-Plane.fbx");
                    AddComponent<MeshCollider>(go);
                    break;
                case PrimitiveType.Quad:
                    go.GetComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
                    AddComponent<MeshCollider>(go);
                    break;
            }

            var renderer = go.GetComponent<Renderer>();
            renderer.material = Material.GetDefaultMaterial();
            //go->GetComponent<Renderer>().SetPreserveUVs(true);
            go.SetActive(true);
            return go;
        }
    }
}
