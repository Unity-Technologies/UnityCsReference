// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Video;

namespace UnityEditor
{
    static class GOCreationCommands
    {
        internal static GameObject CreateGameObject(GameObject parent, string name, params Type[] types)
        {
            return ObjectFactory.CreateGameObject(GameObjectUtility.GetUniqueNameForSibling(parent != null ? parent.transform : null, name), types);
        }

        internal static void Place(GameObject go, GameObject parent)
        {
            if (parent != null)
            {
                var transform = go.transform;
                Undo.SetTransformParent(transform, parent.transform, "Reparenting");
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;
                go.layer = parent.layer;

                if (parent.GetComponent<RectTransform>())
                    ObjectFactory.AddComponent<RectTransform>(go);
            }
            else
            {
                SceneView.PlaceGameObjectInFrontOfSceneView(go);
            }
            EditorWindow.FocusWindowIfItsOpen<SceneHierarchyWindow>();
            Selection.activeGameObject = go;
        }

        [MenuItem("GameObject/Create Empty %#n", priority = 0)]
        static void CreateEmpty(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            Place(CreateGameObject(parent, "GameObject"), parent);
        }

        [MenuItem("GameObject/Create Empty Child &#n", priority = 0)]
        static void CreateEmptyChild(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            if (parent == null)
                parent = Selection.activeGameObject;
            var go = CreateGameObject(parent, "GameObject");
            Place(go, parent);
        }

        static void CreateAndPlacePrimitive(PrimitiveType type, GameObject parent)
        {
            // make sure to get the unique name before the GameObject is created
            // or GetUniqueNameForSibling will always end up with (1) in empty scene
            string uniqueName = GameObjectUtility.GetUniqueNameForSibling(parent != null ? parent.transform : null, type.ToString());
            var primitive = ObjectFactory.CreatePrimitive(type);
            primitive.name = uniqueName;
            Place(primitive, parent);
        }

        [MenuItem("GameObject/3D Object/Cube", priority = 1)]
        static void CreateCube(MenuCommand menuCommand)
        {
            CreateAndPlacePrimitive(PrimitiveType.Cube, menuCommand.context as GameObject);
        }

        [MenuItem("GameObject/3D Object/Sphere", priority = 2)]
        static void CreateSphere(MenuCommand menuCommand)
        {
            CreateAndPlacePrimitive(PrimitiveType.Sphere, menuCommand.context as GameObject);
        }

        [MenuItem("GameObject/3D Object/Capsule", priority = 3)]
        static void CreateCapsule(MenuCommand menuCommand)
        {
            CreateAndPlacePrimitive(PrimitiveType.Capsule, menuCommand.context as GameObject);
        }

        [MenuItem("GameObject/3D Object/Cylinder", priority = 4)]
        static void CreateCylinder(MenuCommand menuCommand)
        {
            CreateAndPlacePrimitive(PrimitiveType.Cylinder, menuCommand.context as GameObject);
        }

        [MenuItem("GameObject/3D Object/Plane", priority = 5)]
        static void CreatePlane(MenuCommand menuCommand)
        {
            CreateAndPlacePrimitive(PrimitiveType.Plane, menuCommand.context as GameObject);
        }

        [MenuItem("GameObject/3D Object/Quad", priority = 6)]
        static void CreateQuad(MenuCommand menuCommand)
        {
            CreateAndPlacePrimitive(PrimitiveType.Quad, menuCommand.context as GameObject);
        }

        [MenuItem("GameObject/2D Object/Sprite", priority = 1)]
        static void CreateSprite(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            var go = CreateGameObject(parent, "New Sprite", typeof(SpriteRenderer));
            var sprite = Selection.activeObject as Sprite;
            if (sprite == null)
            {
                var texture = Selection.activeObject as Texture2D;
                if (texture)
                {
                    string path = AssetDatabase.GetAssetPath(texture);
                    sprite = AssetDatabase.LoadAllAssetsAtPath(path)
                        .OfType<Sprite>()
                        .First();
                    if (sprite == null)
                    {
                        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                        if (importer != null && importer.textureType != TextureImporterType.Sprite)
                        {
                            EditorUtility.DisplayDialog(
                                "Sprite could not be assigned!",
                                "Can not assign a Sprite to the new SpriteRenderer because the selected Texture is not configured to generate Sprites.",
                                "OK");
                        }
                    }
                }
            }
            if (sprite != null)
            {
                go.GetComponent<SpriteRenderer>().sprite = sprite;
            }
            else
            {
                // TODO: assign a default sprite
            }
            Place(go, parent);
        }

