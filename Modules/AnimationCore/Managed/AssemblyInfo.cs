// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.CompilerServices;
using Unity.Burst;

// Without direct calls, Burst adds no [RuntimeInitializeOnLoadMethod] to players, which would keep this module and Animation in every build

[assembly: InternalsVisibleTo("Unity.Modules.Animation.Core.Tests.Playmode")]
[assembly: InternalsVisibleTo("Unity.Motion")]
[assembly: InternalsVisibleTo("Unity.Motion.Graph")]
[assembly: InternalsVisibleTo("Unity.Motion.Graph.Tests")]
[assembly: InternalsVisibleTo("Unity.Motion.Tests")]
[assembly: InternalsVisibleTo("Unity.Motion.DefaultGraphPipeline")]
[assembly: InternalsVisibleTo("Unity.Motion.BurstCompatibilityGen")]
[assembly: InternalsVisibleTo("Unity.Motion.Recorder")]
[assembly: InternalsVisibleTo("Unity.Motion.Recorder.Editor")]
[assembly: InternalsVisibleTo("Unity.Motion.Recorder.Tests")]
[assembly: InternalsVisibleTo("Unity.Motion.URPExamples.ECSExamples")]
[assembly: InternalsVisibleTo("Unity.Motion.Samples.MotionPlayer")]
