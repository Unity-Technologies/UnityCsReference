// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UnityEngine.Serialization;

// Managed serialization V2 PropertyName dynamic handler (doc §2.4). The
// editor persists the resolved name as a framed string, reading the whole
// struct so conflictIndex disambiguates the id, with null or unregistered
// names persisting as the empty string. The native-test-resources image, like
// the player, writes the id as a framed decimal string.
internal static unsafe partial class SerializationBackendManagedCommands
{
    private static unsafe void V2WritePropertyName(ref byte field, NativeBufferContext* ctx, ref BufferDataStager stager, ulong userData, ulong userData2)
    {
        // userData is serializesAsId, stamped per build variant at compose
        // time: game-release persists the decimal id, plain editor signatures
        // persist the resolved name.
        if (userData != 0)
        {
            int id = Unsafe.ReadUnaligned<int>(ref field);
            V2WriteFramedDecimalInt32(ctx, id, ref stager);
        }
        else
        {
            PropertyName pn = Unsafe.ReadUnaligned<PropertyName>(ref field);
            string s = PropertyNameUtils.StringFromPropertyName(pn);
            V2WriteFramedString(ctx, (s ?? string.Empty).AsSpan(), ref stager);
        }
    }

    // Read (M7c). The serialized form follows the compose-time signature:
    // plain editor signatures persist the resolved name, game-release
    // signatures the decimal id. The command's readUserData carries the mode.
    private static unsafe void V2ReadPropertyName(ref byte field, byte* cmdRaw, NativeReadBufferContext* ctx)
    {
        var cmd = (V2CmdExternalDynamic*)cmdRaw;
        if (cmd->readUserData != 0)
        {
            // Game-release read signature: the field persisted its decimal id.
            PropertyName pnId = new PropertyName(V2ReadFramedDecimalInt32(ctx));
            Unsafe.WriteUnaligned(ref field, pnId);
        }
        else
        {
            // The ctor resolves conflictIndex from the name, matching the native read.
            PropertyName pn = new PropertyName(V2ReadFramedStringGuarded(ctx));
            Unsafe.WriteUnaligned(ref field, pn);
        }
    }
}
