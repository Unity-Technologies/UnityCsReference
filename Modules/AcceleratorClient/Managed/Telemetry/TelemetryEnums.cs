// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

#nullable enable
using System;
namespace Unity.AcceleratorClient.Telemetry;

// byte discriminator: 0 reserved sentinel, 1..11 library, 12..63 reserved, 64..127 harness (cast-from-byte).
internal enum OpKind : byte
{
    Reserved = 0,
    Connect = 1,
    Get = 2,
    GetBatch = 3,
    GetToFile = 4,
    Put = 5,
    PutBatch = 6,
    PutFromFile = 7,
    PutFromFiles = 8,
    Delete = 9,
    Dispose = 10,
    ProcessSample = 11,

    // Values 12..63 reserved for future Domain ops and Session events.
}

internal enum Outcome : byte
{
    Reserved = 0,
    Success = 1,
    Failure = 2,

    // value also reused by the harness CancelOrphan OpKind; switch on op before interpreting.
    Cancelled = 3,
    Miss = 4,
}
