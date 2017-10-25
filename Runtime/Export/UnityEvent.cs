// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine.Serialization;
using UnityEngine.Scripting;
using UnityEngineInternal;

namespace UnityEngine.Events
{
    [Serializable]
    public enum PersistentListenerMode
    {
        EventDefined,
        Void,
        Object,
        Int,
        Float,
        String,
        Bool
    }

    [Serializable]
    class ArgumentCache : ISerializationCallbackReceiver
    {
        [FormerlySerializedAs("objectArgument")]
        [SerializeField] private Object m_ObjectArgument;
        [FormerlySerializedAs("objectArgumentAssemblyTypeName")]
        [SerializeField] private string m_ObjectArgumentAssemblyTypeName;
        [FormerlySerializedAs("intArgument")]
        [SerializeField] private int    m_IntArgument;
        [FormerlySerializedAs("floatArgument")]
        [SerializeField] private float  m_FloatArgument;
        [FormerlySerializedAs("stringArgument")]
        [SerializeField] private string m_StringArgument;
        [SerializeField] private bool m_BoolArgument;

        public Object unityObjectArgument
        {
            get { return m_ObjectArgument; }
            set
            {
                m_ObjectArgument = value;
                m_ObjectArgumentAssemblyTypeName = value != null ? value.GetType().AssemblyQualifiedName : string.Empty;
            }
        }

        public string unityObjectArgumentAssemblyTypeName
        {
            get { return m_ObjectArgumentAssemblyTypeName; }
        }

        public int    intArgument    { get { return m_IntArgument;    } set { m_IntArgument = value;    } }
        public float  floatArgument  { get { return m_FloatArgument;  } set { m_FloatArgument = value;  } }
        public string stringArgument { get { return m_StringArgument; } set { m_StringArgument = value; } }
        public bool   boolArgument   { get { return m_BoolArgument;   } set { m_BoolArgument = value; } }

        // Fix for assembly type name containing version / culture. We don't care about this for UI.
        // we need to fix this here, because there is old data in existing projects.
        // Typically, we're looking for .net Assembly Qualified Type Names and stripping everything after '<namespaces>.<typename>, <assemblyname>'
        // Example: System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089' -> 'System.String, mscorlib'
        private void TidyAssemblyTypeName()
        {
            if (string.IsNullOrEmpty(m_ObjectArgumentAssemblyTypeName))
                return;

            int min = Int32.MaxValue;
            int i = m_ObjectArgumentAssemblyTypeName.IndexOf(", Version=");
            if (i != -1)
                min = Math.Min(i, min);
            i = m_ObjectArgumentAssemblyTypeName.IndexOf(", Culture=");
            if (i != -1)
                min = Math.Min(i, min);
            i = m_ObjectArgumentAssemblyTypeName.IndexOf(", PublicKeyToken=");
            if (i != -1)
                min = Math.Min(i, min);

            if (min != Int32.MaxValue)
                m_ObjectArgumentAssemblyTypeName = m_ObjectArgumentAssemblyTypeName.Substring(0, min);

            // Strip module assembly name, as some platforms use modules, and some don't.
            // The non-modular version will always work, due to type forwarders.
            i = m_ObjectArgumentAssemblyTypeName.IndexOf(", UnityEngine.");
            if (i != -1 && m_ObjectArgumentAssemblyTypeName.EndsWith("Module"))
                m_ObjectArgumentAssemblyTypeName = m_ObjectArgumentAssemblyTypeName.Substring(0, i) + ", UnityEngine";
        }

        public void OnBeforeSerialize()
        {
            TidyAssemblyTypeName();
        }

        public void OnAfterDeserialize()
        {
            TidyAssemblyTypeName();
        }
    }

    internal abstract class BaseInvokableCall
    {
        protected BaseInvokableCall()
        {}

        protected BaseInvokableCall(object target, MethodInfo function)
        {
            if (target == null)
                throw new ArgumentNullException("target");
            if (function == null)
                throw new ArgumentNullException("function");
        }

        public abstract void Invoke(object[] args);

