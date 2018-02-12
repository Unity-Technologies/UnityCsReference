// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngineInternal;
using UnityEngine.Audio;

namespace UnityEngine
{
    internal class AudioExtensionDefinition
    {
        public AudioExtensionDefinition(AudioExtensionDefinition definition)
        {
            assemblyName        = definition.assemblyName;
            extensionNamespace  = definition.extensionNamespace;
            extensionTypeName   = definition.extensionTypeName;
            extensionType       = GetExtensionType();
        }

        public AudioExtensionDefinition(string assemblyNameIn, string extensionNamespaceIn, string extensionTypeNameIn)
        {
            assemblyName        = assemblyNameIn;
            extensionNamespace  = extensionNamespaceIn;
            extensionTypeName   = extensionTypeNameIn;
            extensionType       = GetExtensionType();
        }

        public Type GetExtensionType()
        {
            if (extensionType == null)
                extensionType = Type.GetType(extensionNamespace + "." + extensionTypeName + ", " + assemblyName);

            return extensionType;
        }

        private string   assemblyName;
        private string   extensionNamespace;
        private string   extensionTypeName;
        private Type     extensionType;
    }

    internal class AudioSpatializerExtensionDefinition
    {
        public AudioSpatializerExtensionDefinition(string spatializerNameIn, AudioExtensionDefinition definitionIn, AudioExtensionDefinition editorDefinitionIn)
        {
            spatializerName     = spatializerNameIn;
            definition          = definitionIn;
            editorDefinition    = editorDefinitionIn;
        }

        public PropertyName             spatializerName;
        public AudioExtensionDefinition definition;
        public AudioExtensionDefinition editorDefinition;
    }

    internal class AudioAmbisonicExtensionDefinition
    {
        public AudioAmbisonicExtensionDefinition(string ambisonicNameIn, AudioExtensionDefinition definitionIn)
        {
            ambisonicPluginName = ambisonicNameIn;
            definition = definitionIn;
        }

        public PropertyName             ambisonicPluginName;
        public AudioExtensionDefinition definition;
    }

    internal class AudioListenerExtension : ScriptableObject
    {
        [SerializeField]
        private AudioListener m_audioListener;
        public AudioListener audioListener
        {
            get { return m_audioListener; }
            set { m_audioListener = value; }
        }

        public virtual float ReadExtensionProperty(PropertyName propertyName) { return 0.0f; }
        public virtual void  WriteExtensionProperty(PropertyName propertyName, float propertyValue) {}
        public virtual void  ExtensionUpdate() {}
    }

    internal class AudioSourceExtension : ScriptableObject
    {
        [SerializeField]
        private AudioSource m_audioSource;
        public AudioSource audioSource
        {
            get { return m_audioSource; }
            set { m_audioSource = value; }
        }

        public virtual float ReadExtensionProperty(PropertyName propertyName) { return 0.0f; }
        public virtual void  WriteExtensionProperty(PropertyName propertyName, float propertyValue) {}
        public virtual void  Play() {}
        public virtual void  Stop() {}
        public virtual void  ExtensionUpdate() {}

        public void OnDestroy()
        {
            Stop();
            AudioExtensionManager.RemoveExtensionFromManager(this);

            if (audioSource != null)
            {
                if (audioSource.spatializerExtension == this)
                    audioSource.spatializerExtension = null;

                if (audioSource.ambisonicExtension == this)
                    audioSource.ambisonicExtension = null;
            }
        }

        internal int m_ExtensionManagerUpdateIndex = -1;
    }

    internal partial class AudioExtensionManager
    {
        // Lists of the spatializer extensions that have been registered.
        static private List<AudioSpatializerExtensionDefinition> m_ListenerSpatializerExtensionDefinitions = new List<AudioSpatializerExtensionDefinition>();
        static private List<AudioSpatializerExtensionDefinition> m_SourceSpatializerExtensionDefinitions = new List<AudioSpatializerExtensionDefinition>();
        static private List<AudioAmbisonicExtensionDefinition> m_SourceAmbisonicDecoderExtensionDefinitions = new List<AudioAmbisonicExtensionDefinition>();

