// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine.Bindings;

namespace UnityEditor.Search
{
    /// <summary>
    /// Represents the action to perform at the end of a transaction.
    /// </summary>
    [NativeType(Header = "Modules/QuickSearch/LMDB/LMDBTransactionEndAction.h")]
    enum LMDBTransactionEndAction : byte
    {
        /// <summary>
        /// Commit the transaction if there is no error.
        /// </summary>
        Commit,

        /// <summary>
        /// Abort the transaction.
        /// </summary>
        Abort
    }
}