        protected static void ThrowOnInvalidArg<T>(object arg)
        {
            if (arg != null && !(arg is T))
                throw new ArgumentException(UnityString.Format("Passed argument 'args[0]' is of the wrong type. Type:{0} Expected:{1}", arg.GetType(), typeof(T)));
        }

        protected static bool AllowInvoke(Delegate @delegate)
        {
            var target = @delegate.Target;

            // static
            if (target == null)
                return true;

            // UnityEngine object
            var unityObj = target as Object;
            if (!ReferenceEquals(unityObj, null))
                return unityObj != null;

            // Normal object
            return true;
        }

        public abstract bool Find(object targetObj, MethodInfo method);
    }

    class InvokableCall : BaseInvokableCall
    {
        private event UnityAction Delegate;

        public InvokableCall(object target, MethodInfo theFunction)
            : base(target, theFunction)
        {
            Delegate += (UnityAction)theFunction.CreateDelegate(typeof(UnityAction), target);
        }

        public InvokableCall(UnityAction action)
        {
            Delegate += action;
        }

        public override void Invoke(object[] args)
        {
            if (AllowInvoke(Delegate))
                Delegate();
        }

        public void Invoke()
        {
            if (AllowInvoke(Delegate))
                Delegate();
        }

        public override bool Find(object targetObj, MethodInfo method)
        {
            // Case 827748: You can't compare Delegate.GetMethodInfo() == method, because sometimes it will not work, that's why we're using Equals instead, because it will compare that actual method inside.
            //              Comment from Microsoft:
            //              Desktop behavior regarding identity has never really been guaranteed. The desktop aggressively caches and reuses MethodInfo objects so identity checks often work by accident.
            //              .Net Native doesn’t guarantee identity and caches a lot less
            return Delegate.Target == targetObj && Delegate.GetMethodInfo().Equals(method);
        }
    }

    class InvokableCall<T1> : BaseInvokableCall
    {
        protected event UnityAction<T1> Delegate;

        public InvokableCall(object target, MethodInfo theFunction)
            : base(target, theFunction)
        {
            Delegate += (UnityAction<T1>)theFunction.CreateDelegate(typeof(UnityAction<T1>), target);
        }

        public InvokableCall(UnityAction<T1> action)
        {
            Delegate += action;
        }

        public override void Invoke(object[] args)
        {
            if (args.Length != 1)
                throw new ArgumentException("Passed argument 'args' is invalid size. Expected size is 1");
            ThrowOnInvalidArg<T1>(args[0]);

            if (AllowInvoke(Delegate))
                Delegate((T1)args[0]);
        }

        public virtual void Invoke(T1 args0)
        {
            if (AllowInvoke(Delegate))
                Delegate(args0);
        }

        public override bool Find(object targetObj, MethodInfo method)
        {
            return Delegate.Target == targetObj && Delegate.GetMethodInfo().Equals(method);
        }
    }

    class InvokableCall<T1, T2> : BaseInvokableCall
    {
        protected event UnityAction<T1, T2> Delegate;

        public InvokableCall(object target, MethodInfo theFunction)
            : base(target, theFunction)
        {
            Delegate = (UnityAction<T1, T2>)theFunction.CreateDelegate(typeof(UnityAction<T1, T2>), target);
        }

        public InvokableCall(UnityAction<T1, T2> action)
        {
            Delegate += action;
        }

        public override void Invoke(object[] args)
        {
            if (args.Length != 2)
                throw new ArgumentException("Passed argument 'args' is invalid size. Expected size is 1");
            ThrowOnInvalidArg<T1>(args[0]);
            ThrowOnInvalidArg<T2>(args[1]);

            if (AllowInvoke(Delegate))
                Delegate((T1)args[0], (T2)args[1]);
        }

        public void Invoke(T1 args0, T2 args1)
        {
            if (AllowInvoke(Delegate))
                Delegate(args0, args1);
        }

        public override bool Find(object targetObj, MethodInfo method)
        {
            return Delegate.Target == targetObj && Delegate.GetMethodInfo().Equals(method);
        }
    }

    class InvokableCall<T1, T2, T3> : BaseInvokableCall
    {
        protected event UnityAction<T1, T2, T3> Delegate;

