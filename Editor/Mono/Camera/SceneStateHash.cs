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
        Hash128 m_SceneObjectsHash;
        Hash128 m_SkySettingsHash;
        Hash128 m_AmbientProbeHash;

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
            var h = new Hash128();
            unsafe
            {
                fixed(Hash128* ptr = &m_SceneObjectsHash) h.Append(ptr, (ulong)sizeof(Hash128));
                fixed(Hash128* ptr = &m_SkySettingsHash) h.Append(ptr, (ulong)sizeof(Hash128));
                fixed(Hash128* ptr = &m_AmbientProbeHash) h.Append(ptr, (ulong)sizeof(Hash128));
            }
            return h.GetHashCode();
        }
    }
}
