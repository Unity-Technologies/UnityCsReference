// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Internal;

namespace UnityEngine.Audio
{
    ///<summary>Object representing a group in the mixer.</summary>
    ///<remarks>
    ///  <para>Used to route audio to specific channels in an <see cref="AudioMixer" />, allowing for grouped processing and effects.
    ///
    ///Commonly used for managing different categories of sounds, like music, sound effects (SFX), and dialogue with shared volume controls and audio effect settings.
    ///
    ///<see cref="AudioMixerGroup" />s can be nested for more complex audio hierarchies. Typically, a root "Master" group is used that contains multiple specific groups like "Music", "SFX" and "Dialogue".</para>
    ///  <para>**Note:** Create your <see cref="AudioMixerGroup" />s in the editor before referencing them in code.
    ///
    ///</para>
    ///</remarks>
    ///<example>
    ///  <code><![CDATA[using UnityEngine;
    ///using UnityEngine.Audio;
    ///
    /// // Example of a class that manages audio in a game
    ///public class AudioManager : MonoBehaviour
    ///{
    ///    // References to the AudioMixer and AudioSources
    ///    public AudioMixer audioMixer;
    ///    public AudioSource musicSource;
    ///    public AudioSource sfxSource;
    ///
    ///    // Handles to the AudioMixerGroups
    ///    private AudioMixerGroup musicGroup;
    ///    private AudioMixerGroup sfxGroup;
    ///
    ///    private void Start()
    ///    {
    ///        // Find the AudioMixerGroups and set the output of the AudioSources to them
    ///        musicGroup = audioMixer.FindMatchingGroups("Music")[0];
    ///        musicSource.outputAudioMixerGroup = musicGroup;
    ///
    ///        sfxGroup = audioMixer.FindMatchingGroups("SFX")[0];
    ///        sfxSource.outputAudioMixerGroup = sfxGroup;
    ///
    ///        SetMusicVolume(0.8f);
    ///        SetSFXVolume(1.0f);
    ///    }
    ///
    ///    public void SetMusicVolume(float volume)
    ///    {
    ///        // Set the volume of the Music group in the AudioMixer
    ///        // Volume needs to be exposed in the AudioMixer
    ///        audioMixer.SetFloat("MusicVolume", volume);
    ///    }
    ///
    ///    public void SetSFXVolume(float volume)
    ///    {
    ///        // Set the volume of the SFX group in the AudioMixer
    ///        // Volume needs to be exposed in the AudioMixer
    ///        audioMixer.SetFloat("SFXVolume", volume);
    ///    }
    ///}
    ///]]></code>
    ///</example>
    ///<seealso cref="AudioMixer" />
    ///<seealso cref="AudioSource" />
    ///<seealso cref="AudioListener" />
    [global::UnityEngine.NativeClass("AudioMixerGroup", PersistentTypeId = 273)]
    [NativeHeader("Modules/Audio/Public/AudioMixerGroup.h")]
    public class AudioMixerGroup : Object, ISubAssetNotDuplicatable
    {
        protected internal AudioMixerGroup(global::UnityEngine.EntityId id) : base(id) {}
        // Make constructor internal
        internal AudioMixerGroup() {}

        ///<summary>Gain access to the <see cref="AudioMixer" /> that this AudioMixerGroup belongs to (RO).</summary>
        ///<remarks>This property is useful if you want to access the <see cref="AudioMixer" /> and modify its properties or the properties of its groups dynamically.</remarks>
        ///<example>
        ///  <code><![CDATA[using UnityEngine;
        ///using UnityEngine.Audio;
        ///
        ///public class ExampleAudioMixer : MonoBehaviour
        ///{
        ///    public AudioMixerGroup audioMixerGroup;
        ///
        ///    void Start()
        ///    {
        ///        // Output the name of the AudioMixer that this AudioMixerGroup belongs to
        ///        AudioMixer parentMixer = audioMixerGroup.audioMixer;
        ///        Debug.Log("AudioMixer Name: " + parentMixer.name);
        ///
        ///        // Use the exposed parameters of different AudioMixerGroups to change the volume of those groups. 
        ///        // Make sure to expose the parameters you want to change in your groups and rename them to something memorable. 
        ///        // "TestVolume" and "MainVolume" are the exposed and renamed volume parameters of 2 different AudioMixerGroups. 
        ///        parentMixer.SetFloat("TestVolume", -80f);
        ///        parentMixer.SetFloat("MainVolume", 5.0f);
        ///    }
        ///}
        ///]]></code>
        ///</example>
        [NativeProperty]
        public extern AudioMixer audioMixer { get; }
    }
}