        public InvokableCall(object target, MethodInfo theFunction)
            : base(target, theFunction)
        {
            Delegate = (UnityAction<T1, T2, T3>)theFunction.CreateDelegate(typeof(UnityAction<T1, T2, T3>), target);
        }

        public InvokableCall(UnityAction<T1, T2, T3> action)
        {
            Delegate += action;
        }

        public override void Invoke(object[] args)
        {
            if (args.Length != 3)
                throw new ArgumentException("Passed argument 'args' is invalid size. Expected size is 1");
            ThrowOnInvalidArg<T1>(args[0]);
            ThrowOnInvalidArg<T2>(args[1]);
            ThrowOnInvalidArg<T3>(args[2]);

            if (AllowInvoke(Delegate))
                Delegate((T1)args[0], (T2)args[1], (T3)args[2]);
        }

        public void Invoke(T1 args0, T2 args1, T3 args2)
        {
            if (AllowInvoke(Delegate))
                Delegate(args0, args1, args2);
        }

        public override bool Find(object targetObj, MethodInfo method)
        {
            return Delegate.Target == targetObj && Delegate.GetMethodInfo().Equals(method);
        }
    }

    class InvokableCall<T1, T2, T3, T4> : BaseInvokableCall
    {
        protected event UnityAction<T1, T2, T3, T4> Delegate;

        public InvokableCall(object target, MethodInfo theFunction)
            : base(target, theFunction)
        {
            Delegate = (UnityAction<T1, T2, T3, T4>)theFunction.CreateDelegate(typeof(UnityAction<T1, T2, T3, T4>), target);
        }

        public InvokableCall(UnityAction<T1, T2, T3, T4> action)
        {
            Delegate += action;
        }

        public override void Invoke(object[] args)
        {
            if (args.Length != 4)
                throw new ArgumentException("Passed argument 'args' is invalid size. Expected size is 1");
            ThrowOnInvalidArg<T1>(args[0]);
            ThrowOnInvalidArg<T2>(args[1]);
            ThrowOnInvalidArg<T3>(args[2]);
            ThrowOnInvalidArg<T4>(args[3]);

            if (AllowInvoke(Delegate))
                Delegate((T1)args[0], (T2)args[1], (T3)args[2], (T4)args[3]);
        }

        public void Invoke(T1 args0, T2 args1, T3 args2, T4 args3)
        {
            if (AllowInvoke(Delegate))
                Delegate(args0, args1, args2, args3);
        }

        public override bool Find(object targetObj, MethodInfo method)
        {
            return Delegate.Target == targetObj && Delegate.GetMethodInfo().Equals(method);
        }
    }

    class CachedInvokableCall<T> : InvokableCall<T>
    {
        private readonly T m_Arg1;

        public CachedInvokableCall(Object target, MethodInfo theFunction, T argument)
            : base(target, theFunction)
        {
            m_Arg1 = argument;
        }

        public override void Invoke(object[] args)
        {
            base.Invoke(m_Arg1);
        }

        public override void Invoke(T arg0)
        {
            base.Invoke(m_Arg1);
        }
    }

    public enum UnityEventCallState
    {
        Off = 0,
        EditorAndRuntime = 1,
        RuntimeOnly = 2,
    }

    [Serializable]
    class PersistentCall
    {
        //keep the layout of this class in sync with MonoPersistentCall in PersistentCallCollection.cpp
        [FormerlySerializedAs("instance")]
        [SerializeField]
        private Object m_Target;

        [FormerlySerializedAs("methodName")]
        [SerializeField]
        private string m_MethodName;

        [FormerlySerializedAs("mode")]
        [SerializeField]
        private PersistentListenerMode m_Mode = PersistentListenerMode.EventDefined;

        [FormerlySerializedAs("arguments")]
        [SerializeField]
        private ArgumentCache m_Arguments = new ArgumentCache();

        [FormerlySerializedAs("enabled")]
        [FormerlySerializedAs("m_Enabled")]
        [SerializeField]
        private UnityEventCallState m_CallState = UnityEventCallState.RuntimeOnly;

        public Object target
        {
            get { return m_Target; }
        }

        public string methodName
        {
            get { return m_MethodName; }
        }

