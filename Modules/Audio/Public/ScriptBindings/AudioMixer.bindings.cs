// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using System;
using JetBrains.Annotations;
using UnityEngine.Bindings;
using Unity.Scripting.LifecycleManagement;

namespace UnityEngine.Audio
{
    ///<summary>The mode in which an AudioMixer should update its time.</summary>
    ///<remarks>Use this with AudioMixer.updateMode to define how the AudioMixer time is updated.</remarks>
    public enum AudioMixerUpdateMode
    {
        ///<summary>Update the AudioMixer with scaled game time.</summary>
        ///<remarks>This means that Snapshot transitions will be affected by time scaling.</remarks>
        Normal = 0,
        ///<summary>Update the AudioMixer with unscaled realtime.</summary>
        ///<remarks>Use this to ignore the games current time scale and update Snapshot transitions in realtime.</remarks>
        UnscaledTime = 1
    }

    ///<summary>AudioMixer asset.</summary>
    ///<remarks>This is a singleton representing a specific audio mixer asset in the project.</remarks>
    [ExcludeFromPreset]
    [ExcludeFromObjectFactory]
    [NativeHeader("Modules/Audio/Public/AudioMixer.h")]
    [global::UnityEngine.NativeClass("AudioMixer", PersistentTypeId = 240)]
    [NativeHeader("Modules/Audio/Public/ScriptBindings/AudioMixer.bindings.h")]
    public partial class AudioMixer : Object
    {
        internal AudioMixer() {}

        ///<summary>Routing target.</summary>
        ///<remarks>The audio mixer to which this mixer routes its output.</remarks>
        [NativeProperty]
        public extern AudioMixerGroup outputAudioMixerGroup { get; set; }

        ///<summary>The name must be an exact match.</summary>
        ///<param name="name">Name of snapshot object to be returned.</param>
        ///<returns>The snapshot identified by the name.</returns>
        [NativeMethod("FindSnapshotFromName")]
        public extern AudioMixerSnapshot FindSnapshot(string name);

        ///<summary>Returns mixer groups whose path contains the specified substring.</summary>
        ///<remarks>
        ///  <para>Connected groups in the mixer form a path from the mixer's master group to the leaves. This path has the format **Master Group/Child of Master Group/Grandchild of Master Group**, and so on.
        ///
        ///<img src="AudioMixerFindMatchingGroupsExampleHierarchy.png" /></para>
        ///  <para />
        ///</remarks>
        ///<param name="subPath">Substring to match against group paths.</param>
        ///<returns>Groups in the mixer whose paths contain the specified substring.</returns>
        ///<example>
        ///  <code><![CDATA[
        ///using UnityEngine;
        ///using UnityEngine.Audio;
        ///
        ///public class FindMatchingMixerGroups : MonoBehaviour
        ///{
        ///    public AudioMixer mixer;
        ///    const string dropsVolumeParam = "DropsVolume";
        ///
        ///    void Start()
        ///    {
        ///        // Returns the group at path Master/WATER/DROPS (see hierarchy image above).
        ///        var dropsGroup = mixer.FindMatchingGroups("DROPS")[0];
        ///
        ///        // Returns Master/AMBIENCE/CROWD, Master/AMBIENCE/ROAD, and Master/AMBIENCE.
        ///        var ambienceGroups = mixer.FindMatchingGroups("Master/AMBIENCE");
        ///
        ///        // Returns ROAD and RIVER.
        ///        var rGroups = mixer.FindMatchingGroups("/R");
        ///
        ///        // Exposed parameters are read and written on the AudioMixer, not on AudioMixerGroup.
        ///        if (dropsGroup.audioMixer.GetFloat(dropsVolumeParam, out float volume))
        ///        {
        ///            Debug.Log($"{dropsVolumeParam}: {volume}");
        ///            dropsGroup.audioMixer.SetFloat(dropsVolumeParam, volume);
        ///        }
        ///    }
        ///}
        ///]]></code>
        ///</example>
        ///<seealso cref="Audio.AudioMixer.GetFloat" />
        ///<seealso cref="Audio.AudioMixer.SetFloat" />
        ///<seealso cref="Audio.AudioMixerGroup" />
        ///<seealso cref="Audio.AudioMixer" />
        [NativeMethod("AudioMixerBindings::FindMatchingGroups", IsFreeFunction = true, HasExplicitThis = true)]
        public extern AudioMixerGroup[] FindMatchingGroups(string subPath);

