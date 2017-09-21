// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace UnityEditor
{
    // Utility class that manages a pool of strings depending on which lighting mode is currently selected for each light category,
    // i.e. realtime, mixed, baked. Light modes can be set in the editor via the new lighting window.
    // The Draw routines can be used to display a selection box which will show the currently selected mode for each category.
    internal class LightModeUtil
    {
        // should match enum LightmapMixedBakeMode from LightmapEditorSettings.h
        internal enum LightmapMixedBakeMode
        {
            IndirectOnly = 0,
            LightmapsWithSubtractiveShadows = 1,
            ShadowmaskAndIndirect = 2,
        }

        public  static readonly GUIContent s_enableBaked = EditorGUIUtility.TextContent("Baked Global Illumination|Controls whether Mixed and Baked lights will use baked Global Illumination. If enabled, Mixed lights are baked using the specified Lighting Mode and Baked lights will be completely baked and not adjustable at runtime.");

        // combobox content
        private int[]               m_modeVals          = { 0, 0, 0 };
        // cached data
        private Object              m_cachedObject      = null;
        private SerializedObject    m_so                = null;
        private SerializedProperty  m_enableRealtimeGI  = null;
        private SerializedProperty  m_mixedBakeMode     = null;
        private SerializedProperty  m_useShadowmask     = null;
        private SerializedProperty  m_enabledBakedGI    = null;
        private SerializedProperty  m_workflowMode      = null;
        private SerializedProperty  m_environmentMode   = null;
        // global static singleton style pointer
        private static LightModeUtil gs_ptr              = null;

        // singleton accessor, so this utility can be used by the lightingwindowlightingtab and the lighteditor in the inspector
        public static LightModeUtil Get()
        {
            if (gs_ptr == null)
                gs_ptr = new LightModeUtil();

            return gs_ptr;
        }

        private LightModeUtil()
        {
            Load();
        }

        // access the currently set modes, 0 based for each category
        public void GetModes(out int realtimeMode, out int mixedMode)
        {
            realtimeMode = m_modeVals[0];
            mixedMode = m_modeVals[1];
        }

        // query whether any lightmaps should be baked - only returns meaningful results if Load() was called beforehand
        public bool AreBakedLightmapsEnabled()
        {
            return m_enabledBakedGI != null ? m_enabledBakedGI.boolValue : false;
        }

        // query whether realtime GI is active - only returns meaningful results if Load() was called beforehand
        public bool IsRealtimeGIEnabled()
        {
            return m_enableRealtimeGI != null ? m_enableRealtimeGI.boolValue : false;
        }

        // query whether any GI is active
        public bool IsAnyGIEnabled()
        {
            return IsRealtimeGIEnabled() || AreBakedLightmapsEnabled();
        }

        // get the ambient mode - returns true if the user's setting is used, returns false if the user settings are overridden
        public bool GetAmbientLightingMode(out int mode)
        {
            if (AreBakedLightmapsEnabled() && IsRealtimeGIEnabled())
            {
                mode = m_environmentMode.intValue;
                return true;
            }

            mode = AreBakedLightmapsEnabled() ? 1 : 0;
            return false;
        }

        // returns the ambient mode depending on global lighting settings - this is what the system actually ends up using
        public int GetAmbientLightingMode()
        {
            int mode;
            GetAmbientLightingMode(out mode);
            return mode;
        }

        // query whether subtractive mode is active - can be used to determine whether the shadow mask color parameter should be displayed
        public bool IsSubtractiveModeEnabled()
        {
            return m_modeVals[1] == (int)LightmapMixedBakeMode.LightmapsWithSubtractiveShadows;
        }

        // query whether the GI workflow mode is set to auto or explicitly pressing the Bake button is required
        public bool IsWorkflowAuto()
        {
            return m_workflowMode.intValue == (int)Lightmapping.GIWorkflowMode.Iterative;
        }

        public void SetWorkflow(bool bAutoEnabled)
        {
            m_workflowMode.intValue = (int)(bAutoEnabled ? Lightmapping.GIWorkflowMode.Iterative : Lightmapping.GIWorkflowMode.OnDemand);
        }

        // access the cached props of the currently serialized render settings
        public void GetProps(out SerializedProperty o_enableRealtimeGI, out SerializedProperty o_enableBakedGI, out SerializedProperty o_mixedBakeMode, out SerializedProperty o_useShadowMask)
        {
            Debug.Assert(m_so != null, "No valid lightmap settings object loaded. Call Load() first and check the return value before calling this function.");
            o_enableRealtimeGI = m_enableRealtimeGI;
            o_enableBakedGI = m_enabledBakedGI;
            o_mixedBakeMode = m_mixedBakeMode;
            o_useShadowMask = m_useShadowmask;
        }

        // load render settings, cache props and update internal state
        public bool Load()
        {
            if (!CheckCachedObject())
                return false;

            int realtimeMode = m_enableRealtimeGI.boolValue ? 0 : 1;
            int mixedMode  = m_mixedBakeMode.intValue;

            Update(realtimeMode, mixedMode);
            return true;
        }

        // write back the current state into the properties. Does not serialize the properties, call Flush for that.
        public void Store(int realtimeMode, int mixedMode)
        {
            Update(realtimeMode, mixedMode);
            if (!CheckCachedObject())
                return;

            m_enableRealtimeGI.boolValue = m_modeVals[0] == 0;
            m_mixedBakeMode.intValue = m_modeVals[1];
            m_useShadowmask.boolValue = (m_modeVals[1] == (int)LightmapMixedBakeMode.ShadowmaskAndIndirect);
        }

        // write the properties
        public bool Flush()
        {
            return m_so.ApplyModifiedProperties();
        }

        // helper function to draw the checkbox for enabling baked GI
        public void DrawBakedGIElement()
        {
            EditorGUILayout.PropertyField(m_enabledBakedGI, s_enableBaked);
        }

        // calls the runtime to get some statistics on the current scene's lights
        public void AnalyzeScene(ref LightModeValidator.Stats stats)
        {
            LightModeValidator.AnalyzeScene(m_modeVals[0], m_modeVals[1], m_modeVals[2], GetAmbientLightingMode(), ref stats);
        }

        // checks whether the lightmap settings have changed and recaches data if necessary
        private bool CheckCachedObject()
        {
            Object o = LightmapEditorSettings.GetLightmapSettings();
            if (o == null)
                return false;
            else if (o == m_cachedObject)
            {
                m_so.UpdateIfRequiredOrScript();
                return true;
            }

            m_cachedObject = o;
            m_so = new SerializedObject(o);
            m_enableRealtimeGI      = m_so.FindProperty("m_GISettings.m_EnableRealtimeLightmaps");
            m_mixedBakeMode         = m_so.FindProperty("m_LightmapEditorSettings.m_MixedBakeMode");
            m_useShadowmask         = m_so.FindProperty("m_UseShadowmask");
            m_enabledBakedGI        = m_so.FindProperty("m_GISettings.m_EnableBakedLightmaps");
            m_workflowMode          = m_so.FindProperty("m_GIWorkflowMode");
            m_environmentMode       = m_so.FindProperty("m_GISettings.m_EnvironmentLightingMode");

            return true;
        }

        // updates combobox string content data
        private void Update(int realtimeMode, int mixedMode)
        {
            m_modeVals[0] = realtimeMode;
            m_modeVals[1] = mixedMode;
            m_modeVals[2] = 0;
        }
    }

    internal class LightModeValidator
    {
        [System.Flags]
        internal enum Receivers
        {
            None       = 0,
            StaticMesh = 1 << 0,
            LightProbe = 1 << 1
        }

        [System.Flags]
        internal enum Emitters
        {
            None                = 0,
            RealtimeLight       = 1 << 1,
            RealtimeAmbient     = 1 << 2,
            RealtimeEmissive    = 1 << 3,
            BakedLight          = 1 << 4,
            BakedAmbient        = 1 << 5,
            BakedEmissive       = 1 << 6,

            Realtime = RealtimeLight | RealtimeAmbient | RealtimeEmissive,
            Baked = BakedLight | BakedAmbient | BakedEmissive
        }

        internal struct Stats
        {
            public Receivers receiverMask;
            public Emitters  emitterMask;
            // set lighting modes
            public int realtimeMode;
            public int mixedMode;
            public int bakedMode;
            public int ambientMode;
            // flags which modes need to be enabled
            public bool requiresRealtimeGI;
            public bool requiresLightmaps;
            // scene stats
            public LightingStats enabled;
            public LightingStats active;
            public LightingStats inactive;

            public void Reset()
            {
                receiverMask        = 0;
                emitterMask         = 0;
                realtimeMode         = 0;
                mixedMode           = 0;
                bakedMode           = 0;
                ambientMode         = 0;
                requiresRealtimeGI  = false;
                requiresLightmaps   = false;
                enabled.Reset();
                active.Reset();
                inactive.Reset();
            }
        }

        internal static void AnalyzeScene(int realtimeMode, int mixedMode, int bakedMode, int ambientMode, ref Stats stats)
        {
            stats.Reset();
            stats.realtimeMode  = realtimeMode;
            stats.mixedMode     = mixedMode;
            stats.bakedMode     = bakedMode;
            stats.ambientMode   = ambientMode;


            LightmapEditorSettings.AnalyzeLighting(out stats.enabled, out stats.active, out stats.inactive);

            stats.emitterMask = Emitters.None;
            stats.emitterMask |= stats.enabled.realtimeLightsCount > 0          ? Emitters.RealtimeLight    : 0;
            stats.emitterMask |= stats.enabled.staticMeshesRealtimeEmissive > 0 ? Emitters.RealtimeEmissive : 0;
            stats.emitterMask |= IsAmbientRealtime(ref stats)                 ? Emitters.RealtimeAmbient  : 0;
            stats.emitterMask |= stats.enabled.bakedLightsCount > 0             ? Emitters.BakedLight       : 0;
            stats.emitterMask |= stats.enabled.staticMeshesBakedEmissive > 0    ? Emitters.BakedEmissive    : 0;
            stats.emitterMask |= IsAmbientBaked(ref stats)                    ? Emitters.BakedAmbient     : 0;

            stats.receiverMask = Receivers.None;
            stats.receiverMask |= stats.enabled.lightProbeGroupsCount > 0 ? Receivers.LightProbe : 0;
            stats.receiverMask |= stats.enabled.staticMeshesCount     > 0 ? Receivers.StaticMesh : 0;

            if (stats.receiverMask == 0)
            {
                stats.requiresRealtimeGI = false;
                stats.requiresLightmaps  = false;
            }
            else
            {
                stats.requiresRealtimeGI = IsRealtimeGI(ref stats);   // realtime lights could be spawned via script during gameplay, can't reason on this based on the current scene
                stats.requiresLightmaps  = (stats.emitterMask & Emitters.Baked) != 0;
            }
        }

        private static bool IsRealtimeGI(ref Stats stats)       { return stats.realtimeMode == 0; }
        private static bool IsAmbientRealtime(ref Stats stats)   { return stats.ambientMode == 0; }
        private static bool IsAmbientBaked(ref Stats stats)     { return stats.ambientMode == 1; }
    }
}