        // List of audio source extensions that are associated with playing audio sources.
        static private List<AudioSourceExtension> m_SourceExtensionsToUpdate = new List<AudioSourceExtension>();

        static private int m_NextStopIndex = 0;
        static private bool m_BuiltinDefinitionsRegistered = false;
        static private PropertyName m_SpatializerName = 0;
        static private PropertyName m_SpatializerExtensionName = 0;
        static private PropertyName m_ListenerSpatializerExtensionName = 0;

        static internal bool IsListenerSpatializerExtensionRegistered()
        {
            foreach (AudioSpatializerExtensionDefinition extensionDefinition in m_ListenerSpatializerExtensionDefinitions)
            {
                if (AudioSettings.GetSpatializerPluginName() == extensionDefinition.spatializerName)
                    return true;
            }

            return false;
        }

        static internal bool IsSourceSpatializerExtensionRegistered()
        {
            foreach (AudioSpatializerExtensionDefinition extensionDefinition in m_SourceSpatializerExtensionDefinitions)
            {
                if (AudioSettings.GetSpatializerPluginName() == extensionDefinition.spatializerName)
                    return true;
            }

            return false;
        }

        static internal bool IsSourceAmbisonicDecoderExtensionRegistered()
        {
            foreach (AudioAmbisonicExtensionDefinition extensionDefinition in m_SourceAmbisonicDecoderExtensionDefinitions)
            {
                if (AudioSettings.GetAmbisonicDecoderPluginName() == extensionDefinition.ambisonicPluginName)
                    return true;
            }

            return false;
        }

        // Check to see if we need to add a spatializer extension to this audio source, based on the currently-registered extensions.
        static internal AudioSourceExtension AddSpatializerExtension(AudioSource source)
        {
            if (!source.spatialize)
                return null;

            if (source.spatializerExtension != null)
                return source.spatializerExtension;

            RegisterBuiltinDefinitions();

            foreach (AudioSpatializerExtensionDefinition extensionDefinition in m_SourceSpatializerExtensionDefinitions)
            {
                if (AudioSettings.GetSpatializerPluginName() == extensionDefinition.spatializerName)
                {
                    AudioSourceExtension newExtension = source.AddSpatializerExtension(extensionDefinition.definition.GetExtensionType());
                    if (newExtension != null)
                    {
                        newExtension.audioSource = source;
                        source.spatializerExtension = newExtension;

                        WriteExtensionProperties(newExtension, extensionDefinition.definition.GetExtensionType().Name);
                        return newExtension;
                    }
                }
            }

            return null;
        }

        // Check to see if we need to add an ambisonic decoder extension to this audio source, based on the currently-registered extensions.
        static internal AudioSourceExtension AddAmbisonicDecoderExtension(AudioSource source)
        {
            if (source.ambisonicExtension != null)
                return source.ambisonicExtension;

            RegisterBuiltinDefinitions();

            foreach (AudioAmbisonicExtensionDefinition extensionDefinition in m_SourceAmbisonicDecoderExtensionDefinitions)
            {
                if (AudioSettings.GetAmbisonicDecoderPluginName() == extensionDefinition.ambisonicPluginName)
                {
                    AudioSourceExtension newExtension = source.AddAmbisonicExtension(extensionDefinition.definition.GetExtensionType());
                    if (newExtension != null)
                    {
                        newExtension.audioSource = source;
                        source.ambisonicExtension = newExtension;

                        return newExtension;
                    }
                }
            }

            return null;
        }