        // Broadcast to internal consumers when script code requests a snapshot transition, after argument validation.
        [AutoStaticsCleanupOnCodeReload]
        internal static event Action<AudioMixer, AudioMixerSnapshot[], float[], float> snapshotTransitionRequested;

        internal void TransitionToSnapshot(AudioMixerSnapshot snapshot, float timeToReach)
        {
            if (snapshot == null)
                throw new ArgumentException("null Snapshot passed to AudioMixer.TransitionToSnapshot of AudioMixer '" + name + "'");

            if (snapshot.audioMixer != this)
                throw new ArgumentException("Snapshot '" + snapshot.name + "' passed to AudioMixer.TransitionToSnapshot is not a snapshot from AudioMixer '" + name + "'");

            TransitionToSnapshotInternal(snapshot, timeToReach);

            if (snapshotTransitionRequested != null)
                RaiseSnapshotTransitionRequested(new[] { snapshot }, new[] { 1.0f }, timeToReach);
        }

        [NativeMethod("TransitionToSnapshot")]
        private extern void TransitionToSnapshotInternal(AudioMixerSnapshot snapshot, float timeToReach);

        ///<summary>Transitions to a weighted mixture of the snapshots specified. This can be used for games that specify the game state as a continuum between states or for interpolating snapshots from a triangulated map location.</summary>
        ///<param name="snapshots">The set of snapshots to be mixed.</param>
        ///<param name="weights">The mix weights for the snapshots specified.</param>
        ///<param name="timeToReach">Relative time after which the mixture should be reached from any current state.</param>
        public void TransitionToSnapshots(AudioMixerSnapshot[] snapshots, float[] weights, float timeToReach)
        {
            TransitionToSnapshotsInternal(snapshots, weights, timeToReach);

            if (snapshotTransitionRequested != null)
            {
                // Clone the arrays so subscribers are unaffected by callers mutating them after the call.
                // Null arrays are forwarded as-is; the native call above treats them as empty.
                RaiseSnapshotTransitionRequested(
                    (AudioMixerSnapshot[])snapshots?.Clone(), (float[])weights?.Clone(), timeToReach);
            }
        }

        [NativeMethod("AudioMixerBindings::TransitionToSnapshots", IsFreeFunction = true, HasExplicitThis = true, ThrowsException = true)]
        private extern void TransitionToSnapshotsInternal(AudioMixerSnapshot[] snapshots, float[] weights, float timeToReach);

