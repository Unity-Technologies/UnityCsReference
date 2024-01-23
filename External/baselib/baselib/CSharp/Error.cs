using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Baselib.LowLevel;

namespace Unity.Baselib
{
    internal struct ErrorState
    {
        // Can't use CallerFilePath/CallerLineNumber because UnityEngine.dll is still linking against an old .Net framework.
        //public void ThrowIfFailed([CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
        public void ThrowIfFailed()
        {
            if (ErrorCode != Binding.Baselib_ErrorCode.Success)
                throw new BaselibException(this);
        }

        public Binding.Baselib_ErrorCode ErrorCode => nativeErrorState.code;

        /// <summary>
        /// Native error state object, should only be written by native code.
        /// </summary>
        private Binding.Baselib_ErrorState nativeErrorState;

        public unsafe Binding.Baselib_ErrorState* NativeErrorStatePtr
        {
            get
            {
                fixed (Binding.Baselib_ErrorState* ptr = &nativeErrorState)
                    return ptr;
            }
        }

        /// <summary>
        /// Retrieves a (potentially) platform specific explanation string from the error state.
        /// </summary>
        /// <param name="verbose">
        /// If false, only writes error code type and value.
        /// If true, adds source location if available and error explanation if available (similar to strerror).
        /// </param>
        public string Explain(Binding.Baselib_ErrorState_ExplainVerbosity verbosity = Binding.Baselib_ErrorState_ExplainVerbosity.ErrorType_SourceLocation_Explanation)
        {
            unsafe
            {
                fixed (Binding.Baselib_ErrorState* nativeErrorStatePtr = &nativeErrorState)
                {
                    // Add 1 because querying the length does not contain nullterminator.
                    var length = Binding.Baselib_ErrorState_Explain(nativeErrorStatePtr, null, 0, verbosity) + 1;
                    var nativeExplanationString = Binding.Baselib_Memory_Allocate(new UIntPtr(length));
                    try
                    {
                        Binding.Baselib_ErrorState_Explain(nativeErrorStatePtr, (byte*)nativeExplanationString, length, verbosity);
                        return Marshal.PtrToStringAnsi(nativeExplanationString); // System.Text.Encoding.UTF8.GetString is not supported in DOTS as of writing.
                    }
                    finally
                    {
                        Binding.Baselib_Memory_Free(nativeExplanationString);
                    }
                }
            }
        }
    }


    /// <summary>
    /// Exception thrown when a critical Baselib error was raised.
    /// </summary>
    internal class BaselibException : Exception
    {
        internal BaselibException(ErrorState errorState) : base(errorState.Explain())
        {
            this.errorState = errorState;
        }

        public Binding.Baselib_ErrorCode ErrorCode => errorState.ErrorCode;

        private readonly ErrorState errorState;
    }
}
