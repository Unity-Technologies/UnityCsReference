// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

// Test-only static field fixtures for the ALC leak detector suite
// (Modules/Scripting/Tests/UTFTests/ALCLeakDetector). They live here, in a long-living module assembly
// that survives domain reload, rather than in the reloadable test project itself - a static field
// declared in a reloadable assembly is wiped on reload along with the rest of that assembly's
// statics, which would defeat the point of these fixtures.


using Unity.Scripting.LifecycleManagement;

namespace UnityEngine
{
    internal class StaticFieldUtility
    {
        [NoAutoStaticsCleanup]
        public static object StaticObjectField;

        [NoAutoStaticsCleanup]
        [System.ThreadStatic]
        public static object ThreadStaticObjectField;

        public struct ValueTypeWrapper
        {
            public object ObjectField;
        }

        [NoAutoStaticsCleanup]
        public static ValueTypeWrapper StaticValueTypeField;

        // Struct containing a struct containing a reference, to cover the leak detector's
        // value-type static walk when the reference is nested one level deeper than
        // ValueTypeWrapper covers above.
        public struct NestedValueTypeWrapper
        {
            public ValueTypeWrapper Inner;
        }

        [NoAutoStaticsCleanup]
        public static NestedValueTypeWrapper StaticNestedValueTypeField;
    }

    internal class GenericStaticFieldUtility<T>
    {
        [NoAutoStaticsCleanup]
        public static T StaticObjectField;

        [NoAutoStaticsCleanup]
        [System.ThreadStatic]
        public static T ThreadStaticObjectField;

        public struct ValueTypeWrapper
        {
            public T ObjectField;
        }

        [NoAutoStaticsCleanup]
        public static ValueTypeWrapper StaticValueTypeField;
    }
}

