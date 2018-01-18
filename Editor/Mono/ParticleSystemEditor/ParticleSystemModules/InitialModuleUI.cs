// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Linq;

namespace UnityEditor
{
    internal class InitialModuleUI : ModuleUI
    {
        public SerializedProperty m_LengthInSec;
        public SerializedProperty m_Looping;
        public SerializedProperty m_Prewarm;
        public SerializedMinMaxCurve m_StartDelay;
        public SerializedProperty m_PlayOnAwake;
        public SerializedProperty m_SimulationSpace;
        public SerializedProperty m_CustomSimulationSpace;
        public SerializedProperty m_SimulationSpeed;
        public SerializedProperty m_UseUnscaledTime;
        public SerializedProperty m_ScalingMode;

        public SerializedMinMaxCurve m_LifeTime;
        public SerializedMinMaxCurve m_Speed;
        public SerializedMinMaxGradient m_Color;
        public SerializedProperty m_Size3D;
        public SerializedMinMaxCurve m_SizeX;
        public SerializedMinMaxCurve m_SizeY;
        public SerializedMinMaxCurve m_SizeZ;
        public SerializedProperty m_Rotation3D;
        public SerializedMinMaxCurve m_RotationX;
        public SerializedMinMaxCurve m_RotationY;
        public SerializedMinMaxCurve m_RotationZ;
        public SerializedProperty m_RandomizeRotationDirection;
        public SerializedMinMaxCurve m_GravityModifier;
        public SerializedProperty m_EmitterVelocity;
        public SerializedProperty m_MaxNumParticles;
        public SerializedProperty m_AutoRandomSeed;
        public SerializedProperty m_RandomSeed;
        public SerializedProperty m_StopAction;

        class Texts
        {
            public GUIContent duration = EditorGUIUtility.TextContent("Duration|The length of time the Particle System is emitting particles. If the system is looping, this indicates the length of one cycle.");
            public GUIContent looping = EditorGUIUtility.TextContent("Looping|If true, the emission cycle will repeat after the duration.");
            public GUIContent prewarm = EditorGUIUtility.TextContent("Prewarm|When played a prewarmed system will be in a state as if it had emitted one loop cycle. Can only be used if the system is looping.");
            public GUIContent startDelay = EditorGUIUtility.TextContent("Start Delay|Delay in seconds that this Particle System will wait before emitting particles. Cannot be used together with a prewarmed looping system.");
            public GUIContent maxParticles = EditorGUIUtility.TextContent("Max Particles|The number of particles in the system will be limited by this number. Emission will be temporarily halted if this is reached.");
            public GUIContent lifetime = EditorGUIUtility.TextContent("Start Lifetime|Start lifetime in seconds, particle will die when its lifetime reaches 0.");
            public GUIContent speed = EditorGUIUtility.TextContent("Start Speed|The start speed of particles, applied in the starting direction.");
            public GUIContent color = EditorGUIUtility.TextContent("Start Color|The start color of particles.");
            public GUIContent size3D = EditorGUIUtility.TextContent("3D Start Size|If enabled, you can control the size separately for each axis.");
            public GUIContent size = EditorGUIUtility.TextContent("Start Size|The start size of particles.");
            public GUIContent rotation3D = EditorGUIUtility.TextContent("3D Start Rotation|If enabled, you can control the rotation separately for each axis.");
            public GUIContent rotation = EditorGUIUtility.TextContent("Start Rotation|The start rotation of particles in degrees.");
            public GUIContent randomizeRotationDirection = EditorGUIUtility.TextContent("Randomize Rotation|Cause some particles to spin in the opposite direction. (Set between 0 and 1, where a higher value causes more to flip)");
            public GUIContent autoplay = EditorGUIUtility.TextContent("Play On Awake*|If enabled, the system will start playing automatically. Note that this setting is shared between all Particle Systems in the current particle effect.");
            public GUIContent gravity = EditorGUIUtility.TextContent("Gravity Modifier|Scales the gravity defined in Physics Manager");
            public GUIContent scalingMode = EditorGUIUtility.TextContent("Scaling Mode|Use the combined scale from our entire hierarchy, just this local particle node, or only apply scale to the shape module.");
            public GUIContent simulationSpace = EditorGUIUtility.TextContent("Simulation Space|Makes particle positions simulate in world, local or custom space. In local space they stay relative to their own Transform, and in custom space they are relative to the custom Transform.");
            public GUIContent customSimulationSpace = EditorGUIUtility.TextContent("Custom Simulation Space|Makes particle positions simulate relative to a custom Transform component.");
            public GUIContent simulationSpeed = EditorGUIUtility.TextContent("Simulation Speed|Scale the playback speed of the Particle System.");
            public GUIContent deltaTime = EditorGUIUtility.TextContent("Delta Time|Use either the Delta Time or the Unscaled Delta Time. Useful for playing effects whilst paused.");
            public GUIContent autoRandomSeed = EditorGUIUtility.TextContent("Auto Random Seed|Simulate differently each time the effect is played.");
            public GUIContent randomSeed = EditorGUIUtility.TextContent("Random Seed|Randomize the look of the Particle System. Using the same seed will make the Particle System play identically each time. After changing this value, restart the Particle System to see the changes, or check the Resimulate box.");
            public GUIContent emitterVelocity = EditorGUIUtility.TextContent("Emitter Velocity|When the Particle System is moving, should we use its Transform, or Rigidbody Component, to calculate its velocity?");
            public GUIContent stopAction = EditorGUIUtility.TextContent("Stop Action|When the Particle System is stopped and all particles have died, should the GameObject automatically disable/destroy itself?");
            public GUIContent x = EditorGUIUtility.TextContent("X");
            public GUIContent y = EditorGUIUtility.TextContent("Y");
            public GUIContent z = EditorGUIUtility.TextContent("Z");

