// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

namespace Unity.Burst
{
    /// <summary>
    /// Specifies the possible diagnostic IDs.
    /// </summary>
    internal
    enum DiagnosticId
    {
        // General
        ERR_InternalCompilerErrorInBackend = 100,
        ERR_InternalCompilerErrorInFunction = 101,
        ERR_InternalCompilerErrorInInstruction = 102,

        // ILBuilder
        ERR_OnlyStaticMethodsAllowed = 1000,
        ERR_UnableToAccessManagedMethod = 1001,
        ERR_UnableToFindInterfaceMethod = 1002,

        // ILCompiler
        ERR_UnexpectedEmptyMethodBody = 1003,
        ERR_ManagedArgumentsNotSupported = 1004,
        // ERR_TryConstructionNotSupported = 1005, // not used anymore
        ERR_CatchConstructionNotSupported = 1006,
        ERR_CatchAndFilterConstructionNotSupported = 1007,
        ERR_LdfldaWithFixedArrayExpected = 1008,
        ERR_PointerExpected = 1009,
        ERR_LoadingFieldFromManagedObjectNotSupported = 1010,
        ERR_LoadingFieldWithManagedTypeNotSupported = 1011,
        ERR_LoadingArgumentWithManagedTypeNotSupported = 1012,
        ERR_CallingBurstDiscardMethodWithReturnValueNotSupported = 1015,
        ERR_CallingManagedMethodNotSupported = 1016,
        //ERR_BinaryPointerOperationNotSupported = 1017,
        //ERR_AddingPointersWithNonPointerResultNotSupported = 1018,
        ERR_InstructionUnboxNotSupported = 1019,
        ERR_InstructionBoxNotSupported = 1020,
        ERR_InstructionNewobjWithManagedTypeNotSupported = 1021,
        ERR_AccessingManagedArrayNotSupported = 1022,
        ERR_InstructionLdtokenFieldNotSupported = 1023,
        ERR_InstructionLdtokenMethodNotSupported = 1024,
        ERR_InstructionLdtokenTypeNotSupported = 1025,
        ERR_InstructionLdtokenNotSupported = 1026,
        ERR_InstructionLdvirtftnNotSupported = 1027,
        ERR_InstructionNewarrNotSupported = 1028,
        ERR_InstructionRethrowNotSupported = 1029,
        ERR_InstructionCastclassNotSupported = 1030,
        //ERR_InstructionIsinstNotSupported = 1031,
        ERR_InstructionLdftnNotSupported = 1032,
        ERR_InstructionLdstrNotSupported = 1033,
        ERR_InstructionStsfldNotSupported = 1034,
        ERR_InstructionEndfilterNotSupported = 1035,
        ERR_InstructionEndfinallyNotSupported = 1036,
        ERR_InstructionLeaveNotSupported = 1037,
        ERR_InstructionNotSupported = 1038,
        ERR_LoadingFromStaticFieldNotSupported = 1039,
        ERR_LoadingFromNonReadonlyStaticFieldNotSupported = 1040,
        ERR_LoadingFromManagedStaticFieldNotSupported = 1041,
        ERR_LoadingFromManagedNonReadonlyStaticFieldNotSupported = 1042,
        ERR_InstructionStfldToManagedObjectNotSupported = 1043,
        ERR_InstructionLdlenNonConstantLengthNotSupported = 1044,
        ERR_StructWithAutoLayoutNotSupported = 1045,
        //ERR_StructWithPackNotSupported = 1046,
        ERR_StructWithGenericParametersAndExplicitLayoutNotSupported = 1047,
        ERR_StructSizeNotSupported = 1048,
        ERR_StructZeroSizeNotSupported = 1049,
        ERR_MarshalAsOnFieldNotSupported = 1050,
        ERR_TypeNotSupported = 1051,
        ERR_RequiredTypeModifierNotSupported = 1052,
        ERR_ErrorWhileProcessingVariable = 1053,

        // CecilExtensions
        ERR_UnableToResolveType = 1054,

        // ILFunctionReference
        ERR_UnableToResolveMethod = 1055,
        ERR_ConstructorNotSupported = 1056,
        ERR_FunctionPointerMethodMissingBurstCompileAttribute = 1057,
        ERR_FunctionPointerTypeMissingBurstCompileAttribute = 1058,
        ERR_FunctionPointerMethodAndTypeMissingBurstCompileAttribute = 1059,
        INF_FunctionPointerMethodAndTypeMissingMonoPInvokeCallbackAttribute = 10590,

        // ILVisitor
        // ERR_EntryPointFunctionCannotBeCalledInternally = 1060, // no longer used

        // ExternalFunctionParameterChecks
        ERR_MarshalAsOnParameterNotSupported = 1061,
        ERR_MarshalAsOnReturnTypeNotSupported = 1062,
        ERR_TypeNotBlittableForFunctionPointer = 1063,
        ERR_StructByValueNotSupported = 1064,
        ERR_StructsWithNonUnicodeCharsNotSupported = 1066,
        ERR_VectorsByValueNotSupported = 1067,

        // JitCompiler
        ERR_MissingExternBindings = 1068,

        // More ExternalFunctionParameterChecks
        ERR_MarshalAsNativeTypeOnReturnTypeNotSupported = 1069,

        // AssertProcessor
        ERR_AssertTypeNotSupported = 1071,

        // ReadOnlyProcessor
        ERR_StoringToReadOnlyFieldNotAllowed = 1072,
        ERR_StoringToFieldInReadOnlyParameterNotAllowed = 1073,
        ERR_StoringToReadOnlyParameterNotAllowed = 1074,

        // TypeManagerProcessor
        ERR_TypeManagerStaticFieldNotCompatible = 1075,
        ERR_UnableToFindTypeIndexForTypeManagerType = 1076,
        ERR_UnableToFindFieldForTypeManager = 1077,

