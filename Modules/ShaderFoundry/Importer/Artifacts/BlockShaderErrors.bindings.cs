// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Bindings;

namespace UnityEditor
{
    namespace ShaderFoundry
    {
        [NativeHeader("Modules/ShaderFoundry/Importer/Artifacts/BlockShaderErrors.h")]
        [NativeClass("ShaderFoundry::BlockShaderErrors")]
        internal sealed partial class BlockShaderErrors : UnityEngine.Object
        {
            [NativeHeader("Modules/ShaderFoundry/Importer/Artifacts/BlockShaderErrors.h")]
            public struct Error
            {
                internal string message;
                internal string filePath;
                internal UInt32 startLine;
                internal UInt32 startChar;
                internal ErrorSeverity severity;

                public string Message => message;
                public string FilePath => filePath;
                public UInt32 Line => startLine;
                public UInt32 Character => startChar;
                public bool IsWarning => severity == ErrorSeverity.kWarning;
            };

            // The following pair of functions are used to work around the fact that Error is a non-blittable struct,
            // so returning an Error[] from a native function is unsupported.
            [NativeMethod(IsThreadSafe = true)] private extern UInt64 GetErrorCountFromManaged();
            [NativeMethod(IsThreadSafe = true)] private extern void GetErrorsFromManaged([Out] Error[] errors);
            private IEnumerable<Error> GetErrors()
            {
                UInt64 errorCount = GetErrorCountFromManaged();
                if (errorCount > 0)
                {
                    Error[] errors = new Error[errorCount];
                    GetErrorsFromManaged(errors);
                    return errors;
                }
                return new List<Error>();
            }

            [NativeMethod("HasErrors", IsThreadSafe = true)]
            internal extern bool Internal_HasErrors();

            public bool HasErrors => Internal_HasErrors();
            public IEnumerable<Error> Errors => GetErrors();
        }
    }
}