        public PersistentListenerMode mode
        {
            get { return m_Mode; }
            set { m_Mode = value; }
        }

        public ArgumentCache arguments
        {
            get { return m_Arguments; }
        }

        public UnityEventCallState callState
        {
            get { return m_CallState; }
            set { m_CallState = value; }
        }

        public bool IsValid()
        {
            // We need to use the same logic found in PersistentCallCollection.cpp, IsPersistentCallValid
            return target != null && !String.IsNullOrEmpty(methodName);
        }

        public BaseInvokableCall GetRuntimeCall(UnityEventBase theEvent)
        {
            if (m_CallState == UnityEventCallState.RuntimeOnly && !Application.isPlaying)
                return null;
            if (m_CallState == UnityEventCallState.Off || theEvent == null)
                return null;

            var method = theEvent.FindMethod(this);
            if (method == null)
                return null;

            switch (m_Mode)
            {
                case PersistentListenerMode.EventDefined:
                    return theEvent.GetDelegate(target, method);
                case PersistentListenerMode.Object:
                    return GetObjectCall(target, method, m_Arguments);
                case PersistentListenerMode.Float:
                    return new CachedInvokableCall<float>(target, method, m_Arguments.floatArgument);
                case PersistentListenerMode.Int:
                    return new CachedInvokableCall<int>(target, method, m_Arguments.intArgument);
                case PersistentListenerMode.String:
                    return new CachedInvokableCall<string>(target, method, m_Arguments.stringArgument);
                case PersistentListenerMode.Bool:
                    return new CachedInvokableCall<bool>(target, method, m_Arguments.boolArgument);
                case PersistentListenerMode.Void:
                    return new InvokableCall(target, method);
            }
            return null;
        }

        // need to generate a generic typed version of the call here
        // this is due to the fact that we allow binding of 'any'
        // functions that extend object.
        private static BaseInvokableCall GetObjectCall(Object target, MethodInfo method, ArgumentCache arguments)
        {
            var type = typeof(Object);
            if (!string.IsNullOrEmpty(arguments.unityObjectArgumentAssemblyTypeName))
                type = Type.GetType(arguments.unityObjectArgumentAssemblyTypeName, false) ?? typeof(Object);

            var generic = typeof(CachedInvokableCall<>);
            var specific = generic.MakeGenericType(type);
            var ci = specific.GetConstructor(new[] { typeof(Object), typeof(MethodInfo), type});

            var castedObject = arguments.unityObjectArgument;
            if (castedObject != null && !type.IsAssignableFrom(castedObject.GetType()))
                castedObject = null;

            // need to pass explicit null here!
            return ci.Invoke(new object[] {target, method, castedObject}) as BaseInvokableCall;
        }

        public void RegisterPersistentListener(Object ttarget, string mmethodName)
        {
            m_Target = ttarget;
            m_MethodName = mmethodName;
        }

        public void UnregisterPersistentListener()
        {
            m_MethodName = string.Empty;
            m_Target = null;
        }
    }

    [Serializable]
    internal class PersistentCallGroup
    {
        [FormerlySerializedAs("m_Listeners")]
        [SerializeField] private List<PersistentCall> m_Calls;

        public PersistentCallGroup()
        {
            m_Calls = new List<PersistentCall>();
        }

        public int Count
        {
            get { return m_Calls.Count; }
        }

        public PersistentCall GetListener(int index)
        {
            return m_Calls[index];
        }

        public IEnumerable<PersistentCall> GetListeners()
        {
            return m_Calls;
        }

        public void AddListener()
        {
            m_Calls.Add(new PersistentCall());
        }

        public void AddListener(PersistentCall call)
        {
            m_Calls.Add(call);
        }

        public void RemoveListener(int index)
        {
            m_Calls.RemoveAt(index);
        }

        public void Clear()
        {
            m_Calls.Clear();
        }

        public void RegisterEventPersistentListener(int index, Object targetObj, string methodName)
        {
            var listener = GetListener(index);
            listener.RegisterPersistentListener(targetObj, methodName);
            listener.mode = PersistentListenerMode.EventDefined;
        }