        static internal void WriteExtensionProperties(AudioSourceExtension extension, string extensionName)
        {
            if (m_SpatializerExtensionName == 0)
                m_SpatializerExtensionName = extensionName;

            for (int sourceIndex = 0; sourceIndex < extension.audioSource.GetNumExtensionProperties(); sourceIndex++)
            {
                if (extension.audioSource.ReadExtensionName(sourceIndex) == m_SpatializerExtensionName)
                {
                    PropertyName propertyName = extension.audioSource.ReadExtensionPropertyName(sourceIndex);
                    float propertyValue = extension.audioSource.ReadExtensionPropertyValue(sourceIndex);

                    extension.WriteExtensionProperty(propertyName, propertyValue);
                }
            }

        }

        // Check to see if we need to add a spatializer extension to the audio listener.
        static internal AudioListenerExtension AddSpatializerExtension(AudioListener listener)
        {
            if (listener.spatializerExtension != null)
                return listener.spatializerExtension;

            RegisterBuiltinDefinitions();

            foreach (AudioSpatializerExtensionDefinition extensionDefinition in m_ListenerSpatializerExtensionDefinitions)
            {
                if ((AudioSettings.GetSpatializerPluginName() == extensionDefinition.spatializerName) ||
                    (AudioSettings.GetAmbisonicDecoderPluginName() == extensionDefinition.spatializerName))
                {
                    AudioListenerExtension newExtension = listener.AddExtension(extensionDefinition.definition.GetExtensionType());
                    if (newExtension != null)
                    {
                        newExtension.audioListener = listener;
                        listener.spatializerExtension = newExtension;

                        WriteExtensionProperties(newExtension, extensionDefinition.definition.GetExtensionType().Name);
                        return newExtension;
                    }
                }
            }

            return null;
        }

        static internal void WriteExtensionProperties(AudioListenerExtension extension, string extensionName)
        {
            if (m_ListenerSpatializerExtensionName == 0)
                m_ListenerSpatializerExtensionName = extensionName;

            for (int listenerIndex = 0; listenerIndex < extension.audioListener.GetNumExtensionProperties(); listenerIndex++)
            {
                if (extension.audioListener.ReadExtensionName(listenerIndex) == m_ListenerSpatializerExtensionName)
                {
                    PropertyName propertyName = extension.audioListener.ReadExtensionPropertyName(listenerIndex);
                    float propertyValue = extension.audioListener.ReadExtensionPropertyValue(listenerIndex);

                    extension.WriteExtensionProperty(propertyName, propertyValue);
                }
            }

        }

        static internal AudioListenerExtension GetSpatializerExtension(AudioListener listener)
        {
            if (listener.spatializerExtension != null)
                return listener.spatializerExtension;

            return null;
        }

        static internal AudioSourceExtension GetSpatializerExtension(AudioSource source)
        {
            return source.spatialize ? source.spatializerExtension : null;
        }

        static internal AudioSourceExtension GetAmbisonicExtension(AudioSource source)
        {
            return source.ambisonicExtension;
        }

        static internal Type GetListenerSpatializerExtensionType()
        {
            foreach (AudioSpatializerExtensionDefinition definition in m_ListenerSpatializerExtensionDefinitions)
            {
                if (AudioSettings.GetSpatializerPluginName() == definition.spatializerName)
                    return definition.definition.GetExtensionType();
            }

            return null;
        }

        static internal Type GetListenerSpatializerExtensionEditorType()
        {
            foreach (AudioSpatializerExtensionDefinition definition in m_ListenerSpatializerExtensionDefinitions)
            {
                if (AudioSettings.GetSpatializerPluginName() == definition.spatializerName)
                    return definition.editorDefinition.GetExtensionType();
            }

            return null;
        }

        static internal Type GetSourceSpatializerExtensionType()
        {
            foreach (AudioSpatializerExtensionDefinition definition in m_SourceSpatializerExtensionDefinitions)
            {
                if (AudioSettings.GetSpatializerPluginName() == definition.spatializerName)
                    return definition.definition.GetExtensionType();
            }

            return null;
        }

