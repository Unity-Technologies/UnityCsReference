// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License


using System;
using Unity.Jobs.LowLevel.Unsafe;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Unity.UNode.Audio")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Unity.UNode.Audio.Tests")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Unity.Audio")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Unity.Audio.Tests")]

namespace Unity.Experimental.Audio
{
    [JobProducerType(typeof(AudioJobExtensions.AudioJobStructProduce< , , >))]
    internal interface IAudioJob<TParams, TProvs>
        where TParams : struct, IConvertible
        where TProvs  : struct, IConvertible
    {
        void Init(ParameterData<TParams> parameters);
        void Execute(ref ExecuteContext<TParams, TProvs> context);
    }
}

