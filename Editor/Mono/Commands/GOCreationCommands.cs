// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Video;

namespace UnityEditor
{
    static class GOCreationCommands
    {
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
                StageUtility.PlaceGameObjectInCurrentStage(go); // may change parent
            }

            // Only at this point do we know the actual parent of the object and can mopdify its name accordingly.
            GameObjectUtility.EnsureUniqueNameForSibling(go);
            Undo.SetCurrentGroupName("Create " + go.name);

            EditorWindow.FocusWindowIfItsOpen<SceneHierarchyWindow>();
            Selection.activeGameObject = go;
        }

        [MenuItem("GameObject/Create Empty %#n", priority = 0)]
        static void CreateEmpty(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            Place(ObjectFactory.CreateGameObject("GameObject"), parent);
        }

        [MenuItem("GameObject/Create Empty Child &#n", priority = 0)]
        static void CreateEmptyChild(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            if (parent == null)
            {
                var activeGO = Selection.activeGameObject;
                if (activeGO != null && !EditorUtility.IsPersistent(activeGO))
                    parent = activeGO;
            }

            var go = ObjectFactory.CreateGameObject("GameObject");
            Place(go, parent);
        }

        static void CreateAndPlacePrimitive(PrimitiveType type, GameObject parent)
        {
            var primitive = ObjectFactory.CreatePrimitive(type);
            primitive.name = type.ToString();
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
            var go = ObjectFactory.CreateGameObject("New Sprite", typeof(SpriteRenderer));
            var sprite = Selection.activeObject as Sprite;
            if (sprite == null)
            {
                var texture = Selection.activeObject as Texture2D;
                if (texture)
                {
                    string path = AssetDatabase.GetAssetPath(texture);
                    sprite = AssetDatabase.LoadAllAssetsAtPath(path)
                        .OfType<Sprite>()
                        .FirstOrDefault();
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
            var go = ObjectFactory.CreateGameObject("Directional Light", typeof(Light));

            go.GetComponent<Light>().type = LightType.Directional;
            go.GetComponent<Light>().intensity = 1f;
            go.GetComponent<Transform>().SetLocalEulerAngles(new Vector3(50, -30, 0), RotationOrder.OrderZXY);

            Place(go, parent);
        }

        [MenuItem("GameObject/Light/Point Light", priority = 2)]
        static void CreatePointLight(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            var go = ObjectFactory.CreateGameObject("Point Light", typeof(Light));

            go.GetComponent<Light>().type = LightType.Point;

            Place(go, parent);
        }

        [MenuItem("GameObject/Light/Spotlight", priority = 3)]
        static void CreateSpotLight(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            var go = ObjectFactory.CreateGameObject("Spot Light", typeof(Light));

            go.GetComponent<Light>().type = LightType.Spot;
            go.GetComponent<Transform>().SetLocalEulerAngles(new Vector3(90, 0, 0), RotationOrder.OrderZXY);

            Place(go, parent);
        }

        [MenuItem("GameObject/Light/Area Light", priority = 4)]
        static void CreateAreaLight(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            var go = ObjectFactory.CreateGameObject("Area Light", typeof(Light));

            go.GetComponent<Light>().type = LightType.Rectangle;
            go.GetComponent<Light>().shadows = LightShadows.Soft;
            go.GetComponent<Transform>().SetLocalEulerAngles(new Vector3(90, 0, 0), RotationOrder.OrderZXY);

            Place(go, parent);
        }

        [MenuItem("GameObject/Light/Reflection Probe", priority = 20)]
        static void CreateReflectionProbe(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            Place(ObjectFactory.CreateGameObject("Reflection Probe", typeof(ReflectionProbe)), parent);
        }

        [MenuItem("GameObject/Light/Light Probe Group", priority = 21)]
        static void CreateLightProbeGroup(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            Place(ObjectFactory.CreateGameObject("Light Probe Group", typeof(LightProbeGroup)), parent);
        }

        [MenuItem("GameObject/Audio/Audio Source", priority = 1)]
        static void CreateAudioSource(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            Place(ObjectFactory.CreateGameObject("Audio Source", typeof(AudioSource)), parent);
        }

        [MenuItem("GameObject/Audio/Audio Reverb Zone", priority = 2)]
        static void CreateAudioReverbZone(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            Place(ObjectFactory.CreateGameObject("Audio Reverb Zone", typeof(AudioReverbZone)), parent);
        }

        [MenuItem("GameObject/Video/Video Player", priority = 1)]
        static void CreateVideoPlayer(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            Place(ObjectFactory.CreateGameObject("Video Player", typeof(VideoPlayer)), parent);
        }

        [MenuItem("GameObject/Effects/Particle System", priority = 1)]
        static void CreateParticleSystem(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            var go = ObjectFactory.CreateGameObject("Particle System", typeof(ParticleSystem));

            go.GetComponent<Transform>().SetLocalEulerAngles(new Vector3(-90, 0, 0), RotationOrder.OrderZXY);
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.materials = new Material[]
            {
                Material.GetDefaultParticleMaterial(),
                    null
            };
            Place(go, parent);
        }

        [MenuItem("GameObject/Effects/Particle System Force Field", priority = 2)]
        static void CreateParticleSystemForceField(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            Place(ObjectFactory.CreateGameObject("Particle System Force Field", typeof(ParticleSystemForceField)), parent);
        }

        [MenuItem("GameObject/Effects/Trail", priority = 3)]
        static void CreateTrail(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            var go = ObjectFactory.CreateGameObject("Trail", typeof(TrailRenderer));
            go.GetComponent<TrailRenderer>().material = Material.GetDefaultLineMaterial();
            Place(go, parent);
        }

        [MenuItem("GameObject/Effects/Line", priority = 4)]
        static void CreateLine(MenuCommand menuCommand)
        {
            var parent = menuCommand.context as GameObject;
            var go = ObjectFactory.CreateGameObject("Line", typeof(LineRenderer));
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
            Place(ObjectFactory.CreateGameObject("Camera", typeof(Camera), typeof(AudioListener)), parent);
        }
    }
}