        public void RegisterVoidPersistentListener(int index, Object targetObj, string methodName)
        {
            var listener = GetListener(index);
            listener.RegisterPersistentListener(targetObj, methodName);
            listener.mode = PersistentListenerMode.Void;
        }

        public void RegisterObjectPersistentListener(int index, Object targetObj, Object argument, string methodName)
        {
            var listener = GetListener(index);
            listener.RegisterPersistentListener(targetObj, methodName);
            listener.mode = PersistentListenerMode.Object;
            listener.arguments.unityObjectArgument = argument;
        }

        public void RegisterIntPersistentListener(int index, Object targetObj, int argument, string methodName)
        {
            var listener = GetListener(index);
            listener.RegisterPersistentListener(targetObj, methodName);
            listener.mode = PersistentListenerMode.Int;
            listener.arguments.intArgument = argument;
        }

        public void RegisterFloatPersistentListener(int index, Object targetObj, float argument, string methodName)
        {
            var listener = GetListener(index);
            listener.RegisterPersistentListener(targetObj, methodName);
            listener.mode = PersistentListenerMode.Float;
            listener.arguments.floatArgument = argument;
        }

        public void RegisterStringPersistentListener(int index, Object targetObj, string argument, string methodName)
        {
            var listener = GetListener(index);
            listener.RegisterPersistentListener(targetObj, methodName);
            listener.mode = PersistentListenerMode.String;
            listener.arguments.stringArgument = argument;
        }

        public void RegisterBoolPersistentListener(int index, Object targetObj, bool argument, string methodName)
        {
            var listener = GetListener(index);
            listener.RegisterPersistentListener(targetObj, methodName);
            listener.mode = PersistentListenerMode.Bool;
            listener.arguments.boolArgument = argument;
        }

        public void UnregisterPersistentListener(int index)
        {
            var evt = GetListener(index);
            evt.UnregisterPersistentListener();
        }

        public void RemoveListeners(Object target, string methodName)
        {
            var toRemove = new List<PersistentCall>();
            for (int index = 0; index < m_Calls.Count; index++)
            {
                if (m_Calls[index].target == target && m_Calls[index].methodName == methodName)
                    toRemove.Add(m_Calls[index]);
            }
            m_Calls.RemoveAll(toRemove.Contains);
        }

        public void Initialize(InvokableCallList invokableList, UnityEventBase unityEventBase)
        {
            foreach (var persistentCall in m_Calls)
            {
                if (!persistentCall.IsValid())
                    continue;

                var call = persistentCall.GetRuntimeCall(unityEventBase);
                if (call != null)
                    invokableList.AddPersistentInvokableCall(call);
            }
        }
    }

    class InvokableCallList
    {
        private readonly List<BaseInvokableCall> m_PersistentCalls = new List<BaseInvokableCall>();
        private readonly List<BaseInvokableCall> m_RuntimeCalls = new List<BaseInvokableCall>();

        private readonly List<BaseInvokableCall> m_ExecutingCalls = new List<BaseInvokableCall>();

        private bool m_NeedsUpdate = true;

        public int Count
        {
            get { return m_PersistentCalls.Count + m_RuntimeCalls.Count; }
        }

        public void AddPersistentInvokableCall(BaseInvokableCall call)
        {
            m_PersistentCalls.Add(call);
            m_NeedsUpdate = true;
        }

        public void AddListener(BaseInvokableCall call)
        {
            m_RuntimeCalls.Add(call);
            m_NeedsUpdate = true;
        }

        public void RemoveListener(object targetObj, MethodInfo method)
        {
            var toRemove = new List<BaseInvokableCall>();
            for (int index = 0; index < m_RuntimeCalls.Count; index++)
            {
                if (m_RuntimeCalls[index].Find(targetObj, method))
                    toRemove.Add(m_RuntimeCalls[index]);
            }
            m_RuntimeCalls.RemoveAll(toRemove.Contains);
            m_NeedsUpdate = true;
        }

        public void Clear()
        {
            m_RuntimeCalls.Clear();
            m_NeedsUpdate = true;
        }

        public void ClearPersistent()
        {
            m_PersistentCalls.Clear();
            m_NeedsUpdate = true;
        }