            public GUIContent[] simulationSpaces = new GUIContent[]
            {
                EditorGUIUtility.TextContent("Local"),
                EditorGUIUtility.TextContent("World"),
                EditorGUIUtility.TextContent("Custom")
            };

            public GUIContent[] scalingModes = new GUIContent[]
            {
                EditorGUIUtility.TextContent("Hierarchy"),
                EditorGUIUtility.TextContent("Local"),
                EditorGUIUtility.TextContent("Shape")
            };

            public GUIContent[] stopActions = new GUIContent[]
            {
                EditorGUIUtility.TextContent("None"),
                EditorGUIUtility.TextContent("Disable"),
                EditorGUIUtility.TextContent("Destroy"),
                EditorGUIUtility.TextContent("Callback")
            };
        }
        private static Texts s_Texts;

        public InitialModuleUI(ParticleSystemUI owner, SerializedObject o, string displayName)
            : base(owner, o, "InitialModule", displayName, VisibilityState.VisibleAndFoldedOut)
        {
            Init(); // Should always be initialized since it is used by other modules (see ShapeModule)
        }

        public override float GetXAxisScalar()
        {
            return m_ParticleSystemUI.GetEmitterDuration();
        }

        protected override void Init()
        {
            // Already initialized?
            if (m_LengthInSec != null)
                return;
            if (s_Texts == null)
                s_Texts = new Texts();

            // general emitter state
            m_LengthInSec = GetProperty0("lengthInSec");
            m_Looping = GetProperty0("looping");
            m_Prewarm = GetProperty0("prewarm");
            m_StartDelay = new SerializedMinMaxCurve(this, s_Texts.startDelay, "startDelay", false, true);
            m_StartDelay.m_AllowCurves = false;
            m_PlayOnAwake = GetProperty0("playOnAwake");
            m_SimulationSpace = GetProperty0("moveWithTransform");
            m_CustomSimulationSpace = GetProperty0("moveWithCustomTransform");
            m_SimulationSpeed = GetProperty0("simulationSpeed");
            m_UseUnscaledTime = GetProperty0("useUnscaledTime");
            m_ScalingMode = GetProperty0("scalingMode");
            m_EmitterVelocity = GetProperty0("useRigidbodyForVelocity");
            m_AutoRandomSeed = GetProperty0("autoRandomSeed");
            m_RandomSeed = GetProperty0("randomSeed");
            m_StopAction = GetProperty0("stopAction");

            // module properties
            m_LifeTime = new SerializedMinMaxCurve(this, s_Texts.lifetime, "startLifetime");
            m_Speed = new SerializedMinMaxCurve(this, s_Texts.speed, "startSpeed", kUseSignedRange);
            m_Color = new SerializedMinMaxGradient(this, "startColor");
            m_Color.m_AllowRandomColor = true;
            m_Size3D = GetProperty("size3D");
            m_SizeX = new SerializedMinMaxCurve(this, s_Texts.x, "startSize");
            m_SizeY = new SerializedMinMaxCurve(this, s_Texts.y, "startSizeY", false, false, m_Size3D.boolValue);
            m_SizeZ = new SerializedMinMaxCurve(this, s_Texts.z, "startSizeZ", false, false, m_Size3D.boolValue);
            m_Rotation3D = GetProperty("rotation3D");
            m_RotationX = new SerializedMinMaxCurve(this, s_Texts.x, "startRotationX", kUseSignedRange, false, m_Rotation3D.boolValue);
            m_RotationY = new SerializedMinMaxCurve(this, s_Texts.y, "startRotationY", kUseSignedRange, false, m_Rotation3D.boolValue);
            m_RotationZ = new SerializedMinMaxCurve(this, s_Texts.z, "startRotation", kUseSignedRange);
            m_RotationX.m_RemapValue = Mathf.Rad2Deg;
            m_RotationY.m_RemapValue = Mathf.Rad2Deg;
            m_RotationZ.m_RemapValue = Mathf.Rad2Deg;
            m_RotationX.m_DefaultCurveScalar = Mathf.PI;
            m_RotationY.m_DefaultCurveScalar = Mathf.PI;
            m_RotationZ.m_DefaultCurveScalar = Mathf.PI;
            m_RandomizeRotationDirection = GetProperty("randomizeRotationDirection");
            m_GravityModifier = new SerializedMinMaxCurve(this, s_Texts.gravity, "gravityModifier", kUseSignedRange);
            m_MaxNumParticles = GetProperty("maxNumParticles");
        }

