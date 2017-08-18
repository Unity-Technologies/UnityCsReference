// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
namespace EmbeddedScriptedObjectsTests
{
    public class DummyClass
    {
        public int Attribute1 = 1;
        public int GetValue() { return Attribute1; }
        public void SetValue(int val) { Attribute1 = val; }
    }
}