        public List<BaseInvokableCall> PrepareInvoke()
        {
            if (m_NeedsUpdate)
            {
                m_ExecutingCalls.Clear();
                m_ExecutingCalls.AddRange(m_PersistentCalls);
                m_ExecutingCalls.AddRange(m_RuntimeCalls);
                m_NeedsUpdate = false;
            }

            return m_ExecutingCalls;
        }
    }

    [Serializable]
    [UsedByNativeCode]
    public abstract class UnityEventBase : ISerializationCallbackReceiver
    {
        private InvokableCallList m_Calls;

        [FormerlySerializedAs("m_PersistentListeners")]
        [SerializeField]
        private PersistentCallGroup m_PersistentCalls;

#pragma warning disable 414 //used by serialized properties
        [SerializeField] private string m_TypeName;

        // Dirtying can happen outside of MainThread, but we need to rebuild on the MainThread.
        private bool m_CallsDirty = true;

        protected UnityEventBase()
        {
            m_Calls = new InvokableCallList();
            m_PersistentCalls = new PersistentCallGroup();
            m_TypeName = GetType().AssemblyQualifiedName;
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {}

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            DirtyPersistentCalls();
            m_TypeName = GetType().AssemblyQualifiedName;
        }

        protected abstract MethodInfo FindMethod_Impl(string name, object targetObj);
        internal abstract BaseInvokableCall GetDelegate(object target, MethodInfo theFunction);

        internal MethodInfo FindMethod(PersistentCall call)
        {
            var type = typeof(Object);
            if (!string.IsNullOrEmpty(call.arguments.unityObjectArgumentAssemblyTypeName))
                type = Type.GetType(call.arguments.unityObjectArgumentAssemblyTypeName, false) ?? typeof(Object);

            return FindMethod(call.methodName, call.target, call.mode, type);
        }

        internal MethodInfo FindMethod(string name, object listener, PersistentListenerMode mode, Type argumentType)
        {
            switch (mode)
            {
                case PersistentListenerMode.EventDefined:
                    return FindMethod_Impl(name, listener);
                case PersistentListenerMode.Void:
                    return GetValidMethodInfo(listener, name, new Type[0]);
                case PersistentListenerMode.Float:
                    return GetValidMethodInfo(listener, name, new[] { typeof(float) });
                case PersistentListenerMode.Int:
                    return GetValidMethodInfo(listener, name, new[] { typeof(int) });
                case PersistentListenerMode.Bool:
                    return GetValidMethodInfo(listener, name, new[] { typeof(bool) });
                case PersistentListenerMode.String:
                    return GetValidMethodInfo(listener, name, new[] { typeof(string) });
                case PersistentListenerMode.Object:
                    return GetValidMethodInfo(listener, name, new[] { argumentType ?? typeof(Object) });
                default:
                    return null;
            }
        }

        public int GetPersistentEventCount()
        {
            return m_PersistentCalls.Count;
        }

        public Object GetPersistentTarget(int index)
        {
            var listener = m_PersistentCalls.GetListener(index);
            return listener != null ? listener.target : null;
        }

        public string GetPersistentMethodName(int index)
        {
            var listener = m_PersistentCalls.GetListener(index);
            return listener != null ? listener.methodName : string.Empty;
        }

        private void DirtyPersistentCalls()
        {
            m_Calls.ClearPersistent();
            m_CallsDirty = true;
        }

        // Can only run on MainThread
        private void RebuildPersistentCallsIfNeeded()
        {
            if (m_CallsDirty)
            {
                m_PersistentCalls.Initialize(m_Calls, this);
                m_CallsDirty = false;
            }
        }

        public void SetPersistentListenerState(int index, UnityEventCallState state)
        {
            var listener = m_PersistentCalls.GetListener(index);
            if (listener != null)
                listener.callState = state;

            DirtyPersistentCalls();
        }

        protected void AddListener(object targetObj, MethodInfo method)
        {
            m_Calls.AddListener(GetDelegate(targetObj, method));
        }

        internal void AddCall(BaseInvokableCall call)
        {
            m_Calls.AddListener(call);
        }

        protected void RemoveListener(object targetObj, MethodInfo method)
        {
            m_Calls.RemoveListener(targetObj, method);
        }