        void RaiseSnapshotTransitionRequested(
            AudioMixerSnapshot[] snapshots, float[] weights, float timeToReach)
        {
            var handlers = snapshotTransitionRequested;
            if (handlers == null)
                return;

            foreach (Action<AudioMixer, AudioMixerSnapshot[], float[], float> handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(this, snapshots, weights, timeToReach);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        ///<summary>How time should progress for this AudioMixer. Used during Snapshot transitions.</summary>
        ///<remarks>During update of AudioMixers and Snapshot transitions, this property defines how those transitions should progress.
        ///
        ///- AudioMixerUpdateMode.Normal updates the AudioMixer with scaled game time progression.
        ///- AudioMixerUpdateMode.UnscaledTime ignores <see cref="Time.timeScale" /> changes and updates the AudioMixer in realtime.</remarks>
        [NativeProperty]
        public extern AudioMixerUpdateMode updateMode { get; set; }

        // Broadcast to internal consumers when script code successfully sets or clears an exposed-parameter
        // override. A null value means the override was cleared and the parameter is snapshot-controlled again.
        [AutoStaticsCleanupOnCodeReload]
        internal static event Action<AudioMixer, string, float?> parameterOverrideChanged;

        ///<summary>
        ///  <see cref="AudioMixer.SetFloat" /> sets the value of the exposed parameter specified. Once you call this function, mixer snapshots will no longer control the exposed parameter, and you can only modify the parameter using <see cref="AudioMixer.SetFloat" />.</summary>
        ///<remarks>**Note:** Don’t call <see cref="AudioMixer.SetFloat" /> in the following event functions as it can result in unexpected behavior: 
        ///
        ///- <see cref="M:UnityEngine.MonoBehaviour.Awake" />
        ///- <see cref="M:UnityEngine.MonoBehaviour.OnEnable" />
        ///- <see cref="RuntimeInitializeLoadType.AfterSceneLoad" />
        ///
        ///Instead, call <see cref="AudioMixer.SetFloat" /> in <see cref="M:UnityEngine.MonoBehaviour.Start" /> or any event function Unity calls afterwards in [Order of execution for event functions](xref:execution-order).
        ///
        ///To see your exposed parameters, &lt;ol&gt;&lt;li&gt; Double click on your audio mixer. This opens the **Audio Mixer** window.&lt;/li&gt;&lt;li&gt; At the top right of the Audio Mixer tab, click on the **Exposed Parameters** button to show the list of exposed parameters. &lt;/li&gt;&lt;/ol&gt; To rename or remove a parameter, right click the item in the list. 
        ///
        ///If the parameter you want to expose isn't in the list, you need to expose the parameter. To expose a parameter, right click the parameter you want to expose in the Audio Mixer Inspector window, and choose **Expose [parameter name] to script**.</remarks>
        ///<param name="name">The name of an exposed Audio Mixer group parameter. To expose a parameter, go to the Audio Mixer group's Inspector window, right click the parameter you want to expose, and choose **Expose [parameter name] to script**.</param>
        ///<param name="value">Use to set the exposed Audio Mixer group parameter to a new value.</param>
        ///<returns>Returns false if the exposed parameter was not found or snapshots are currently being edited.</returns>
        ///<example>
        ///  <code><![CDATA[using System;
        ///using UnityEngine;
        ///using UnityEngine.Audio;
        ///
        ///public class MixerVolumeController : MonoBehaviour
        ///{
        ///    // The range of the volume slider on a mixer group
        ///    const float minVolume = -80f;
        ///    const float maxVolume = 20f;
        ///
        ///    public AudioMixer mixer;
        ///
        ///    [Range(minVolume, maxVolume)]
        ///    public float volume;
        ///
        ///    float previousVolume;
        ///
        ///    void Update()
        ///    {
        ///
        ///        // Sets the exposed parameter "volume" in the audio mixer,
        ///        // In this example the parameter is assumed to be the volume of a mixer group.
        ///        // It could be any other exposable mixer parameter.
        ///        if (!Mathf.Approximately(volume, previousVolume))
        ///        {
        ///            mixer.SetFloat("volume", volume);
        ///        }
        ///
        ///        previousVolume = volume;
        ///    }
        ///
        ///    void OnGUI()
        ///    {
        ///        GUILayout.BeginHorizontal();
        ///        GUILayout.Label("Mixer volume");
        ///        var newVolume = GUILayout.HorizontalSlider(volume, minVolume, maxVolume, GUILayout.Width(300));
        ///
        ///        if (!Mathf.Approximately(newVolume, previousVolume))
        ///        {
        ///            volume = newVolume;
        ///            mixer.SetFloat("volume", volume);
        ///        }
        ///
        ///        GUILayout.EndHorizontal();
        ///    }
        ///}]]></code>
        ///</example>
        public bool SetFloat(string name, float value)
        {
            if (!SetFloatInternal(name, value))
                return false;

            if (parameterOverrideChanged != null)
                RaiseParameterOverrideChanged(name, value);

            return true;
        }

        [NativeMethod("SetFloat")]
        private extern bool SetFloatInternal(string name, float value);

        ///<summary>Resets an exposed parameter to its initial value.</summary>
        ///<param name="name">Exposed parameter.</param>
        ///<returns>Returns false if the parameter was not found or could not be set.</returns>
        public bool ClearFloat(string name)
        {
            if (!ClearFloatInternal(name))
                return false;

            if (parameterOverrideChanged != null)
                RaiseParameterOverrideChanged(name, null);

            return true;
        }

        [NativeMethod("ClearFloat")]
        private extern bool ClearFloatInternal(string name);

        void RaiseParameterOverrideChanged(string name, float? value)
        {
            var handlers = parameterOverrideChanged;
            if (handlers == null)
                return;

            foreach (Action<AudioMixer, string, float?> handler in handlers.GetInvocationList())
            {
                try
                {
                    handler(this, name, value);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        ///<summary>Returns the value of the exposed parameter specified. If the parameter doesn't exist the function returns false. Prior to calling SetFloat and after ClearFloat has been called on this parameter the value returned will be that of the current snapshot or snapshot transition.</summary>
        ///<remarks>To see your exposed parameters, &lt;ol&gt;&lt;li&gt; Double click on your audio mixer. This opens the Audio Mixer window.&lt;/li&gt;&lt;li&gt; At the top right of the **Audio Mixer** window, click on the **Exposed Parameters** button to show the list of exposed parameters. &lt;/li&gt;&lt;/ol&gt; To rename or remove a parameter, right click the item in the list. 
        ///
        ///If the parameter you want to expose isn't in the list, you need to expose the parameter. To expose a parameter, right click the parameter you want to expose in the Audio Mixer Inspector window, and choose **Expose [parameter name] to script**.</remarks>
        ///<param name="name">Name of exposed parameter.</param>
        ///<param name="value">Return value of exposed parameter.</param>
        ///<returns>Returns false if the exposed parameter specified doesn't exist.</returns>
        ///<example>
        ///  <code><![CDATA[using System;
        ///using UnityEngine;
        ///using UnityEngine.Audio;
        ///
        /// // 1. Attach this script to a GameObject in your Scene.
        /// // 2. Create an Audio Mixer and expose some variables on it.
        /// // 3. Add an Audio Source to your Scene and assign your Audio Mixer to it. 
        ///
        ///public class MixerVolumeController : MonoBehaviour
        ///{
        ///    // Make sure to assign your Audio Mixer in the Inspector window of the GameObject you attach this script to.
        ///    public AudioMixer mixer;
        ///    float volume, exposedParam;
        ///
        ///    void Start()
        ///{
        ///
        ///    // Gets the exposed parameters "MyExposedParam" and "volume" in the Audio Mixer.
        ///    // "MyExposedParam" is the default name for exposed parameters.
        /// 
        ///    // "Volume is an exposed parameter that has been renamed. 
        ///    // Change these names to what your exposed parameters are called. 
        ///
        ///    mixer.GetFloat("MyExposedParam", out exposedParam);
        ///    Debug.Log("My Exposed Param: " + exposedParam);
        ///
        ///    mixer.GetFloat("Volume", out volume);
        ///    Debug.Log("Volume: " + volume);
        ///}
        ///}
        ///]]></code>
        ///</example>
        [NativeMethod]
        public extern bool GetFloat(string name, out float value);

        [NativeMethod("AudioMixerBindings::GetAbsoluteAudibilityFromGroup", HasExplicitThis = true, IsFreeFunction = true)]
        internal extern float GetAbsoluteAudibilityFromGroup(AudioMixerGroup group);
    }
}
