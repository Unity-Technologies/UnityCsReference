using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.Pool;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    class BuilderAttributeTypeName : VisualElement
    {
        static readonly UnityEngine.Pool.ObjectPool<Label> s_LabelPool = new UnityEngine.Pool.ObjectPool<Label>(
            () => new Label(),
            null,
            l =>
            {
                l.RemoveFromClassList(s_NamespaceClass);
                l.RemoveFromClassList(s_TypeNameClass);
                l.RemoveFromClassList(s_MatchedTokenClass);
            });

        const string s_UssPathNoExt = BuilderConstants.UtilitiesPath + "/TypeField/BuilderAttributeTypeName";
        const string s_UssPath = s_UssPathNoExt + ".uss";
        const string s_UssDarkPath = s_UssPathNoExt + "Dark.uss";
        const string s_UssLightPath = s_UssPathNoExt + "Light.uss";
        const string s_BaseClass = "unity-attribute-type-name";
        const string s_NamespaceClass = s_BaseClass + "__namespace";
        const string s_NamespaceContainerClass = s_NamespaceClass + "-container";
        const string s_NamespacePrefixClass = s_NamespaceClass + "-prefix";
        const string s_TypeNameClass = s_BaseClass + "__type-name";
        const string s_TypeNameContainerClass = s_TypeNameClass + "-container";
        const string s_MatchedTokenClass = s_BaseClass + "__matched-token";

        readonly VisualElement m_Namespace;
        readonly VisualElement m_TypeName;
        readonly Label m_InNamespace;

        public BuilderAttributeTypeName()
        {
            AddToClassList(s_BaseClass);

            styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(s_UssPath));
            if (EditorGUIUtility.isProSkin)
                styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(s_UssDarkPath));
            else
                styleSheets.Add(BuilderPackageUtilities.LoadAssetAtPath<StyleSheet>(s_UssLightPath));

            m_Namespace = new VisualElement();
            m_Namespace.AddToClassList(s_NamespaceContainerClass);
            m_TypeName = new VisualElement();
            m_TypeName.AddToClassList(s_TypeNameContainerClass);

            Add(m_TypeName);
            Add(m_Namespace);

            // Trailing spaces are trimmed when measuring the text, so we'll add a class on this particular Label to
            // force some padding on the right.
            m_InNamespace = new Label("in");
            m_InNamespace.AddToClassList(s_NamespacePrefixClass);
        }

        public static IEnumerable<int> AllIndexesOf(string str, string searchString)
        {
            var minIndex = str.IndexOf(searchString, StringComparison.InvariantCultureIgnoreCase);
            while (minIndex != -1)
            {
                yield return minIndex;
                minIndex = str.IndexOf(searchString, minIndex + searchString.Length, StringComparison.InvariantCultureIgnoreCase);
            }
        }

        readonly struct AutoCompleteToken
        {
            public AutoCompleteToken(string src, int start, int length, bool matchedToken)
            {
                this.source = src;
                this.startIndex = start;
                this.length = length;
                this.isMatchedToken = matchedToken;
            }

            public AutoCompleteToken(string src, int start, bool matchedToken)
                : this(src, start, src.Length - start, matchedToken)
            {
            }

            public readonly string source;
            public readonly int startIndex;
            public readonly int length;
            public readonly bool isMatchedToken;

            public override string ToString()
            {
                return source.Substring(startIndex, length);
            }
        }

        public void ClearType()
        {
            this.Query<Label>().ForEach(l =>
            {
                if (l != m_InNamespace)
                    s_LabelPool.Release(l)
                    ;
            });
            m_TypeName.Clear();
            m_Namespace.Clear();
        }

        public void SetType(Type type, string searchString = null)
        {
            ClearType();
            if (null == type)
                return;

            tooltip = type.AssemblyQualifiedName;

            var fullName = type.FullName;
            var nsLength = string.IsNullOrEmpty(type.Namespace)
                ? 0
                : type.Namespace.Length + 1;

            m_Namespace.Add(m_InNamespace);

            if (nsLength == 0)
            {
                var namespaceLabel = s_LabelPool.Get();
                namespaceLabel.AddToClassList(s_NamespaceClass);
                m_Namespace.Add(namespaceLabel);
                namespaceLabel.text = "global namespace";
            }

            if (string.IsNullOrEmpty(searchString))
            {
                var namespaceLabel = s_LabelPool.Get();
                namespaceLabel.AddToClassList(s_NamespaceClass);
                m_Namespace.Add(namespaceLabel);
                // Using the source namespace directly here, since we don't want to include the `.`
                namespaceLabel.text = !string.IsNullOrEmpty(type.Namespace)
                    ? type.Namespace
                    : "global namespace";

                var typeNameLabel = s_LabelPool.Get();
                m_TypeName.Add(typeNameLabel);
                typeNameLabel.text = fullName.Substring(nsLength);
                typeNameLabel.AddToClassList(s_TypeNameClass);
                return;
            }

            var list = ListPool<AutoCompleteToken>.Get();
            try
            {
                var current = 0;
                foreach (var index in AllIndexesOf(fullName, searchString))
                {
                    if (current != index)
                        list.Add(new AutoCompleteToken(fullName, current, index - current, false));
                    list.Add(new AutoCompleteToken(fullName, index, searchString.Length, true));
                    current = index + searchString.Length;
                }

                if (current < fullName.Length)
                    list.Add(new AutoCompleteToken(fullName, current, false));

                foreach (var part in list)
                {
                    if (part.startIndex < nsLength)
                    {
                        if (part.startIndex + part.length <= nsLength)
                        {
                            var namespaceLabel = s_LabelPool.Get();
                            m_Namespace.Add(namespaceLabel);
                            var length = part.length;
                            if (part.startIndex + part.length == nsLength)
                                length -= 1;
                            namespaceLabel.text = part.source.Substring(part.startIndex, length);
                            namespaceLabel.AddToClassList(s_NamespaceClass);
                            namespaceLabel.EnableInClassList(s_MatchedTokenClass, part.isMatchedToken);
                        }
                        else
                        {
                            var namespaceLabel = s_LabelPool.Get();
                            namespaceLabel.AddToClassList(s_NamespaceClass);
                            m_Namespace.Add(namespaceLabel);
                            namespaceLabel.text = part.source.Substring(part.startIndex, nsLength - part.startIndex - 1);
                            namespaceLabel.EnableInClassList(s_MatchedTokenClass, part.isMatchedToken);

                            var typeNameLabel = s_LabelPool.Get();
                            m_TypeName.Add(typeNameLabel);
                            typeNameLabel.text =
                                part.source.Substring(nsLength, part.startIndex + part.length - nsLength);
                            typeNameLabel.AddToClassList(s_TypeNameClass);
                            typeNameLabel.EnableInClassList(s_MatchedTokenClass, part.isMatchedToken);
                        }
                    }
                    else
                    {
                        var typeNameLabel = s_LabelPool.Get();
                        m_TypeName.Add(typeNameLabel);
                        typeNameLabel.text = part.ToString();
                        typeNameLabel.AddToClassList(s_TypeNameClass);
                        typeNameLabel.EnableInClassList(s_MatchedTokenClass, part.isMatchedToken);
                    }
                }
            }
            finally
            {
                ListPool<AutoCompleteToken>.Release(list);
            }
        }
    }
}