        public void RemoveAllListeners()
        {
            m_Calls.Clear();
        }

        internal List<BaseInvokableCall> PrepareInvoke()
        {
            RebuildPersistentCallsIfNeeded();
            return m_Calls.PrepareInvoke();
        }

        protected void Invoke(object[] parameters)
        {
            List<BaseInvokableCall> calls = PrepareInvoke();

            for (var i = 0; i < calls.Count; i++)
                calls[i].Invoke(parameters);
        }

        public override string ToString()
        {
            return base.ToString() + " " + GetType().FullName;
        }

        // Find a valid method that can be bound to an event with a given name
        public static MethodInfo GetValidMethodInfo(object obj, string functionName, Type[] argumentTypes)
        {
            var type = obj.GetType();
            while (type != typeof(object) && type != null)
            {
                var method = type.GetMethod(functionName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, argumentTypes, null);
                if (method != null)
                {
                    // We need to make sure the Arguments are sane. When using the Type.DefaultBinder like we are above,
                    // it is possible to receive a method that takes a System.Object enve though we requested a float, int or bool.
                    // This can be an issue when the user changes the signature of a function that he had already set up via inspector.
                    // When changing a float parameter to a System.Object the getMethod would still bind to the cahnged version, but
                    // the PersistentListenerMode would still be kept as Float.
                    // TODO: Should we allow anything else besides Primitive types and types derived from UnityEngine.Object?
                    var parameterInfos = method.GetParameters();
                    var methodValid = true;
                    var i = 0;
                    foreach (ParameterInfo pi in parameterInfos)
                    {
                        var requestedType = argumentTypes[i];
                        var receivedType = pi.ParameterType;
                        methodValid = requestedType.IsPrimitive == receivedType.IsPrimitive;

                        if (!methodValid)
                            break;
                        i++;
                    }
                    if (methodValid)
                        return method;
                }
                type = type.BaseType;
            }
            return null;
        }


        protected bool ValidateRegistration(MethodInfo method, object targetObj, PersistentListenerMode mode)
        {
            return ValidateRegistration(method, targetObj, mode, typeof(Object));
        }

        protected bool ValidateRegistration(MethodInfo method, object targetObj, PersistentListenerMode mode, Type argumentType)
        {
            if (method == null)
                throw new ArgumentNullException("method", UnityString.Format("Can not register null method on {0} for callback!", targetObj));

            var obj = targetObj as Object;
            if (obj == null || obj.GetInstanceID() == 0)
            {
                throw new ArgumentException(
                    UnityString.Format("Could not register callback {0} on {1}. The class {2} does not derive from UnityEngine.Object",
                        method.Name,
                        targetObj,
                        targetObj == null ? "null" : targetObj.GetType().ToString()));
            }

            if (method.IsStatic)
                throw new ArgumentException(UnityString.Format("Could not register listener {0} on {1} static functions are not supported.", method, GetType()));

            if (FindMethod(method.Name, targetObj, mode, argumentType) == null)
            {
                Debug.LogWarning(UnityString.Format("Could not register listener {0}.{1} on {2} the method could not be found.", targetObj, method, GetType()));
                return false;
            }
            return true;
        }

        internal void AddPersistentListener()
        {
            m_PersistentCalls.AddListener();
        }

        protected void RegisterPersistentListener(int index, object targetObj, MethodInfo method)
        {
            if (!ValidateRegistration(method, targetObj, PersistentListenerMode.EventDefined))
                return;

            m_PersistentCalls.RegisterEventPersistentListener(index, targetObj as Object, method.Name);
            DirtyPersistentCalls();
        }

        internal void RemovePersistentListener(Object target, MethodInfo method)
        {
            if (method == null || method.IsStatic || target == null || target.GetInstanceID() == 0)
                return;
            m_PersistentCalls.RemoveListeners(target, method.Name);
            DirtyPersistentCalls();
        }

        internal void RemovePersistentListener(int index)
        {
            m_PersistentCalls.RemoveListener(index);
            DirtyPersistentCalls();
        }

        internal void UnregisterPersistentListener(int index)
        {
            m_PersistentCalls.UnregisterPersistentListener(index);
            DirtyPersistentCalls();
        }

