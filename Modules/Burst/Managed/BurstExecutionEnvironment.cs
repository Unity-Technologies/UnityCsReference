// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Burst
{
        /// <summary>
        /// Represents the types of compiled code that are run on the current thread.
        /// </summary>
        public enum BurstExecutionEnvironment
        {
            /// <summary>
            /// Use the default (aka FloatMode specified via Compile Attribute - <see cref="FloatMode"/>
            /// </summary>
            Default=0,
            /// <summary>
            /// Override the specified float mode and run the non deterministic version
            /// </summary>
            NonDeterministic=0,
            /// <summary>
            /// Override the specified float mode and run the deterministic version
            /// </summary>
            Deterministic=1,
        }

}