        static internal Type GetSourceSpatializerExtensionEditorType()
        {
            foreach (AudioSpatializerExtensionDefinition definition in m_SourceSpatializerExtensionDefinitions)
            {
                if (AudioSettings.GetSpatializerPluginName() == definition.spatializerName)
                    return definition.editorDefinition.GetExtensionType();
            }

            return null;
        }

        static internal Type GetSourceAmbisonicExtensionType()
        {
            foreach (AudioAmbisonicExtensionDefinition definition in m_SourceAmbisonicDecoderExtensionDefinitions)
            {
                if (AudioSettings.GetAmbisonicDecoderPluginName() == definition.ambisonicPluginName)
                    return definition.definition.GetExtensionType();
            }

            return null;
        }

        static internal PropertyName GetSpatializerName()
        {
            return m_SpatializerName;
        }

        static internal PropertyName GetSourceSpatializerExtensionName()
        {
            return m_SpatializerExtensionName;
        }

        static internal PropertyName GetListenerSpatializerExtensionName()
        {
            return m_ListenerSpatializerExtensionName;
        }

        // Add an audio source extension to this manager. This is called when an audio source starts playing, so we can make ExtensionUpdate calls.
        static internal void AddExtensionToManager(AudioSourceExtension extension)
        {
            RegisterBuiltinDefinitions();

            if (extension.m_ExtensionManagerUpdateIndex == -1)
            {
                m_SourceExtensionsToUpdate.Add(extension);
                extension.m_ExtensionManagerUpdateIndex = m_SourceExtensionsToUpdate.Count - 1;
            }
        }

        // Remove this extension from the manager. This is called when an audio source stops playing.
        static internal void RemoveExtensionFromManager(AudioSourceExtension extension)
        {
            int removeIndex = extension.m_ExtensionManagerUpdateIndex;

            if ((removeIndex >= 0) && (removeIndex < m_SourceExtensionsToUpdate.Count))
            {
                int lastIndex = m_SourceExtensionsToUpdate.Count - 1;
                m_SourceExtensionsToUpdate[removeIndex] = m_SourceExtensionsToUpdate[lastIndex];
                m_SourceExtensionsToUpdate[removeIndex].m_ExtensionManagerUpdateIndex = removeIndex;
                m_SourceExtensionsToUpdate.RemoveAt(lastIndex);
            }

            extension.m_ExtensionManagerUpdateIndex = -1;
        }

        // This manager Update function calls ExtensionUpdate for all extensions on playing audio sources. It also goes through part of the list of
        // extensions to determine which ones have stopped and should be removed. The stop check is fairly expensive, so we handle it separately and lazily to
        // help keep ExtensionUpdate very cheap. Each extension's OnDestroy function is a fail-safe and will remove the extension from the manager's list so
        // we are never handling destroyed extensions here.
        static internal void Update()
        {
            RegisterBuiltinDefinitions();


            AudioListener listener = GetAudioListener() as AudioListener;
            if (listener != null)
            {
                AudioListenerExtension extension = AddSpatializerExtension(listener);
                if (extension != null)
                    extension.ExtensionUpdate();
            }

            for (int i = 0; i < m_SourceExtensionsToUpdate.Count; i++)
                m_SourceExtensionsToUpdate[i].ExtensionUpdate();

            m_NextStopIndex = (m_NextStopIndex >= m_SourceExtensionsToUpdate.Count) ? 0 : m_NextStopIndex;

            // Perform stop checks on roughly 1/8th of the list of playing audio source extensions. We're using that equation instead of a fixed maximum like 10 so
            // that we don't get into a situation where the game is playing more sounds every frame than we are stop-checking.
            int numStopChecks = (m_SourceExtensionsToUpdate.Count > 0) ? 1 + (m_SourceExtensionsToUpdate.Count / 8) : 0;
            for (int i = 0; i < numStopChecks; i++)
            {
                AudioSourceExtension extension = m_SourceExtensionsToUpdate[m_NextStopIndex];
                if ((extension.audioSource == null) || !extension.audioSource.enabled || !extension.audioSource.isPlaying)
                {
                    extension.Stop();
                    RemoveExtensionFromManager(extension);
                }
                else
                {
                    m_NextStopIndex++;
                    m_NextStopIndex = (m_NextStopIndex >= m_SourceExtensionsToUpdate.Count) ? 0 : m_NextStopIndex;
                }
            }
        }

