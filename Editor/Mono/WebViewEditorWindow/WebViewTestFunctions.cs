// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using Debug = UnityEngine.Debug;

namespace UnityEditor.Web
{
    internal class WebViewTestFunctions
    {
        public int ReturnInt()
        {
            return 5;
        }

        public string ReturnString()
        {
            return "Five";
        }

        public bool ReturnBool()
        {
            return true;
        }

        public int[] ReturnNumberArray()
        {
            return new[] {1, 2, 3};
        }

        public string[] ReturnStringArray()
        {
            return new[] {"One", "Two", "Three"};
        }

        public bool[] ReturnBoolArray()
        {
            return new[] {true, false, true};
        }

        public TestObject ReturnObject()
        {
            TestObject testObject = new TestObject {NumberProperty = 5, StringProperty = "Five", BoolProperty = true};
            return testObject;
        }

        public void AcceptInt(int passedInt)
        {
            Debug.Log("A value was passed from JS: " + passedInt);
        }

        public void AcceptString(string passedString)
        {
            Debug.Log("A value was passed from JS: " + passedString);
        }

        public void AcceptBool(bool passedBool)
        {
            Debug.Log("A value was passed from JS: " + passedBool);
        }

        public void AcceptIntArray(int[] passedArray)
        {
            Debug.Log("An array was passed from the JS. Array elements were:");
            for (int i = 0; i <= passedArray.Length; i++)
            {
                Debug.Log("Element at index " + i + ": " + passedArray[i]);
            }
        }

        public void AcceptStringArray(string[] passedArray)
        {
            Debug.Log("An array was passed from the JS. Array elements were:");
            for (int i = 0; i <= passedArray.Length; i++)
            {
                Debug.Log("Element at index " + i + ": " + passedArray[i]);
            }
        }

        public void AcceptBoolArray(bool[] passedArray)
        {
            Debug.Log("An array was passed from the JS. Array elements were:");
            for (int i = 1; i <= passedArray.Length; i++)
            {
                Debug.Log("Element at index " + i + ": " + passedArray[i]);
            }
        }

        public void AcceptTestObject(TestObject passedObject)
        {
            Debug.Log("An object was passed from the JS. Properties were:");
            Debug.Log("StringProperty: " + passedObject.StringProperty);
            Debug.Log("NumberProperty: " + passedObject.NumberProperty);
            Debug.Log("BoolProperty: " + passedObject.BoolProperty);
        }

        //For testing function calls with no parameters or return value
        public void VoidMethod(string logMessage)
        {
            Debug.Log("A method was called from the CEF: " + logMessage);
        }

        //For testing access control on CEF function calls
        private string APrivateMethod(string input)
        {
            return "This method is private and not for CEF";
        }

        //For testing function calls that supply and expect an array of strings
        public string[] ArrayReverse(string[] input)
        {
            var outputStrings = (string[])input.Reverse();
            return outputStrings;
        }

        public void LogMessage(string message)
        {
            Debug.Log(message);
        }

        public static void RunTestScript(string path)
        {
            var url = "file:///" + path;

            JSProxyMgr.GetInstance().AddGlobalObject("WebViewTestFunctions", new WebViewTestFunctions());
            var window = WebViewEditorWindowTabs.Create<WebViewEditorWindowTabs>("Test Window", url, 0, 0, 0, 0);
            window.OnBatchMode();
        }
    }

    internal class TestObject
    {
        public string StringProperty { get; set; }
        public int NumberProperty { get; set; }
        public bool BoolProperty { get; set; }
    }
}
