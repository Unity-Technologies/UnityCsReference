// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine.UIElements;
using UnityEngine.UIElements.StyleSheets;

namespace Unity.UI.Builder
{
    internal static class BuilderNameUtilities
    {
        public static string ConvertDashToCamel(string dash)
        {
            return StyleSheetUtility.ConvertDashToUpperNoSpace(dash, false, false);
        }

        public static string ConvertDashToHuman(string dash)
        {
            return StyleSheetUtility.ConvertDashToUpperNoSpace(dash, true, true);
        }

        public static string ConvertCamelToHuman(string camel)
        {
            return Regex.Replace(camel, "(\\B[A-Z])", " $1");
        }

        public static string ConvertStyleCSharpNameToUssName(string csharpName)
        {
            if (StylePropertyUtil.cSharpNameToUssName.TryGetValue(csharpName, out var ussName))
                return ussName;

            var dashCasedName = StyleSheetUtility.ConvertCamelToDash(csharpName);
            if (dashCasedName.StartsWith("unity-"))
                dashCasedName = "-" + dashCasedName;

            return dashCasedName;
        }

        public static string ConvertStyleUssNameToCSharpName(string ussName)
        {
            if (StylePropertyUtil.ussNameToCSharpName.TryGetValue(ussName, out var cSharpStyleName))
                return cSharpStyleName;

            if (ussName.StartsWith("-unity"))
                ussName = ussName.Substring(1);
            return ConvertDashToCamel(ussName);
        }

        public static string ConvertUssNameToStyleName(string ussName)
        {
            if (ussName == "-unity-font-style")
                return "-unity-font-style-and-weight";

            return ussName;
        }

        public static string ConvertUssNameToStyleCSharpName(string ussName)
        {
            ussName = ConvertUssNameToStyleName(ussName);

            if (ussName.StartsWith("-unity"))
                ussName = ussName.Substring(1);

            return ConvertDashToCamel(ussName);
        }

        public static string GetNameByReflection(object obj)
        {
            if (obj == null)
                return null;

            var type = obj.GetType();

            // Look for a name property
            var nameProperty = type.GetProperty("name");
            var objName = BuilderConstants.UnnamedValue;
            object nameValue = null;

            if (nameProperty != null)
            {
                nameValue = nameProperty.GetValue(obj);
            }
            else
            {
                var nameField = type.GetField(("name"));

                if (nameField != null)
                    nameValue = nameField.GetValue(obj);
            }

            if (nameValue != null)
                objName = nameValue.ToString();

            if (string.IsNullOrEmpty(objName))
                objName = BuilderConstants.UnnamedValue;
            return objName;
        }

        public static Regex attributeRegex { get; } = new Regex(@"^[a-zA-Z0-9\-_]+$");
        public static Regex styleSelectorRegex { get; } = new Regex(@"^[a-zA-Z0-9\-_:#\*>. ]+$");
        public static Regex bindingPathAttributeRegex { get; } = new Regex(@"^[a-zA-Z0-9\-_.\[\]]+$");
    }
}