        // Deprecated NoAliasAnalyzer
        // WRN_DisablingNoaliasUnsupportedLdobjImplicitNativeContainer = 1078,
        // WRN_DisablingNoaliasLoadingDirectlyFromFieldOfNativeArray = 1079,
        // WRN_DisablingNoaliasWritingDirectlyToFieldOfNativeArray = 1080,
        // WRN_DisablingNoaliasStoringImplicitNativeContainerToField = 1081,
        // WRN_DisablingNoaliasStoringImplicitNativeContainerToLocalVariable = 1082,
        // WRN_DisablingNoaliasStoringImplicitNativeContainerToPointer = 1083,
        // WRN_DisablingNoaliasCannotLoadNativeContainerAsBothArgumentAndField = 1084,
        // WRN_DisablingNoaliasSameArgumentPath = 1085,
        // WRN_DisablingNoaliasCannotPassMultipleNativeContainersConcurrently = 1086,
        // WRN_DisablingNoaliasUnsupportedNativeArrayUnsafeUtilityMethod = 1087,
        // WRN_DisablingNoaliasUnsupportedNativeArrayMethod = 1088,
        // WRN_DisablingNoaliasUnsupportedThisArgument = 1089,

        // StaticFieldAccessTransform
        ERR_CircularStaticConstructorUsage = 1090,
        ERR_ExternalInternalCallsInStaticConstructorsNotSupported = 1091,

        // AotCompiler
        ERR_PlatformNotSupported = 1092,
        ERR_InitModuleVerificationError = 1093,

        // NativeCompiler
        ERR_ModuleVerificationError = 1094,

        // TypeManagerProcessor
        ERR_UnableToFindTypeRequiredForTypeManager = 1095,

        // ILBuilder
        ERR_UnexpectedIntegerTypesForBinaryOperation = 1096,
        ERR_BinaryOperationNotSupported = 1097,
        ERR_CalliWithThisNotSupported = 1098,
        ERR_CalliNonCCallingConventionNotSupported = 1099,
        ERR_StringLiteralTooBig = 1100,
        ERR_LdftnNonCCallingConventionNotSupported = 1101,
        ERR_UnableToCallMethodOnInterfaceObject = 1102,

        // CheckIntrinsicUsageTransform
        ERR_UnsupportedCpuDependentBranch = 1199,
        ERR_InstructionTargetCpuFeatureNotAllowedInThisBlock = 1200,

        // AssumeRange
        ERR_AssumeRangeTypeMustBeInteger = 1201,
        ERR_AssumeRangeTypeMustBeSameSign = 1202,

        // LdfldaTransform
        ERR_UnsupportedSpillTransform = 1300,
        ERR_UnsupportedSpillTransformTooManyUsers = 1301,

        // Intrinsics
        ERR_MethodNotSupported = 1302,
        ERR_VectorsLoadFieldIsAddress = 1303,
        ERR_ConstantExpressionRequired = 1304,
        WRN_HWInstrinsicsWithFPDeterminism = 1305,
        ERR_UnsupportedHardwareIntrinsic = 1306,

        // UBAA
        ERR_PointerArgumentsUnexpectedAliasing = 1310,

        // Loop intrinsics
        ERR_LoopIntrinsicMustBeCalledInsideLoop = 1320,
        ERR_LoopUnexpectedAutoVectorization = 1321,
        WRN_LoopIntrinsicCalledButLoopOptimizedAway = 1322,

        // AssertProcessor
        ERR_AssertArgTypesDiffer = 1330,

        // StringUsageTransform
        ERR_StringInternalCompilerFixedStringTooManyUsers = 1340,
        ERR_StringInvalidFormatMissingClosingBrace = 1341,
        ERR_StringInvalidIntegerForArgumentIndex = 1342,
        ERR_StringInvalidFormatForArgument = 1343,
        ERR_StringArgumentIndexOutOfRange = 1344,
        ERR_StringInvalidArgumentType = 1345,
        ERR_DebugLogNotSupported = 1346,
        ERR_StringInvalidNonLiteralFormat = 1347,
        ERR_StringInvalidStringFormatMethod = 1348,
        ERR_StringInvalidArgument = 1349,

        ERR_StringArrayInvalidArrayCreation = 1350,
        ERR_StringArrayInvalidArraySize = 1351,
        ERR_StringArrayInvalidControlFlow = 1352,
        ERR_StringArrayInvalidArrayIndex = 1353,
        ERR_StringArrayInvalidArrayIndexOutOfRange = 1354,

        ERR_UnmanagedStringMethodMissing = 1355,
        ERR_UnmanagedStringMethodInvalid = 1356,

        // Static constructor
        ERR_ManagedStaticConstructor = 1360,
        ERR_StaticConstantArrayInStaticConstructor = 1361,

        // Safety check warning
        WRN_ExceptionThrownInNonSafetyCheckGuardedFunction = 1370,

        // Discarded method warning
        WRN_ACallToMethodHasBeenDiscarded = 1371,

        // Accessing a nested managed array is not supported
        ERR_AccessingNestedManagedArrayNotSupported = 1380,

        // Loading from a non-pointer / non-reference is not supported
        ERR_LdobjFromANonPointerNonReference = 1381,

        ERR_StringLiteralRequired = 1382,

        ERR_MultiDimensionalArrayUnsupported = 1383,

        ERR_NonBlittableAndNonManagedSequentialStructNotSupported = 1384,

        ERR_VarArgFunctionNotSupported = 1385,

        // StringInterpolatedTransform
        ERR_DefaultStringInterpolatedHandlerConstructorNotSupported = 1386,
        ERR_DefaultStringInterpolatedHandlerInvalidControlFlow = 1387,
    }
}
