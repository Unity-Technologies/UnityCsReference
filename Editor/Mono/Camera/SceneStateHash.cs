// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEngine.Scripting;

namespace UnityEditor.Experimental.Rendering
{
    [RequiredByNativeCode]
    public struct SceneStateHash : IEquatable<SceneStateHash>
    {
        readonly Hash128 m_SceneObjectsHash;
        readonly Hash128 m_SkySettingsHash;
        readonly Hash128 m_AmbientProbeHash;

        public Hash128 sceneObjectsHash => m_SceneObjectsHash;
        public Hash128 skySettingsHash => m_SkySettingsHash;
        public Hash128 ambientProbeHash => m_AmbientProbeHash;

        public SceneStateHash(
            Hash128 sceneObjectsHash,
            Hash128 skySettingsHash,
            Hash128 ambientProbeHash
            )
        {
            m_SceneObjectsHash = sceneObjectsHash;
            m_SkySettingsHash = skySettingsHash;
            m_AmbientProbeHash = ambientProbeHash;
        }

        public bool Equals(SceneStateHash other)
        {
            return sceneObjectsHash.Equals(other.sceneObjectsHash)
                && skySettingsHash.Equals(other.skySettingsHash)
                && ambientProbeHash.Equals(other.ambientProbeHash);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is SceneStateHash
                && Equals((SceneStateHash)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return sceneObjectsHash.GetHashCode()
                    ^ skySettingsHash.GetHashCode()
                    ^ ambientProbeHash.GetHashCode();
            }
        }
    }
}
