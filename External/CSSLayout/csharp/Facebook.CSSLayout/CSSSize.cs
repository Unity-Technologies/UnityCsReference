/**
 * Copyright (c) 2014-present, Facebook, Inc.
 * All rights reserved.
 *
 * This source code is licensed under the BSD-style license found in the
 * LICENSE file in the root directory of this source tree. An additional grant
 * of patent rights can be found in the PATENTS file in the same directory.
 */

using System.Runtime.InteropServices;

// BEGIN_UNITY @joce 11-07-2016 CompileForC#Bindings
//namespace Facebook.CSSLayout
namespace UnityEngine.CSSLayout
// END_UNITY
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct CSSSize
    {
        public float width;
        public float height;
    }
}