        // This function gets the extension get ready to play and adds it to the list of extensions that need ExtensionUpdate callbacks.
        static internal void GetReadyToPlay(AudioSourceExtension extension)
        {
            if (extension != null)
            {
                extension.Play();
                AddExtensionToManager(extension);
            }
        }

        // This is where we register our built-in spatializer extensions.
        static private void RegisterBuiltinDefinitions()
        {
            bool bRegisterAllDefinitions = true;

            if (!m_BuiltinDefinitionsRegistered)
            {
                if (bRegisterAllDefinitions || (AudioSettings.GetSpatializerPluginName() == "GVR Audio Spatializer"))
                {
                }

                if (bRegisterAllDefinitions || (AudioSettings.GetAmbisonicDecoderPluginName() == "GVR Audio Spatializer"))
                {
                }

                m_BuiltinDefinitionsRegistered = true;
            }
        }

        static private bool RegisterListenerSpatializerDefinition(string spatializerName, AudioExtensionDefinition extensionDefinition, AudioExtensionDefinition editorDefinition)
        {
            foreach (AudioSpatializerExtensionDefinition definition in m_ListenerSpatializerExtensionDefinitions)
            {
                if (spatializerName == definition.spatializerName)
                {
                    Debug.Log("RegisterListenerSpatializerDefinition failed for " + extensionDefinition.GetExtensionType() + ". We only allow one audio listener extension to be registered for each spatializer.");
                    return false;
                }
            }

            AudioSpatializerExtensionDefinition newDefinition = new AudioSpatializerExtensionDefinition(spatializerName, extensionDefinition, editorDefinition);
            m_ListenerSpatializerExtensionDefinitions.Add(newDefinition);

            return true;
        }

        static private bool RegisterSourceSpatializerDefinition(string spatializerName, AudioExtensionDefinition extensionDefinition, AudioExtensionDefinition editorDefinition)
        {
            foreach (AudioSpatializerExtensionDefinition definition in m_SourceSpatializerExtensionDefinitions)
            {
                if (spatializerName == definition.spatializerName)
                {
                    Debug.Log("RegisterSourceSpatializerDefinition failed for " + extensionDefinition.GetExtensionType() + ". We only allow one audio source extension to be registered for each spatializer.");
                    return false;
                }
            }

            AudioSpatializerExtensionDefinition newDefinition = new AudioSpatializerExtensionDefinition(spatializerName, extensionDefinition, editorDefinition);
            m_SourceSpatializerExtensionDefinitions.Add(newDefinition);

            return true;
        }

        static private bool RegisterSourceAmbisonicDefinition(string ambisonicDecoderName, AudioExtensionDefinition extensionDefinition)
        {
            foreach (AudioAmbisonicExtensionDefinition definition in m_SourceAmbisonicDecoderExtensionDefinitions)
            {
                if (ambisonicDecoderName == definition.ambisonicPluginName)
                {
                    Debug.Log("RegisterSourceAmbisonicDefinition failed for " + extensionDefinition.GetExtensionType() + ". We only allow one audio source extension to be registered for each ambisonic decoder.");
                    return false;
                }
            }

            AudioAmbisonicExtensionDefinition newDefinition = new AudioAmbisonicExtensionDefinition(ambisonicDecoderName, extensionDefinition);
            m_SourceAmbisonicDecoderExtensionDefinitions.Add(newDefinition);

            return true;
        }
    }
}
