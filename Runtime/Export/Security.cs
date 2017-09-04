// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Reflection;
using System.Collections.Generic;
using Mono.Security;
using Mono.Security.Cryptography;
using UnityEngine.Scripting;

namespace UnityEngine
{
    public sealed partial class Security
    {
        static readonly string kSignatureExtension = ".signature";

        [RequiredByNativeCode]
        internal static bool VerifySignature(string file, byte[] publicKey)
        {
            try
            {
                string signature = file + kSignatureExtension;
                if (!File.Exists(signature))
                    return false;

                using (var provider = new RSACryptoServiceProvider())
                {
                    provider.ImportCspBlob(publicKey);
                    using (var sha1 = new SHA1CryptoServiceProvider())
                        return provider.VerifyData(File.ReadAllBytes(file), sha1, File.ReadAllBytes(signature));
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            return false;
        }


        [Obsolete("This was an internal method which is no longer used", true)]
        public static Assembly LoadAndVerifyAssembly(byte[] assemblyData, string authorizationKey)
        {
            return null;
        }

        [Obsolete("This was an internal method which is no longer used", true)]
        public static Assembly LoadAndVerifyAssembly(byte[] assemblyData)
        {
            return null;
        }
    }

    public static class Types
    {
        [Obsolete("This was an internal method which is no longer used", true)]
        public static Type GetType(string typeName, string assemblyName)
        {
            return null;
        }
    }
}