        internal void AddVoidPersistentListener(UnityAction call)
        {
            var count = GetPersistentEventCount();
            AddPersistentListener();
            RegisterVoidPersistentListener(count, call);
        }

        internal void RegisterVoidPersistentListener(int index, UnityAction call)
        {
            if (call == null)
            {
                Debug.LogWarning("Registering a Listener requires an action");
                return;
            }
            if (!ValidateRegistration(call.Method, call.Target, PersistentListenerMode.Void))
                return;

            m_PersistentCalls.RegisterVoidPersistentListener(index, call.Target as Object, call.Method.Name);
            DirtyPersistentCalls();
        }

        internal void AddIntPersistentListener(UnityAction<int> call, int argument)
        {
            var count = GetPersistentEventCount();
            AddPersistentListener();
            RegisterIntPersistentListener(count, call, argument);
        }

        internal void RegisterIntPersistentListener(int index, UnityAction<int> call, int argument)
        {
            if (call == null)
            {
                Debug.LogWarning("Registering a Listener requires an action");
                return;
            }
            if (!ValidateRegistration(call.Method, call.Target, PersistentListenerMode.Int))
                return;

            m_PersistentCalls.RegisterIntPersistentListener(index, call.Target as Object, argument, call.Method.Name);
            DirtyPersistentCalls();
        }

        internal void AddFloatPersistentListener(UnityAction<float> call, float argument)
        {
            var count = GetPersistentEventCount();
            AddPersistentListener();
            RegisterFloatPersistentListener(count, call, argument);
        }

        internal void RegisterFloatPersistentListener(int index, UnityAction<float> call, float argument)
        {
            if (call == null)
            {
                Debug.LogWarning("Registering a Listener requires an action");
                return;
            }
            if (!ValidateRegistration(call.Method, call.Target, PersistentListenerMode.Float))
                return;

            m_PersistentCalls.RegisterFloatPersistentListener(index, call.Target as Object, argument, call.Method.Name);
            DirtyPersistentCalls();
        }

        internal void AddBoolPersistentListener(UnityAction<bool> call, bool argument)
        {
            var count = GetPersistentEventCount();
            AddPersistentListener();
            RegisterBoolPersistentListener(count, call, argument);
        }

        internal void RegisterBoolPersistentListener(int index, UnityAction<bool> call, bool argument)
        {
            if (call == null)
            {
                Debug.LogWarning("Registering a Listener requires an action");
                return;
            }
            if (!ValidateRegistration(call.Method, call.Target, PersistentListenerMode.Bool))
                return;

            m_PersistentCalls.RegisterBoolPersistentListener(index, call.Target as Object, argument, call.Method.Name);
            DirtyPersistentCalls();
        }

        internal void AddStringPersistentListener(UnityAction<string> call, string argument)
        {
            var count = GetPersistentEventCount();
            AddPersistentListener();
            RegisterStringPersistentListener(count, call, argument);
        }

        internal void RegisterStringPersistentListener(int index, UnityAction<string> call, string argument)
        {
            if (call == null)
            {
                Debug.LogWarning("Registering a Listener requires an action");
                return;
            }
            if (!ValidateRegistration(call.Method, call.Target, PersistentListenerMode.String))
                return;

            m_PersistentCalls.RegisterStringPersistentListener(index, call.Target as Object, argument, call.Method.Name);
            DirtyPersistentCalls();
        }

        internal void AddObjectPersistentListener<T>(UnityAction<T> call, T argument) where T : Object
        {
            var count = GetPersistentEventCount();
            AddPersistentListener();
            RegisterObjectPersistentListener(count, call, argument);
        }

        internal void RegisterObjectPersistentListener<T>(int index, UnityAction<T> call, T argument) where T : Object
        {
            if (call == null)
                throw new ArgumentNullException("call", "Registering a Listener requires a non null call");

            if (!ValidateRegistration(call.Method, call.Target, PersistentListenerMode.Object, argument == null ? typeof(Object) : argument.GetType()))
                return;

            m_PersistentCalls.RegisterObjectPersistentListener(index, call.Target as Object, argument, call.Method.Name);
            DirtyPersistentCalls();
        }

    }
}
