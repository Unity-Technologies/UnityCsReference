// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;


namespace EmbeddedScriptedObjectsTests
{
    [AttributeUsage(AttributeTargets.Field)]
    internal sealed partial class SerializeReference : Attribute {}

    [AttributeUsage(AttributeTargets.Field)]
    public sealed partial class SerializeField : Attribute {}

    public sealed class ExtensionOfNativeClass : Attribute {}

    public sealed class HideInInspector : Attribute {}

    public sealed class FormerlySerializedAsAttribute : Attribute {}

    public interface ISerializationCallbackReceiver {}

    // Mimic exactly data layout of UnityEnging.Object!
    [StructLayout(LayoutKind.Sequential)]
    public class Object
    {
        public IntPtr m_CachedPtr;
        public int m_InstanceID;
    }

    [ExtensionOfNativeClass]
    public class DummyClass : Object
    {
        public int Attribute1 = 1;
        public int GetValue() { return Attribute1; }
        public void SetValue(int val) { Attribute1 = val; }
    }

    [Serializable]
    public class SubObject
    {
        [SerializeReference]
        public BaseType value1;

        [SerializeReference]
        public BaseType value2;

        [SerializeReference]
        public BaseType value3;

        [SerializeReference]
        public BaseType nullValue;
    }


    public interface IBaseType
    {
    }

    [Serializable]
    public class BaseType : IBaseType
    {
        public int intField;

        public BaseType()
        {// This is needed do to native test quirky mono state.
            intField = 0;
        }
    }

    [Serializable]
    public class ChildType1 : BaseType
    {
        public float floatField;

        public ChildType1()
        {// This is needed do to native test quicky mono state.
            floatField = 0;
        }
    }

    [Serializable]
    public class ChildType2 : BaseType
    {
        public string stringField;

        public ChildType2()
        {// This is needed do to native test quirky mono state.
            stringField = string.Empty;
        }
    }

    [Serializable]
    public struct ValueType
    {
        [SerializeField]
        public int x;
        [SerializeField]
        public int y;
    }

    [ExtensionOfNativeClass]
    public class ClassWithReferences : Object
    {
        [SerializeReference]
        public SubObject field;

        public ClassWithReferences()
        {// This is needed do to native test quirky mono state.
            field = null;
        }

        public void Init()
        {
            field = new SubObject
            {
                value1 = new ChildType1() { intField = 45, floatField = 23f },
                value2 = new ChildType2() { intField = 79, stringField = "hello world" }
            };
            field.value3 = field.value2;
        }
    }

    [Serializable]
    public class ClassWithAnArray
    {
        [SerializeField]
        public List<ValueType> aList;

        public ClassWithAnArray()
        {// This is needed do to native test quirky mono state.
            aList = null;
        }
    }

    [ExtensionOfNativeClass]
    public class ClassWithArrayOfRefs : Object
    {
        [SerializeReference]
        public BaseType[] arrayOfRefs;

        [SerializeReference]
        public List<BaseType> listOfRefs;


        public ClassWithArrayOfRefs()
        { // This is needed do to native test quirky mono state.
            arrayOfRefs = null;
            listOfRefs = null;
        }

        public void Init()
        {
            var value1 = new ChildType1() { intField = 45, floatField = 23f             };
            var value2 = new ChildType2() { intField = 79, stringField = "hello world"  };

            arrayOfRefs = new BaseType[]    { value1, value2, value2 };
            listOfRefs = new List<BaseType> { value1, value2, value2 };
        }
    }

    [Serializable]
    public abstract class AbstractClass
    {
        [SerializeField]
        public int fieldX;

        public abstract void Foo();
    }

    [Serializable]
    public class ConcreateClass : AbstractClass
    {
        [SerializeField]
        public int fieldY;

        public override void Foo() {}
    }


    [ExtensionOfNativeClass]
    public class ClassWithAbstractFields : Object
    {
        [SerializeReference]
        public IBaseType anInterface;

        [SerializeReference]
        public IBaseType[] arrayOInterfaces;

        [SerializeReference]
        public AbstractClass anAbstract;

        [SerializeReference]
        public AbstractClass[] anAbstractArray;

        public ClassWithAbstractFields()
        {// This is needed do to native test quirky mono state.
            anInterface = null;
            arrayOInterfaces = null;
            anAbstract = null;
            anAbstractArray = null;
        }

        public void Init()
        {
            anInterface = new BaseType() { intField = 1 };
            arrayOInterfaces = new IBaseType[] { new BaseType() { intField = 2 } };
            anAbstract = new ConcreateClass() { fieldY = 1, fieldX = 2 };
            anAbstractArray = new AbstractClass[] { new ConcreateClass() { fieldY = 2, fieldX = 4 } };
        }
    }

    [ExtensionOfNativeClass]
    public class ClassWithNestedClass : Object
    {
        [Serializable]
        public class NestedClass
        {
            [SerializeField] public int value;
        }

        [SerializeField] public NestedClass field;

        public ClassWithNestedClass()
        {
            field = null;
        }

        public void Init()
        {
            field = new NestedClass() { value = 1 };
        }
    }
}
