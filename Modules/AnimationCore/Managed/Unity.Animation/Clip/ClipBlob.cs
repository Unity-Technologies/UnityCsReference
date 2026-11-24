// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Runtime.InteropServices;
using Unity.Entities;
using Unity.IntegerTime;
using UnityEngine;

namespace Unity.Animations
{
    // Byte-exact C# mirror of native mecanim::animation::ClipMuscleConstant and every
    // struct it (transitively) contains. The intent is to reinterpret_cast a pointer to
    // native muscle-clip blob memory (produced by BlobWrite with kBuildNoTargetPlatform in
    // the running editor or player) as a pointer to Clip here, with zero
    // copying.
    //
    // Sources mirrored:
    //   Modules/Animation/mecanim/animation/clipmuscle.h        (ClipMuscleConstant, ValueDelta)
    //   Modules/Animation/mecanim/animation/curvedata.h         (Clip)
    //   Modules/Animation/mecanim/animation/streamedclip.h      (StreamedClip)
    //   Modules/Animation/mecanim/animation/denseclip.h         (DenseClip)
    //   Modules/Animation/mecanim/animation/constantclip.h      (ConstantClip)
    //   Modules/Animation/mecanim/human/human.h                 (HumanPose, HumanGoal)
    //   Modules/Animation/mecanim/human/hand.h                  (HandPose)
    //   Modules/Mathematics/Include/Mathematics/Simd/vec-trs-type.h (trsX)
    //   Modules/Mathematics/Include/Mathematics/Simd/vec-types.h    (float3, float4)
    //   Runtime/Serialize/Blobification/offsetptr.h             (OffsetPtr<T>)
    //
    // Platform assumptions (must hold to keep the cast valid):
    //  * 64-bit process (size_t == 8, void* == 8).
    //  * OffsetPtr is an 8-byte, 8-aligned SInt64 offset on every platform, so the blob has
    //    the same bytes in the editor and in players, which load it as it was saved.
    //  * Native uses clang SIMD: float3 and float4 are 16 bytes with 16-byte alignment
    //    (float3 has one trailing padding lane), and trsX = float3 + float4 + float3 = 48.
    //
    // Every struct below uses [StructLayout(LayoutKind.Explicit, Size = ...)] with a
    // [FieldOffset] on every field so the compiler cannot re-pack anything.

    /// <summary>math::float3 (SIMD): x, y, z and one padding lane.</summary>
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    internal struct Float3
    {
        [FieldOffset(0)]  public float x;
        [FieldOffset(4)]  public float y;
        [FieldOffset(8)]  public float z;
        // [FieldOffset(12)] unused SIMD lane
    }

    /// <summary>math::float4 (SIMD): x, y, z, w.</summary>
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    internal struct Float4
    {
        [FieldOffset(0)]  public float x;
        [FieldOffset(4)]  public float y;
        [FieldOffset(8)]  public float z;
        [FieldOffset(12)] public float w;
    }

    /// <summary>math::trsX = { float3 t; float4 q; float3 s; }.</summary>
    [StructLayout(LayoutKind.Explicit, Size = 48)]
    internal struct TrsX
    {
        [FieldOffset(0)]  public Float3 t;
        [FieldOffset(16)] public Float4 q;
        [FieldOffset(32)] public Float3 s;
    }

    /// <summary>
    /// OffsetPtr&lt;T&gt;:
    ///   SInt64 m_Offset;   // self-relative offset (0 == null)
    /// The template parameter T does not affect layout in native, so this type is not generic.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    internal unsafe struct OffsetPtr
    {
        [FieldOffset(0)] public ulong m_Offset;

        public bool IsNull => m_Offset == 0;

        /// <summary>Resolve the self-relative offset to an absolute pointer.</summary>
        public void* Get()
        {
            if (m_Offset == 0) return null;
            fixed (OffsetPtr* self = &this)
                return (byte*)self + (long)m_Offset;
        }
    }