        override public void OnInspectorGUI(InitialModuleUI initial)
        {
            GUIFloat(s_Texts.duration, m_LengthInSec, "f2");

            EditorGUI.BeginChangeCheck();
            bool looping = GUIToggle(s_Texts.looping, m_Looping);
            if (EditorGUI.EndChangeCheck() && looping)
            {
                foreach (ParticleSystem ps in m_ParticleSystemUI.m_ParticleSystems)
                {
                    if (ps.time >= ps.main.duration)
                        ps.time = 0.0f;
                }
            }

            using (new EditorGUI.DisabledScope(!m_Looping.boolValue))
            {
                GUIToggle(s_Texts.prewarm, m_Prewarm);
            }

            using (new EditorGUI.DisabledScope(m_Prewarm.boolValue && m_Looping.boolValue))
            {
                GUIMinMaxCurve(s_Texts.startDelay, m_StartDelay);
            }

            GUIMinMaxCurve(s_Texts.lifetime, m_LifeTime);
            GUIMinMaxCurve(s_Texts.speed, m_Speed);

            // Size
            EditorGUI.BeginChangeCheck();
            bool size3D = GUIToggle(s_Texts.size3D, m_Size3D);
            if (EditorGUI.EndChangeCheck())
            {
                // Remove old curves from curve editor
                if (!size3D)
                {
                    m_SizeY.RemoveCurveFromEditor();
                    m_SizeZ.RemoveCurveFromEditor();
                }
            }

            // Keep states in sync
            if (!m_SizeX.stateHasMultipleDifferentValues)
            {
                m_SizeZ.SetMinMaxState(m_SizeX.state, size3D);
                m_SizeY.SetMinMaxState(m_SizeX.state, size3D);
            }

            if (size3D)
            {
                m_SizeX.m_DisplayName = s_Texts.x;
                GUITripleMinMaxCurve(GUIContent.none, s_Texts.x, m_SizeX, s_Texts.y, m_SizeY, s_Texts.z, m_SizeZ, null);
            }
            else
            {
                m_SizeX.m_DisplayName = s_Texts.size;
                GUIMinMaxCurve(s_Texts.size, m_SizeX);
            }

            // Rotation
            EditorGUI.BeginChangeCheck();
            bool rotation3D = GUIToggle(s_Texts.rotation3D, m_Rotation3D);
            if (EditorGUI.EndChangeCheck())
            {
                // Remove old curves from curve editor
                if (!rotation3D)
                {
                    m_RotationX.RemoveCurveFromEditor();
                    m_RotationY.RemoveCurveFromEditor();
                }
            }

            // Keep states in sync
            if (!m_RotationZ.stateHasMultipleDifferentValues)
            {
                m_RotationX.SetMinMaxState(m_RotationZ.state, rotation3D);
                m_RotationY.SetMinMaxState(m_RotationZ.state, rotation3D);
            }

            if (rotation3D)
            {
                m_RotationZ.m_DisplayName = s_Texts.z;
                GUITripleMinMaxCurve(GUIContent.none, s_Texts.x, m_RotationX, s_Texts.y, m_RotationY, s_Texts.z, m_RotationZ, null);
            }
            else
            {
                m_RotationZ.m_DisplayName = s_Texts.rotation;
                GUIMinMaxCurve(s_Texts.rotation, m_RotationZ);
            }

            GUIFloat(s_Texts.randomizeRotationDirection, m_RandomizeRotationDirection);
            GUIMinMaxGradient(s_Texts.color, m_Color, false);

            GUIMinMaxCurve(s_Texts.gravity, m_GravityModifier);
            int space = GUIPopup(s_Texts.simulationSpace, m_SimulationSpace, s_Texts.simulationSpaces);
            if (space == 2 && m_CustomSimulationSpace != null)
                GUIObject(s_Texts.customSimulationSpace, m_CustomSimulationSpace);
            GUIFloat(s_Texts.simulationSpeed, m_SimulationSpeed);
            GUIBoolAsPopup(s_Texts.deltaTime, m_UseUnscaledTime, new string[] { "Scaled", "Unscaled" });

            bool anyNonMesh = m_ParticleSystemUI.m_ParticleSystems.FirstOrDefault(o => !o.shape.enabled || (o.shape.shapeType != ParticleSystemShapeType.SkinnedMeshRenderer && o.shape.shapeType != ParticleSystemShapeType.MeshRenderer)) != null;
            if (anyNonMesh)
                GUIPopup(s_Texts.scalingMode, m_ScalingMode, s_Texts.scalingModes);

            bool oldPlayOnAwake = m_PlayOnAwake.boolValue;
            bool newPlayOnAwake = GUIToggle(s_Texts.autoplay, m_PlayOnAwake);
            if (oldPlayOnAwake != newPlayOnAwake)
                m_ParticleSystemUI.m_ParticleEffectUI.PlayOnAwakeChanged(newPlayOnAwake);

            GUIBoolAsPopup(s_Texts.emitterVelocity, m_EmitterVelocity, new string[] { "Transform", "Rigidbody" });

            GUIInt(s_Texts.maxParticles, m_MaxNumParticles);

            bool autoRandomSeed = GUIToggle(s_Texts.autoRandomSeed, m_AutoRandomSeed);
            if (!autoRandomSeed)
            {
                bool isInspectorView = m_ParticleSystemUI.m_ParticleEffectUI.m_Owner is ParticleSystemInspector;
                if (isInspectorView)
                {
                    GUILayout.BeginHorizontal();
                    GUIInt(s_Texts.randomSeed, m_RandomSeed);
                    if (!m_ParticleSystemUI.multiEdit && GUILayout.Button("Reseed", EditorStyles.miniButton, GUILayout.Width(60)))
                        m_RandomSeed.intValue = (int)m_ParticleSystemUI.m_ParticleSystems[0].GenerateRandomSeed();
                    GUILayout.EndHorizontal();
                }
                else
                {
                    GUIInt(s_Texts.randomSeed, m_RandomSeed);
                    if (!m_ParticleSystemUI.multiEdit && GUILayout.Button("Reseed", EditorStyles.miniButton))
                        m_RandomSeed.intValue = (int)m_ParticleSystemUI.m_ParticleSystems[0].GenerateRandomSeed();
                }
            }

            GUIPopup(s_Texts.stopAction, m_StopAction, s_Texts.stopActions);
        }

        override public void UpdateCullingSupportedString(ref string text)
        {
            if (m_SimulationSpace.intValue != 0)
                text += "\nLocal space simulation is not being used.";

            if (m_GravityModifier.state != MinMaxCurveState.k_Scalar)
                text += "\nGravity modifier is not constant.";

            if (m_StopAction.intValue != 0)
                text += "\nStop Action is being used.";
        }
    }
} // namespace UnityEditor
