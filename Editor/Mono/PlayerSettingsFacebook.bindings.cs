// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using UnityEngine.Bindings;

namespace UnityEditor
{
    public partial class PlayerSettings : UnityEngine.Object
    {
        [Obsolete("Facebook support was removed in 2019.3", true)]
        public partial class Facebook
        {
            public static string sdkVersion
            {
                get { throw new NotImplementedException("Facebook support was removed in 2019.3"); }
                set { throw new NotImplementedException("Facebook support was removed in 2019.3"); }
            }

            public static string appId
            {
                get { throw new NotImplementedException("Facebook support was removed in 2019.3"); }
                set { throw new NotImplementedException("Facebook support was removed in 2019.3"); }
            }

            public static bool useCookies
            {
                get { throw new NotImplementedException("Facebook support was removed in 2019.3"); }
                set { throw new NotImplementedException("Facebook support was removed in 2019.3"); }
            }

            internal static bool useLogging
            {
                get { throw new NotImplementedException("Facebook support was removed in 2019.3"); }
                set { throw new NotImplementedException("Facebook support was removed in 2019.3"); }
            }

            public static bool useStatus
            {
                get { throw new NotImplementedException("Facebook support was removed in 2019.3"); }
                set { throw new NotImplementedException("Facebook support was removed in 2019.3"); }
            }

            internal static bool useXfbml
            {
                get { throw new NotImplementedException("Facebook support was removed in 2019.3"); }
                set { throw new NotImplementedException("Facebook support was removed in 2019.3"); }
            }

            public static bool useFrictionlessRequests
            {
                get { throw new NotImplementedException("Facebook support was removed in 2019.3"); }
                set { throw new NotImplementedException("Facebook support was removed in 2019.3"); }
            }
        }
    }
}
