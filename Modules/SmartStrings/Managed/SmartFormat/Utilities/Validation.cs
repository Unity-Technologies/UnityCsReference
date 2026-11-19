// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//
// Copyright SmartFormat Project maintainers and contributors.
// Licensed under the MIT license.
//

using System;

namespace Unity.SmartStrings.Utilities;

internal static class Validation
{
    static readonly char[] k_Valid = new[] { '|', ',', '~' };

    public static char GetValidSplitCharOrThrow(char toCheck)
    {
        return toCheck == k_Valid[0] || toCheck == k_Valid[1] || toCheck == k_Valid[2]
            ? toCheck
            : throw new ArgumentException($"Only '{k_Valid[0]}', '{k_Valid[1]}' and '{k_Valid[2]}' are valid split chars.");
    }
}
