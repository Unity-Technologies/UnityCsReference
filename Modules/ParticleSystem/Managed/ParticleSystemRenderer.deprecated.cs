// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEngine
{
    [Flags, Obsolete("ParticleSystemVertexStreams is deprecated. Please use ParticleSystemVertexStream instead.", false)]
    public enum ParticleSystemVertexStreams
    {
        Position = 1 << 0,
        Normal = 1 << 1,
        Tangent = 1 << 2,
        Color = 1 << 3,
        UV = 1 << 4,
        UV2BlendAndFrame = 1 << 5,
        CenterAndVertexID = 1 << 6,
        Size = 1 << 7,
        Rotation = 1 << 8,
        Velocity = 1 << 9,
        Lifetime = 1 << 10,
        Custom1 = 1 << 11,
        Custom2 = 1 << 12,
        Random = 1 << 13,
        None = 0,
        All = 0x7fffffff
    }

    partial class ParticleSystemRenderer
    {
        [Obsolete("EnableVertexStreams is deprecated.Use SetActiveVertexStreams instead.", false)]
        public void EnableVertexStreams(ParticleSystemVertexStreams streams) { Internal_SetVertexStreams(streams, true); }
        [Obsolete("DisableVertexStreams is deprecated.Use SetActiveVertexStreams instead.", false)]
        public void DisableVertexStreams(ParticleSystemVertexStreams streams) { Internal_SetVertexStreams(streams, false); }
        [Obsolete("AreVertexStreamsEnabled is deprecated.Use GetActiveVertexStreams instead.", false)]
        public bool AreVertexStreamsEnabled(ParticleSystemVertexStreams streams) { return Internal_GetEnabledVertexStreams(streams) == streams; }
        [Obsolete("GetEnabledVertexStreams is deprecated.Use GetActiveVertexStreams instead.", false)]
        public ParticleSystemVertexStreams GetEnabledVertexStreams(ParticleSystemVertexStreams streams) { return Internal_GetEnabledVertexStreams(streams); }

        [Obsolete("Internal_SetVertexStreams is deprecated.Use SetActiveVertexStreams instead.", false)]
        internal void Internal_SetVertexStreams(ParticleSystemVertexStreams streams, bool enabled)
        {
            List<ParticleSystemVertexStream> streamList = new List<ParticleSystemVertexStream>(activeVertexStreamsCount);
            GetActiveVertexStreams(streamList);

            if (enabled)
            {
                if ((streams & ParticleSystemVertexStreams.Position) != 0) { if (!streamList.Contains(ParticleSystemVertexStream.Position)) { streamList.Add(ParticleSystemVertexStream.Position); } }
                if ((streams & ParticleSystemVertexStreams.Normal) != 0) { if (!streamList.Contains(ParticleSystemVertexStream.Normal)) { streamList.Add(ParticleSystemVertexStream.Normal); } }
                if ((streams & ParticleSystemVertexStreams.Tangent) != 0) { if (!streamList.Contains(ParticleSystemVertexStream.Tangent)) { streamList.Add(ParticleSystemVertexStream.Tangent); } }
                if ((streams & ParticleSystemVertexStreams.Color) != 0) { if (!streamList.Contains(ParticleSystemVertexStream.Color)) { streamList.Add(ParticleSystemVertexStream.Color); } }
                if ((streams & ParticleSystemVertexStreams.UV) != 0) { if (!streamList.Contains(ParticleSystemVertexStream.UV)) { streamList.Add(ParticleSystemVertexStream.UV); } }
                if ((streams & ParticleSystemVertexStreams.UV2BlendAndFrame) != 0) { if (!streamList.Contains(ParticleSystemVertexStream.UV2)) { streamList.Add(ParticleSystemVertexStream.UV2); streamList.Add(ParticleSystemVertexStream.AnimBlend); streamList.Add(ParticleSystemVertexStream.AnimFrame); } }
                if ((streams & ParticleSystemVertexStreams.CenterAndVertexID) != 0) { if (!streamList.Contains(ParticleSystemVertexStream.Center)) { streamList.Add(ParticleSystemVertexStream.Center); streamList.Add(ParticleSystemVertexStream.VertexID); } }
                if ((streams & ParticleSystemVertexStreams.Size) != 0) { if (!streamList.Contains(ParticleSystemVertexStream.SizeXYZ)) { streamList.Add(ParticleSystemVertexStream.SizeXYZ); } }
                if ((streams & ParticleSystemVertexStreams.Rotation) != 0) { if (!streamList.Contains(ParticleSystemVertexStream.Rotation3D)) { streamList.Add(ParticleSystemVertexStream.Rotation3D); } }
                if ((streams & ParticleSystemVertexStreams.Velocity) != 0) { if (!streamList.Contains(ParticleSystemVertexStream.Velocity)) { streamList.Add(ParticleSystemVertexStream.Velocity); } }
                if ((streams & ParticleSystemVertexStreams.Lifetime) != 0) { if (!streamList.Contains(ParticleSystemVertexStream.AgePercent)) { streamList.Add(ParticleSystemVertexStream.AgePercent); streamList.Add(ParticleSystemVertexStream.InvStartLifetime); } }
                if ((streams & ParticleSystemVertexStreams.Custom1) != 0) { if (!streamList.Contains(ParticleSystemVertexStream.Custom1XYZW)) { streamList.Add(ParticleSystemVertexStream.Custom1XYZW); } }
                if ((streams & ParticleSystemVertexStreams.Custom2) != 0) { if (!streamList.Contains(ParticleSystemVertexStream.Custom2XYZW)) { streamList.Add(ParticleSystemVertexStream.Custom2XYZW); } }
                if ((streams & ParticleSystemVertexStreams.Random) != 0) { if (!streamList.Contains(ParticleSystemVertexStream.StableRandomXYZ)) { streamList.Add(ParticleSystemVertexStream.StableRandomXYZ); streamList.Add(ParticleSystemVertexStream.VaryingRandomX); } }
            }
            else
            {
                if ((streams & ParticleSystemVertexStreams.Position) != 0) { streamList.Remove(ParticleSystemVertexStream.Position); }
                if ((streams & ParticleSystemVertexStreams.Normal) != 0) { streamList.Remove(ParticleSystemVertexStream.Normal); }
                if ((streams & ParticleSystemVertexStreams.Tangent) != 0) { streamList.Remove(ParticleSystemVertexStream.Tangent); }
                if ((streams & ParticleSystemVertexStreams.Color) != 0) { streamList.Remove(ParticleSystemVertexStream.Color); }
                if ((streams & ParticleSystemVertexStreams.UV) != 0) { streamList.Remove(ParticleSystemVertexStream.UV); }
                if ((streams & ParticleSystemVertexStreams.UV2BlendAndFrame) != 0) { streamList.Remove(ParticleSystemVertexStream.UV2); streamList.Remove(ParticleSystemVertexStream.AnimBlend); streamList.Remove(ParticleSystemVertexStream.AnimFrame); }
                if ((streams & ParticleSystemVertexStreams.CenterAndVertexID) != 0) { streamList.Remove(ParticleSystemVertexStream.Center); streamList.Remove(ParticleSystemVertexStream.VertexID); }
                if ((streams & ParticleSystemVertexStreams.Size) != 0) { streamList.Remove(ParticleSystemVertexStream.SizeXYZ); }
                if ((streams & ParticleSystemVertexStreams.Rotation) != 0) { streamList.Remove(ParticleSystemVertexStream.Rotation3D); }
                if ((streams & ParticleSystemVertexStreams.Velocity) != 0) { streamList.Remove(ParticleSystemVertexStream.Velocity); }
                if ((streams & ParticleSystemVertexStreams.Lifetime) != 0) { streamList.Remove(ParticleSystemVertexStream.AgePercent); streamList.Remove(ParticleSystemVertexStream.InvStartLifetime); }
                if ((streams & ParticleSystemVertexStreams.Custom1) != 0) { streamList.Remove(ParticleSystemVertexStream.Custom1XYZW); }
                if ((streams & ParticleSystemVertexStreams.Custom2) != 0) { streamList.Remove(ParticleSystemVertexStream.Custom2XYZW); }
                if ((streams & ParticleSystemVertexStreams.Random) != 0) { streamList.Remove(ParticleSystemVertexStream.StableRandomXYZW); streamList.Remove(ParticleSystemVertexStream.VaryingRandomX); }
            }

            SetActiveVertexStreams(streamList);
        }

        [Obsolete("Internal_GetVertexStreams is deprecated.Use GetActiveVertexStreams instead.", false)]
        internal ParticleSystemVertexStreams Internal_GetEnabledVertexStreams(ParticleSystemVertexStreams streams)
        {
            List<ParticleSystemVertexStream> streamList = new List<ParticleSystemVertexStream>(activeVertexStreamsCount);
            GetActiveVertexStreams(streamList);

            ParticleSystemVertexStreams deprecatedStreams = 0;
            if (streamList.Contains(ParticleSystemVertexStream.Position)) deprecatedStreams |= ParticleSystemVertexStreams.Position;
            if (streamList.Contains(ParticleSystemVertexStream.Normal)) deprecatedStreams |= ParticleSystemVertexStreams.Normal;
            if (streamList.Contains(ParticleSystemVertexStream.Tangent)) deprecatedStreams |= ParticleSystemVertexStreams.Tangent;
            if (streamList.Contains(ParticleSystemVertexStream.Color)) deprecatedStreams |= ParticleSystemVertexStreams.Color;
            if (streamList.Contains(ParticleSystemVertexStream.UV)) deprecatedStreams |= ParticleSystemVertexStreams.UV;
            if (streamList.Contains(ParticleSystemVertexStream.UV2)) deprecatedStreams |= ParticleSystemVertexStreams.UV2BlendAndFrame;
            if (streamList.Contains(ParticleSystemVertexStream.Center)) deprecatedStreams |= ParticleSystemVertexStreams.CenterAndVertexID;
            if (streamList.Contains(ParticleSystemVertexStream.SizeXYZ)) deprecatedStreams |= ParticleSystemVertexStreams.Size;
            if (streamList.Contains(ParticleSystemVertexStream.Rotation3D)) deprecatedStreams |= ParticleSystemVertexStreams.Rotation;
            if (streamList.Contains(ParticleSystemVertexStream.Velocity)) deprecatedStreams |= ParticleSystemVertexStreams.Velocity;
            if (streamList.Contains(ParticleSystemVertexStream.AgePercent)) deprecatedStreams |= ParticleSystemVertexStreams.Lifetime;
            if (streamList.Contains(ParticleSystemVertexStream.Custom1XYZW)) deprecatedStreams |= ParticleSystemVertexStreams.Custom1;
            if (streamList.Contains(ParticleSystemVertexStream.Custom2XYZW)) deprecatedStreams |= ParticleSystemVertexStreams.Custom2;
            if (streamList.Contains(ParticleSystemVertexStream.StableRandomXYZ)) deprecatedStreams |= ParticleSystemVertexStreams.Random;

            return (deprecatedStreams & streams);
        }
    }
}