    /// <summary>mecanim::animation::ValueDelta = { float m_Start; float m_Stop; }.</summary>
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    internal struct ValueDelta
    {
        [FieldOffset(0)] public float m_Start;
        [FieldOffset(4)] public float m_Stop;
    }

    // Note: m_MarkupsMetadata below points at an array of Unity.Motion.GameplayMarkupMetadata
    // (32 bytes: two DiscreteTime longs + StringHash uint + two int offsets + bool +
    // trailing pad), defined in the Motion package which also guards its size.

    /// <summary>
    /// mecanim::human::HumanGoal.
    ///   0..48   m_X (trsX)
    ///  48..52   m_WeightT
    ///  52..56   m_WeightR
    ///  56..64   padding (float3 alignment)
    ///  64..80   m_HintT (float3)
    ///  80..84   m_HintWeightT
    ///  84..96   trailing pad to alignof=16
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 96)]
    internal struct HumanGoal
    {
        [FieldOffset(0)]  public TrsX   m_X;
        [FieldOffset(48)] public float  m_WeightT;
        [FieldOffset(52)] public float  m_WeightR;
        // 56..64 padding
        [FieldOffset(64)] public Float3 m_HintT;
        [FieldOffset(80)] public float  m_HintWeightT;
        // 84..96 tail padding
    }

    /// <summary>
    /// mecanim::hand::HandPose.
    ///   0..48    m_GrabX
    ///  48..128   m_DoFArray[hand::s_DoFCount=20]
    /// 128..132   m_Override
    /// 132..136   m_CloseOpen
    /// 136..140   m_InOut
    /// 140..144   m_Grab
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 144)]
    internal unsafe struct HandPose
    {
        public const int DoFCount = 20; // hand::s_DoFCount = hand::kLastFinger * hand::kLastFingerDoF

        [FieldOffset(0)]   public TrsX  m_GrabX;
        [FieldOffset(48)]  public fixed float m_DoFArray[DoFCount];
        [FieldOffset(128)] public float m_Override;
        [FieldOffset(132)] public float m_CloseOpen;
        [FieldOffset(136)] public float m_InOut;
        [FieldOffset(140)] public float m_Grab;
    }

    /// <summary>
    /// mecanim::human::HumanPose.
    ///    0..48    m_RootX (trsX)
    ///   48..64    m_LookAtPosition (float3)
    ///   64..80    m_LookAtWeight (float4)
    ///   80..464   m_GoalArray[human::kLastGoal=4], each HumanGoal is 96 bytes
    ///  464..608   m_LeftHandPose (HandPose)
    ///  608..752   m_RightHandPose (HandPose)
    ///  752..972   m_DoFArray[human::kLastDoF=55] (float)
    ///  972..976   padding to align next float3 to 16
    ///  976..1312  m_TDoFArray[human::kLastTDoF=21] (float3 each 16 bytes)
    ///
    /// m_GoalArray is inlined as four fields; m_TDoFArray is stored as a raw float buffer
    /// with 21 * 4 = 84 floats (each float3 occupies a 16-byte SIMD slot).
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 1312)]
    internal unsafe struct HumanPose
    {
        public const int GoalCount = 4;   // human::kLastGoal
        public const int DoFCount  = 55;  // human::kLastDoF
        public const int TDoFCount = 21;  // human::kLastTDoF

        [FieldOffset(0)]   public TrsX   m_RootX;
        [FieldOffset(48)]  public Float3 m_LookAtPosition;
        [FieldOffset(64)]  public Float4 m_LookAtWeight;

        [FieldOffset(80)]  public HumanGoal m_Goal0;
        [FieldOffset(176)] public HumanGoal m_Goal1;
        [FieldOffset(272)] public HumanGoal m_Goal2;
        [FieldOffset(368)] public HumanGoal m_Goal3;

        [FieldOffset(464)] public HandPose m_LeftHandPose;
        [FieldOffset(608)] public HandPose m_RightHandPose;

        [FieldOffset(752)] public fixed float m_DoFArray[DoFCount]; // 220 bytes
        // 972..976 padding for float3 alignment
        [FieldOffset(976)] public fixed float m_TDoFArrayData[TDoFCount * 4]; // 84 floats = 336 bytes

        /// <summary>Access one entry of m_GoalArray by index (0..3).</summary>
        public ref HumanGoal GetGoal(int index)
        {
            fixed (HumanGoal* p = &m_Goal0)
                return ref p[index];
        }

        /// <summary>Access one entry of m_TDoFArray by index (0..20).</summary>
        public ref Float3 GetTDoF(int index)
        {
            fixed (float* p = m_TDoFArrayData)
                return ref *(Float3*)(p + index * 4);
        }
    }

    /// <summary>
    /// mecanim::animation::StreamedClip.
    ///   0..4     dataSize
    ///   4..8     padding (OffsetPtr align)
    ///   8..16    data (OffsetPtr)
    ///  16..18    curveCount
    ///  18..20    discreteCurveCount
    ///  20..24    padding (struct align)
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 24)]
    internal struct StreamedClip
    {
        [FieldOffset(0)]  public uint      dataSize;
        // 4..8 padding
        [FieldOffset(8)]  public OffsetPtr data;
        [FieldOffset(16)] public ushort    curveCount;
        [FieldOffset(18)] public ushort    discreteCurveCount;
    }

    /// <summary>
    /// mecanim::animation::DenseClip.
    ///   0..4    m_FrameCount
    ///   4..8    m_CurveCount
    ///   8..12   m_SampleRate
    ///  12..16   m_BeginTime
    ///  16..20   m_SampleArraySize
    ///  20..24   padding
    ///  24..(24 + sizeof(OffsetPtr))   m_SampleArray
    /// Total: 32 bytes.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    internal struct DenseClip
    {
        [FieldOffset(0)]  public int       m_FrameCount;
        [FieldOffset(4)]  public uint      m_CurveCount;
        [FieldOffset(8)]  public float     m_SampleRate;
        [FieldOffset(12)] public float     m_BeginTime;
        [FieldOffset(16)] public uint      m_SampleArraySize;
        // 20..24 padding
        [FieldOffset(24)] public OffsetPtr m_SampleArray;
    }

    /// <summary>
    /// mecanim::animation::ConstantClip.
    ///   0..4    curveCount
    ///   4..8    padding
    ///   8..(8 + sizeof(OffsetPtr))    data
    /// Total: 16 bytes.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    internal struct ConstantClip
    {
        [FieldOffset(0)] public uint      curveCount;
        // 4..8 padding
        [FieldOffset(8)] public OffsetPtr data;
    }

    /// <summary>
    /// mecanim::animation::Clip = { StreamedClip; DenseClip; ConstantClip }.
    ///   0..24  m_StreamedClip
    ///  24..56  m_DenseClip
    ///  56..72  m_ConstantClip
    /// Total: 72 bytes.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 72)]
    internal struct ClipCurves
    {
        [FieldOffset(0)]  public StreamedClip m_StreamedClip;
        [FieldOffset(24)] public DenseClip    m_DenseClip;
        [FieldOffset(56)] public ConstantClip m_ConstantClip;
    }

    /// <summary>
    /// The baked data of an animation clip, such as its start time, length, and whether it loops.
    /// </summary>
    /// <remarks>
    /// Call <see cref="AnimationClipExtensions.ToClipReference(AnimationClip)"/> to get a reference to the clip data of
    /// an animation clip. Convert the reference with <see cref="BlobObjectReference{T}.AsBlobAssetReference"/> to read
    /// the properties of the clip from jobs and Burst-compiled code. Use <see cref="ToTimerState(DiscreteTime)"/> to
    /// create a timer that plays the clip. To sample the clip into an <see cref="AnimationStream"/>, create a
    /// <see cref="ClipSampler"/> instead.
    /// </remarks>
    /// <seealso cref="AnimationClipExtensions"/>
    /// <seealso cref="ClipSampler"/>
    /// <seealso cref="TimerState"/>
    [StructLayout(LayoutKind.Explicit, Size = 2432)]
    public unsafe struct Clip
    {
        // Version of the layout of the blob that the native clip writer produces (AnimationClip::kMuscleClipBlobVersion).
        // Increase it together with the native version when the bytes of a baked clip change, such as the payload of its markups.
        internal const int BlobVersion = 2;

        // Number of curves in the humanoid curve layout; mirrors the muscle curve count in clipmuscle.h.
        internal const int CurveCount = 200;

        [FieldOffset(0)]    internal HumanPose m_DeltaPose;              // 0    .. 1312

        [FieldOffset(1312)] internal TrsX      m_StartX;                 // 1312 .. 1360
        [FieldOffset(1360)] internal TrsX      m_StopX;                  // 1360 .. 1408
        [FieldOffset(1408)] internal TrsX      m_LeftFootStartX;         // 1408 .. 1456
        [FieldOffset(1456)] internal TrsX      m_RightFootStartX;        // 1456 .. 1504

        [FieldOffset(1504)] internal Float3    m_AverageSpeed;           // 1504 .. 1520

        [FieldOffset(1520)] internal OffsetPtr m_Clip;                   // 1520 .. 1528

        [FieldOffset(1528)] internal float     m_StartTime;
        [FieldOffset(1532)] internal float     m_StopTime;
        [FieldOffset(1536)] internal float     m_OrientationOffsetY;
        [FieldOffset(1540)] internal float     m_Level;
        [FieldOffset(1544)] internal float     m_CycleOffset;
        [FieldOffset(1548)] internal float     m_AverageAngularSpeed;

        [FieldOffset(1552)] internal fixed int m_IndexArray[CurveCount]; // 200 * 4 = 800 bytes; ends at 2352

        [FieldOffset(2352)] internal uint      m_ValueArrayCount;
        // 2356..2360 padding for OffsetPtr alignment
        [FieldOffset(2360)] internal OffsetPtr m_ValueArrayDelta;        // 2360 .. 2368, OffsetPtr<ValueDelta>

        [FieldOffset(2368)] internal uint      m_ValueArrayReferencePoseCount;
        // 2372..2376 padding
        [FieldOffset(2376)] internal OffsetPtr m_ValueArrayReferencePose; // 2376 .. 2384, OffsetPtr<float>

        // Native uses `bool` (1 byte). We use byte here to keep the struct blittable and public.
        [FieldOffset(2384)] internal byte m_Mirror;
        [FieldOffset(2385)] internal byte m_LoopTime;
        [FieldOffset(2386)] internal byte m_LoopBlend;
        [FieldOffset(2387)] internal byte m_LoopBlendOrientation;
        [FieldOffset(2388)] internal byte m_LoopBlendPositionY;
        [FieldOffset(2389)] internal byte m_LoopBlendPositionXZ;
        [FieldOffset(2390)] internal byte m_StartAtOrigin;
        [FieldOffset(2391)] internal byte m_KeepOriginalOrientation;
        [FieldOffset(2392)] internal byte m_KeepOriginalPositionY;
        [FieldOffset(2393)] internal byte m_KeepOriginalPositionXZ;
        [FieldOffset(2394)] internal byte m_HeightFromFeet;
        // 2395..2396 pad to align m_MarkupsMetadataCount (uint) to 4

        [FieldOffset(2396)] internal uint      m_MarkupsMetadataCount;
        // 2400 is already aligned to 8 (OffsetPtr alignment)
        [FieldOffset(2400)] internal OffsetPtr m_MarkupsMetadata;         // 2400..2408, OffsetPtr<GameplayMarkupMetadataBlob>

        [FieldOffset(2408)] internal uint      m_MarkupsDataCount;
        // 2412..2416 padding for OffsetPtr alignment
        [FieldOffset(2416)] internal OffsetPtr m_MarkupsData;             // 2416..2424, OffsetPtr<byte>
        // 2424..2432 trailing padding for 16-byte struct alignment

        /// <summary>The time on the clip curves where the clip starts, in seconds.</summary>
        /// <remarks>
        /// The start time isn't always 0. For example, a clip that you set with
        /// <c>AnimationUtility.SetAnimationClipSettings</c> can start later on its curves. A clip that
        /// the model importer creates from a take starts at 0, because the importer shifts the curves of the take to
        /// begin at 0. The times that you pass to a <see cref="ClipSampler"/> are times on the clip curves, between
        /// <see cref="StartTime"/> and <see cref="StopTime"/>, so don't assume that a clip plays from 0.
        /// </remarks>
        /// <seealso cref="StopTime"/>
        /// <seealso cref="Length"/>
        public float StartTime => m_StartTime;

        /// <summary>The time on the clip curves where the clip ends, in seconds.</summary>
        /// <remarks>
        /// The clip stops here even if its curves have keys after this time. <c>AnimationClip.length</c> reports the end
        /// of the curves instead, so it can be later than the stop time.
        /// </remarks>
        /// <seealso cref="StartTime"/>
        /// <seealso cref="Length"/>
        public float StopTime => m_StopTime;

        /// <summary>The duration of the clip in seconds, from its start time to its stop time.</summary>
        /// <remarks>
        /// This is the duration that a <see cref="ClipSampler"/> plays, and the size of the
        /// <see cref="TimerState.Range"/> of the timer that <see cref="ToTimerState(DiscreteTime)"/> returns.
        ///
        /// <c>AnimationClip.length</c> is the time at the end of the curves of the clip, not a duration, so it can
        /// differ from this value. The two values are the same only when <see cref="StartTime"/> is 0 and the stop
        /// time is the end of the last curve. They differ in the following cases:
        ///
        ///- <see cref="StartTime"/> isn't 0. <c>AnimationClip.length</c> is then at least the stop time, while this
        /// value is the stop time minus the start time.
        ///- The curves of the clip go past <see cref="StopTime"/>. <c>AnimationClip.length</c> reports the end of the
        /// curves, but the clip stops at <see cref="StopTime"/>.
        ///- In a player build, Unity can strip the curves of a clip. <c>AnimationClip.length</c> is then the stop time.
        ///
        /// When you drive a <see cref="ClipSampler"/>, use <see cref="StartTime"/>, <see cref="StopTime"/>, and this
        /// property instead of <c>AnimationClip.length</c>.
        /// </remarks>
        /// <seealso cref="StartTime"/>
        /// <seealso cref="StopTime"/>
        public float Length => m_StopTime - m_StartTime;

        /// <summary>Whether the clip loops, as set by **Loop Time** in the clip settings.</summary>
        public bool LoopTime => m_LoopTime != 0;

        /// <summary>
        /// Creates a timer for the clip, positioned at a time from the start of the clip.
        /// </summary>
        /// <remarks>
        /// The timer is a clip timer. Its <see cref="TimerState.Time"/> is the time on the clip curves, including the
        /// cycle offset. Its <see cref="TimerState.LoopCount"/> counts the wraps of the clip. Pass it to
        /// <see cref="TimerStateExtensions.Update"/> every frame to play the clip.
        ///
        /// The timer has the following values:
        ///
        ///- <see cref="TimerState.Range"/> goes from <see cref="StartTime"/> to <see cref="StopTime"/>.
        ///- <see cref="TimerState.WrapMode"/> is <see cref="UnityEngine.WrapMode.Loop"/> if <see cref="LoopTime"/> is
        /// `true`, and <see cref="UnityEngine.WrapMode.ClampForever"/> otherwise.
        ///- <see cref="TimerState.CycleOffset"/> is the **Cycle Offset** of the clip if the clip loops, and 0
        /// otherwise.
        ///- <see cref="TimerState.TimeScale"/> is 1, and the timer is playing.
        ///- <see cref="TimerState.PreviousTime"/> is the time where the clip starts, which is the start of the range
        /// plus the cycle offset when the clip loops.
        ///
        /// The timer doesn't snap the time to the frames of the clip. To play a clip backward, set a negative
        /// <see cref="TimerState.TimeScale"/> on the timer.
        /// </remarks>
        /// <param name="time">The time, in seconds, to advance the timer by from the start of the clip. A negative time
        /// goes backward.</param>
        /// <returns>The timer of the clip, advanced by <paramref name="time"/>.</returns>
        public readonly TimerState ToTimerState(DiscreteTime time) => ToTimerState(time, WrapMode.Default);

        // Default takes the wrap mode from Loop Time, any other mode is kept
        internal readonly WrapMode ResolveWrapMode(WrapMode mode) =>
            mode != WrapMode.Default ? mode : (m_LoopTime != 0 ? WrapMode.Loop : WrapMode.ClampForever);

        // A caller wrap mode other than Default replaces the one from the clip, and only Loop keeps the cycle offset
        internal readonly TimerState ToTimerState(DiscreteTime time, WrapMode wrapMode)
        {
            var resolvedWrapMode = ResolveWrapMode(wrapMode);
            var timer = TimerState.Create(
                new DiscreteTimeInterval(new DiscreteTime(m_StartTime), new DiscreteTime(m_StopTime)),
                resolvedWrapMode,
                resolvedWrapMode == WrapMode.Loop ? m_CycleOffset : 0f);

            timer.Update(time);

            // The first update of a timer that starts at 0 gathers the events at the start of the clip
            timer.Reset = (byte)(time == DiscreteTime.Zero ? 1 : 0);
            return timer;
        }
    }

    /// <summary>
    /// Compile-time-ish assertions that every mirror struct has the expected native size.
    /// Call this once (e.g. from a test or a system OnCreate) to catch layout drift early.
    /// </summary>
    internal static class ClipLayout
    {
        public static unsafe void AssertLayout()
        {
            AssertSize(sizeof(Float3),             16,   nameof(Float3));
            AssertSize(sizeof(Float4),             16,   nameof(Float4));
            AssertSize(sizeof(TrsX),               48,   nameof(TrsX));
            AssertSize(sizeof(OffsetPtr),                  8,    nameof(OffsetPtr));
            AssertSize(sizeof(ValueDelta),                 8,    nameof(ValueDelta));
            AssertSize(sizeof(MarkupMetadataBlob),         32,   nameof(MarkupMetadataBlob));
            AssertSize(sizeof(HumanGoal),                  96,   nameof(HumanGoal));
            AssertSize(sizeof(HandPose),                   144,  nameof(HandPose));
            AssertSize(sizeof(HumanPose),                  1312, nameof(HumanPose));
            AssertSize(sizeof(StreamedClip),               24,   nameof(StreamedClip));
            AssertSize(sizeof(DenseClip),                  32,   nameof(DenseClip));
            AssertSize(sizeof(ConstantClip),               16,   nameof(ConstantClip));
            AssertSize(sizeof(ClipCurves),                 72,   nameof(ClipCurves));
            AssertSize(sizeof(Clip),                       2432, nameof(Clip));
        }

        static void AssertSize(int actual, int expected, string name)
        {
            if (actual != expected)
                throw new System.InvalidOperationException(
                    $"{name} sizeof mismatch: got {actual}, expected {expected}. " +
                    "Native mecanim layout has drifted; update ClipBlob.cs.");
        }
    }
}