        [MenuItem("GameObject/Light/Directional Light", priority = 1)]
        static void CreateDirectionalLight(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            var go = CreateGameObject(null, "Directional Light", typeof(Light));

            go.GetComponent<Light>().type = LightType.Directional;
            go.GetComponent<Light>().intensity = 1f;
            go.GetComponent<Transform>().SetLocalEulerAngles(new Vector3(50, -30, 0), RotationOrder.OrderZXY);

            Place(go, parent);
        }

        [MenuItem("GameObject/Light/Point Light", priority = 2)]
        static void CreatePointLight(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            var go = CreateGameObject(null, "Point Light", typeof(Light));

            go.GetComponent<Light>().type = LightType.Point;

            Place(go, parent);
        }

        [MenuItem("GameObject/Light/Spotlight", priority = 3)]
        static void CreateSpotLight(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            var go = CreateGameObject(null, "Spot Light", typeof(Light));

            go.GetComponent<Light>().type = LightType.Spot;
            go.GetComponent<Transform>().SetLocalEulerAngles(new Vector3(90, 0, 0), RotationOrder.OrderZXY);

            Place(go, parent);
        }

        [MenuItem("GameObject/Light/Area Light", priority = 4)]
        static void CreateAreaLight(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            var go = CreateGameObject(null, "Area Light", typeof(Light));

            go.GetComponent<Light>().type = LightType.Area;
            go.GetComponent<Transform>().SetLocalEulerAngles(new Vector3(90, 0, 0), RotationOrder.OrderZXY);

            Place(go, parent);
        }

        [MenuItem("GameObject/Light/Reflection Probe", priority = 20)]
        static void CreateReflectionProbe(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            Place(CreateGameObject(parent, "Reflection Probe", typeof(ReflectionProbe)), parent);
        }

        [MenuItem("GameObject/Light/Light Probe Group", priority = 21)]
        static void CreateLightProbeGroup(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            Place(CreateGameObject(parent, "Light Probe Group", typeof(LightProbeGroup)), parent);
        }

        [MenuItem("GameObject/Audio/Audio Source", priority = 1)]
        static void CreateAudioSource(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            Place(CreateGameObject(parent, "Audio Source", typeof(AudioSource)), parent);
        }

        [MenuItem("GameObject/Audio/Audio Reverb Zone", priority = 2)]
        static void CreateAudioReverbZone(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            Place(CreateGameObject(parent, "Audio Reverb Zone", typeof(AudioReverbZone)), parent);
        }

        [MenuItem("GameObject/Video/Video Player", priority = 1)]
        static void CreateVideoPlayer(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            Place(CreateGameObject(parent, "Video Player", typeof(VideoPlayer)), parent);
        }

        [MenuItem("GameObject/Effects/Particle System", priority = 1)]
        static void CreateParticleSystem(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            var go = CreateGameObject(parent, "Particle System", typeof(ParticleSystem));

            go.GetComponent<Transform>().SetLocalEulerAngles(new Vector3(-90, 0, 0), RotationOrder.OrderZXY);
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.materials = new Material[]
            {
                Material.GetDefaultParticleMaterial(),
                    null
            };
            Place(go, parent);
        }

        [MenuItem("GameObject/Effects/Trail", priority = 2)]
        static void CreateTrail(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            var go = CreateGameObject(parent, "Trail", typeof(TrailRenderer));
            go.GetComponent<TrailRenderer>().material = Material.GetDefaultLineMaterial();
            Place(go, parent);
        }

        [MenuItem("GameObject/Effects/Line", priority = 3)]
        static void CreateLine(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            var go = CreateGameObject(parent, "Line", typeof(LineRenderer));
            var line = go.GetComponent<LineRenderer>();
            line.material = Material.GetDefaultLineMaterial();
            line.widthMultiplier = 0.1f;
            line.useWorldSpace = false;
            Place(go, parent);
        }

        [MenuItem("GameObject/Camera", priority = 11)]
        static void CreateCamera(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            Place(CreateGameObject(parent, "Camera", typeof(Camera), typeof(FlareLayer), typeof(AudioListener)), parent);
        }
    }
}
