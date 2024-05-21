// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

//#define DEBUG_INDEXING

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Profiling;
using UnityEditor;
using UnityEditor.Search.Providers;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.Search
{
    internal enum FilePattern
    {
        Extension,
        Folder,
        File
    }

    [Flags]
    enum PropertyWithPrefixIndexing
    {
        None = 0,
        NoPrefix = 1,
        WithPrefix = 1 << 1,

        All = NoPrefix | WithPrefix
    }

    static class PropertyWithPrefixIndexingExtensions
    {
        public static bool HasAny(this PropertyWithPrefixIndexing options, PropertyWithPrefixIndexing value)
        {
            return (options & value) != 0;
        }
    }

    /// <summary>
    /// Specialized <see cref="SearchIndexer"/> used to index Unity Assets. See <see cref="AssetIndexer"/> for a specialized SearchIndexer used to index simple assets and
    /// see <see cref="SceneIndexer"/> for an indexer used to index scene and prefabs.
    /// </summary>
    public abstract class ObjectIndexer : SearchIndexer
    {
        static readonly ProfilerMarker k_IndexWordMarker = new($"{nameof(ObjectIndexer)}.{nameof(IndexWord)}");
        static readonly ProfilerMarker k_IndexObjectMarker = new($"{nameof(ObjectIndexer)}.{nameof(IndexObject)}");
        static readonly ProfilerMarker k_IndexPropertiesMarker = new($"{nameof(ObjectIndexer)}.{nameof(IndexVisibleProperties)}");
        static readonly ProfilerMarker k_IndexPropertyMarker = new($"{nameof(ObjectIndexer)}.{nameof(IndexProperty)}");
        static readonly ProfilerMarker k_IndexSerializedPropertyMarker = new($"{nameof(ObjectIndexer)}.IndexSerializedProperty");
        static readonly ProfilerMarker k_IndexPropertyComponentsMarker = new($"{nameof(ObjectIndexer)}.IndexPropertyComponents");
        static readonly ProfilerMarker k_IndexPropertyStringComponentsMarker = new($"{nameof(ObjectIndexer)}.{nameof(IndexPropertyStringComponents)}");
        static readonly ProfilerMarker k_LogPropertyMarker = new($"{nameof(ObjectIndexer)}.{nameof(LogProperty)}");
        static readonly ProfilerMarker k_IndexCustomPropertiesMarker = new($"{nameof(ObjectIndexer)}.{nameof(IndexCustomProperties)}");
        static readonly ProfilerMarker k_AddReferenceMarker = new($"{nameof(ObjectIndexer)}.{nameof(AddReference)}");
        static readonly ProfilerMarker k_AddObjectReferenceMarker = new($"{nameof(ObjectIndexer)}.AddObjectReference");

        // Define a list of patterns that will automatically skip path entries if they start with it.
        readonly static string[] BuiltInTransientFilePatterns = new[]
        {
            "~",
            "##ignore",
            "InitTestScene"
        };

        internal SearchDatabase.Settings settings { get; private set; }

        private HashSet<string> m_IgnoredProperties;

        internal HashSet<string> ignoredProperties
        {
            get
            {
                if (m_IgnoredProperties == null)
                {
                    m_IgnoredProperties = new HashSet<string>(SearchSettings.ignoredProperties.Split(new char[] { ';', '\n' },
                                            StringSplitOptions.RemoveEmptyEntries).Select(t => t.ToLowerInvariant()));
                }
                return m_IgnoredProperties;
            }
        }

        internal ObjectIndexer(string name, SearchDatabase.Settings settings)
            : base(name)
        {
            this.settings = settings;
        }

        /// <summary>
        /// Run a search query in the index.
        /// </summary>
        /// <param name="searchQuery">Search query to look out for. If if matches any of the indexed variations a result will be returned.</param>
        /// <param name="context">The search context on which the query is applied.</param>
        /// <param name="provider">The provider that initiated the search.</param>
        /// <param name="maxScore">Maximum score of any matched Search Result. See <see cref="SearchResult.score"/>.</param>
        /// <param name="patternMatchLimit">Maximum number of matched Search Result that can be returned. See <see cref="SearchResult"/>.</param>
        /// <returns>Returns a collection of Search Result matching the query.</returns>
        public override IEnumerable<SearchResult> Search(string searchQuery, SearchContext context, SearchProvider provider, int maxScore = int.MaxValue, int patternMatchLimit = 2999)
        {
            if (settings.options.disabled)
                return Enumerable.Empty<SearchResult>();
            return base.Search(searchQuery, context, provider, maxScore, patternMatchLimit);
        }

        internal override IEnumerable<SearchResult> SearchWord(string word, SearchIndexOperator op, SearchResultCollection subset)
        {
            //using (new DebugTimer($"Search word {word}, {op}, {subset?.Count}"))
            {
                foreach (var r in base.SearchWord(word, op, subset))
                    yield return r;

                if (SearchSettings.findProviderIndexHelper)
                {
                    var baseScore = settings.baseScore;
                    var options = FindOptions.Words | FindOptions.Glob | FindOptions.Regex | FindOptions.FileName;
                    if (m_DoFuzzyMatch)
                        options |= FindOptions.Fuzzy;

                    var documents = subset != null ? subset.Select(r => GetDocument(r.index)) : GetDocuments(ignoreNulls: true);
                    foreach (var r in FindProvider.SearchWord(false, word, options, documents))
                    {
                        if (m_IndexByDocuments.TryGetValue(r.id, out var documentIndex))
                            yield return new SearchResult(r.id, documentIndex, baseScore + r.score + 5);
                    }
                }
            }
        }

        static bool ShouldIgnorePattern(in string pattern, in string path)
        {
            var filename = Path.GetFileName(path);
            return filename.StartsWith(pattern, StringComparison.Ordinal) || filename.EndsWith(pattern, StringComparison.Ordinal);
        }

        /// <summary>
        /// Called when the index is built to see if a specified document needs to be indexed. See <see cref="SearchIndexer.skipEntryHandler"/>
        /// </summary>
        /// <param name="path">Path of a document</param>
        /// <param name="checkRoots"></param>
        /// <returns>Returns true if the document doesn't need to be indexed.</returns>
        public override bool SkipEntry(string path, bool checkRoots = false)
        {
            if (string.IsNullOrEmpty(path))
                return true;

            // Skip files with ~ in their file path and built-in transient files
            if (BuiltInTransientFilePatterns.Any(pattern => ShouldIgnorePattern(pattern, path)))
                return true;

            if (checkRoots)
            {
                if (!GetRoots().Any(r => path.StartsWith(r, StringComparison.OrdinalIgnoreCase)))
                    return true;
            }

            var ext = Path.GetExtension(path);

            // Exclude some file extensions by default
            if (ext.Equals(".meta", StringComparison.Ordinal) ||
                ext.Equals(".index", StringComparison.Ordinal))
                return true;

            if (settings.includes?.Length > 0 || settings.excludes?.Length > 0)
            {
                var dir = Path.GetDirectoryName(path).Replace("\\", "/");

                if (settings.includes?.Length > 0 && !settings.includes.Any(pattern => PatternChecks(pattern, ext, dir, path)))
                    return true;

                if (settings.excludes?.Length > 0 && settings.excludes.Any(pattern => PatternChecks(pattern, ext, dir, path)))
                    return true;
            }

            return false;
        }

        /// <summary>
        ///  Get all this indexer root paths.
        /// </summary>
        /// <returns>Returns a list of root paths.</returns>
        internal abstract IEnumerable<string> GetRoots();

        /// <summary>
        /// Get all documents that would be indexed.
        /// </summary>
        /// <returns>Returns a list of file paths.</returns>
        internal abstract List<string> GetDependencies();

        /// <summary>
        /// Compute the hash of a specific document id. Generally a file path.
        /// </summary>
        /// <param name="id">Document id.</param>
        /// <returns>Returns the hash of this document id.</returns>
        internal abstract Hash128 GetDocumentHash(string id);

        /// <summary>
        /// Function to override in a concrete SearchIndexer to index the content of a document.
        /// </summary>
        /// <param name="id">Path of the document to index.</param>
        /// <param name="checkIfDocumentExists">Check if the document actually exists.</param>
        public abstract override void IndexDocument(string id, bool checkIfDocumentExists);

        /// <summary>
        /// Split a word into multiple components.
        /// </summary>
        /// <param name="documentIndex">Document where the indexed word was found.</param>
        /// <param name="word">Word to add to the index.</param>
        public void IndexWordComponents(int documentIndex, in string word)
        {
            int scoreModifier = 0;
            foreach (var c in GetEntryComponents(word, documentIndex))
                IndexWord(documentIndex, c, scoreModifier: scoreModifier++);
        }

        /// <summary>
        /// Split a value into multiple components.
        /// </summary>
        /// <param name="documentIndex">Document where the indexed word was found.</param>
        /// <param name="name">Key used to retrieve the value. See <see cref="SearchIndexer.AddProperty"/></param>
        /// <param name="value">Value to add to the index.</param>
        public void IndexPropertyComponents(int documentIndex, string name, string value)
        {
            using var _ = k_IndexPropertyComponentsMarker.Auto();
            int scoreModifier = 0;
            double number = 0;
            foreach (var c in GetEntryComponents(value, documentIndex))
            {
                var score = settings.baseScore + scoreModifier++;
                if (Utils.TryParse(c, out number))
                {
                    AddNumber(name, number, score, documentIndex);
                }
                else
                {
                    AddProperty(name, c, score, documentIndex, saveKeyword: false, exact: false);
                }
            }
            AddProperty(name, value.ToLowerInvariant(), settings.baseScore - 5, documentIndex, saveKeyword: false, exact: true);
        }

        /// <summary>
        /// Splits a string into multiple words that will be indexed.
        /// It works with paths and UpperCamelCase strings.
        /// </summary>
        /// <param name="entry">The string to be split.</param>
        /// <param name="documentIndex">The document index that will index that entry.</param>
        /// <returns>The entry components.</returns>
        public virtual IEnumerable<string> GetEntryComponents(in string entry, int documentIndex)
        {
            return SearchUtils.SplitFileEntryComponents(entry, SearchUtils.entrySeparators);
        }

        /// <summary>
        /// Add a new word coming from a specific document to the index. The word will be added with multiple variations allowing partial search. See <see cref="SearchIndexer.AddWord"/>.
        /// </summary>
        /// <param name="word">Word to add to the index.</param>
        /// <param name="documentIndex">Document where the indexed word was found.</param>
        /// <param name="maxVariations">Maximum number of variations to compute. Cannot be higher than the length of the word.</param>
        /// <param name="exact">If true, we will store also an exact match entry for this word.</param>
        /// <param name="scoreModifier">Modified to apply to the base score for a specific word.</param>
        public void IndexWord(int documentIndex, in string word, int maxVariations, bool exact, int scoreModifier = 0)
        {
            IndexWord(documentIndex, word, minWordIndexationLength, maxVariations, exact, scoreModifier);
        }

        internal void IndexWord(int documentIndex, in string word, int minVariations, int maxVariations, bool exact, int scoreModifier = 0)
        {
            using var _ = k_IndexWordMarker.Auto();
            var lword = word.ToLowerInvariant();
            var modifiedScore = settings.baseScore + scoreModifier;
            AddWord(lword, minVariations, maxVariations, modifiedScore, documentIndex);
            if (exact)
                AddExactWord(lword, modifiedScore, documentIndex);
        }

        /// <summary>
        /// Add a new word coming from a specific document to the index. The word will be added with multiple variations allowing partial search. See <see cref="SearchIndexer.AddWord"/>.
        /// </summary>
        /// <param name="word">Word to add to the index.</param>
        /// <param name="documentIndex">Document where the indexed word was found.</param>
        /// <param name="exact">If true, we will store also an exact match entry for this word.</param>
        /// <param name="scoreModifier">Modified to apply to the base score for a specific word.</param>
        public void IndexWord(int documentIndex, in string word, bool exact = false, int scoreModifier = 0)
        {
            IndexWord(documentIndex, word, word.Length, exact, scoreModifier: scoreModifier);
        }

        /// <summary>
        /// Add a property value to the index. A property is specified with a key and a string value. The value will be stored with multiple variations. See <see cref="SearchIndexer.AddProperty"/>.
        /// </summary>
        /// <param name="name">Key used to retrieve the value. See <see cref="SearchIndexer.AddProperty"/></param>
        /// <param name="value">Value to add to the index.</param>
        /// <param name="documentIndex">Document where the indexed word was found.</param>
        /// <param name="saveKeyword">Define if we store this key in the keyword registry of the index. See <see cref="SearchIndexer.GetKeywords"/>.</param>
        /// <param name="exact">If exact is true, only the exact match of the value will be stored in the index (not the variations).</param>
        public void IndexProperty(int documentIndex, string name, string value, bool saveKeyword, bool exact = false)
        {
            using var _ = k_IndexPropertyMarker.Auto();
            if (string.IsNullOrEmpty(value))
                return;
            var valueLower = value.ToLowerInvariant();
            if (exact)
            {
                AddProperty(name, valueLower, valueLower.Length, valueLower.Length, settings.baseScore, documentIndex, saveKeyword: saveKeyword, exact: true);
            }
            else
                AddProperty(name, valueLower, settings.baseScore, documentIndex, saveKeyword: saveKeyword);
        }

        public void IndexProperty<TProperty, TPropertyOwner>(int documentIndex, string name, string value, bool saveKeyword, bool exact)
        {
            IndexProperty(documentIndex, name, value, saveKeyword, exact);
            MapProperty(name, name, name, typeof(TProperty).AssemblyQualifiedName, typeof(TPropertyOwner).AssemblyQualifiedName, removeNestedKeys: true);
        }

        internal void IndexProperty<TProperty, TPropertyOwner>(int documentIndex, string name, string value, bool saveKeyword, bool exact, string keywordLabel, string keywordHelp)
        {
            IndexProperty(documentIndex, name, value, saveKeyword, exact);
            MapProperty(name, keywordLabel, keywordHelp, typeof(TProperty).AssemblyQualifiedName, typeof(TPropertyOwner).AssemblyQualifiedName, removeNestedKeys: true);
        }
        /// <summary>
        /// Add a key-number value pair to the index. The key won't be added with variations. See <see cref="SearchIndexer.AddNumber"/>.
        /// </summary>
        /// <param name="name">Key used to retrieve the value.</param>
        /// <param name="number">Number value to store in the index.</param>
        /// <param name="documentIndex">Document where the indexed value was found.</param>
        public void IndexNumber(int documentIndex, string name, double number)
        {
            AddNumber(name, number, settings.baseScore, documentIndex);
        }

        internal static FilePattern GetFilePattern(string pattern)
        {
            if (!string.IsNullOrEmpty(pattern))
            {
                if (pattern[0] == '.')
                    return FilePattern.Extension;
                if (pattern[pattern.Length - 1] == '/')
                    return FilePattern.Folder;
            }
            return FilePattern.File;
        }

        private bool PatternChecks(string pattern, string ext, string dir, string filePath)
        {
            var filePattern = GetFilePattern(pattern);

            switch (filePattern)
            {
                case FilePattern.Extension:
                    return ext.Equals(pattern, StringComparison.OrdinalIgnoreCase);
                case FilePattern.Folder:
                    var icDir = pattern.Substring(0, pattern.Length - 1);
                    return dir.IndexOf(icDir, StringComparison.OrdinalIgnoreCase) != -1;
                case FilePattern.File:
                    return filePath.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) != -1;
            }

            return false;
        }

        /// <summary>
        /// Index all the properties of an object.
        /// </summary>
        /// <param name="obj">Object to index.</param>
        /// <param name="documentIndex">Document where the indexed object was found.</param>
        /// <param name="dependencies">Index dependencies.</param>
        protected void IndexObject(int documentIndex, Object obj, bool dependencies = false)
        {
            IndexObject(documentIndex, obj, dependencies, recursive: false);
        }

        internal void IndexObject(int documentIndex, Object obj, bool dependencies, bool recursive)
        {
            using var _ = k_IndexObjectMarker.Auto();
            using (var so = new SerializedObject(obj))
            {
                var p = so.GetIterator();
                const int maxDepth = 1;
                IndexVisibleProperties(documentIndex, p, recursive, maxDepth);
                IndexNonVisibleProperties(documentIndex, so, maxDepth);
            }
        }

        private bool ShouldIndexChildren(SerializedProperty p, bool recursive)
        {
            // Degenerative built-in types to skip.
            if (p.propertyType == SerializedPropertyType.Generic)
            {
                switch (p.type)
                {
                    case "SerializedProperties":
                    case "VFXPropertySheetSerializedBase":
                    case "ComputeShaderCompilationContext":
                        return false;
                }
            }

            if (p.depth > 2 && !recursive)
                return false;

            if ((p.isArray || p.isFixedBuffer) && !recursive)
                return false;

            if (p.propertyType == SerializedPropertyType.Vector2 ||
                p.propertyType == SerializedPropertyType.Vector3 ||
                p.propertyType == SerializedPropertyType.Vector4)
                return false;

            return p.hasVisibleChildren;
        }

        internal static string GetFieldName(string propertyName)
        {
            return propertyName.Replace("m_", "").Replace(" ", "").ToLowerInvariant();
        }

        static Func<SerializedProperty, bool> s_IndexAllPropertiesFunc = p => true;

        internal void IndexVisibleProperties(int documentIndex, in SerializedProperty p, bool recursive, int maxDepth)
        {
            IndexVisibleProperties(documentIndex, p, recursive, maxDepth, s_IndexAllPropertiesFunc);
        }

        internal static bool IsIndexableProperty(in SerializedPropertyType type)
        {
            switch (type)
            {
                // Unsupported property types:
                case SerializedPropertyType.Generic:
                case SerializedPropertyType.Bounds:
                case SerializedPropertyType.BoundsInt:
                case SerializedPropertyType.Rect:
                case SerializedPropertyType.RectInt:
                case SerializedPropertyType.Vector2Int:
                case SerializedPropertyType.Vector3Int:
                case SerializedPropertyType.LayerMask:
                case SerializedPropertyType.AnimationCurve:
                case SerializedPropertyType.Gradient:
                case SerializedPropertyType.ExposedReference:
                case SerializedPropertyType.ManagedReference:
                case SerializedPropertyType.FixedBufferSize:
                    return false;
                default:
                    return true;
            }
        }

        internal void IndexVisibleProperties(int documentIndex, in SerializedProperty p, bool recursive, int maxDepth, Func<SerializedProperty, bool> shouldContinueIterating)
        {
            using var _ = k_IndexPropertiesMarker.Auto();
            string componentPrefix = null;
            if (p.serializedObject?.targetObject is Component c)
                componentPrefix = c.GetType().Name + ".";
            var next = p.NextVisible(true);
            while (next)
            {
                IndexPropertyIfIndexable(documentIndex, componentPrefix, p, maxDepth, PropertyWithPrefixIndexing.All);
                next = shouldContinueIterating(p) && p.NextVisible(ShouldIndexChildren(p, recursive));
            }
        }

        internal void IndexNonVisibleProperties(int documentIndex, in SerializedObject so, int maxDepth)
        {
            if (so.targetObject is not Component c)
                return;
            if (c is Transform)
                return;

            var propertyPrefix = c.GetType().Name + ".";
            var sp = so.FindProperty("m_Enabled");
            if (sp is { isValid: true })
            {
                IndexPropertyIfIndexable(documentIndex, propertyPrefix, sp, maxDepth, PropertyWithPrefixIndexing.All);
            }
        }

        internal void IndexPropertyIfIndexable(int documentIndex, in string propertyPrefix, in SerializedProperty p, int maxDepth, PropertyWithPrefixIndexing indexingOptions = PropertyWithPrefixIndexing.NoPrefix)
        {
            var fieldName = GetFieldName(p.displayName);
            var indexWithPrefix = indexingOptions.HasAny(PropertyWithPrefixIndexing.WithPrefix) && !string.IsNullOrEmpty(propertyPrefix);
            if (IsIndexableProperty(p.propertyType) && p.propertyPath[p.propertyPath.Length - 1] != ']')
            {
                if (indexingOptions.HasAny(PropertyWithPrefixIndexing.NoPrefix))
                    IndexProperty(documentIndex, fieldName, p, maxDepth, indexWithPrefix ? SearchPropositionGenerationOptions.HideInQueryBuilderMode : SearchPropositionGenerationOptions.None);
                if (indexWithPrefix)
                    IndexProperty(documentIndex, GetFieldName(propertyPrefix) + fieldName, p, maxDepth, SearchPropositionGenerationOptions.None);
            }
        }

        internal void IndexProperty(in int documentIndex, in string fieldName, in SerializedProperty p, int maxDepth, SearchPropositionGenerationOptions propositionGenerationOptions)
        {
            using var _ = k_IndexSerializedPropertyMarker.Auto();
            if (ignoredProperties.Contains(fieldName))
                return;

            if (ignoredProperties.Contains(p.type))
                return;

            if (p.depth <= 1 && p.isArray && p.propertyType != SerializedPropertyType.String)
            {
                IndexNumber(documentIndex, fieldName, p.arraySize);
                LogProperty(fieldName, p, propositionGenerationOptions, p.arraySize);
            }

            if (p.depth > maxDepth)
                return;

            switch (p.propertyType)
            {
                case SerializedPropertyType.ArraySize:
                case SerializedPropertyType.Character:
                case SerializedPropertyType.Integer:
                    IndexNumber(documentIndex, fieldName, (double)p.intValue);
                    var managedType = p.GetManagedType();
                    if (managedType?.IsEnum == true)
                    {
                        var values = Enum.GetValues(managedType);
                        for (int i = 0; i < values.Length; i++)
                        {
                            var v = (int)values.GetValue(i);
                            if (v == p.intValue)
                            {
                                IndexEnum(documentIndex, fieldName, p, propositionGenerationOptions, Enum.GetNames(managedType)[i]);
                                break;
                            }
                        }
                    }
                    else if (managedType == typeof(bool))
                    {
                        var boolStringValue = p.intValue == 0 ? "false" : "true";
                        IndexProperty(documentIndex, fieldName, boolStringValue, saveKeyword: false, exact: true);
                        LogProperty(fieldName, p, propositionGenerationOptions, boolStringValue);
                    }
                    else
                        LogProperty(fieldName, p, propositionGenerationOptions, p.intValue);
                    break;
                case SerializedPropertyType.Boolean:
                    IndexProperty(documentIndex, fieldName, p.boolValue.ToString().ToLowerInvariant(), saveKeyword: false, exact: true);
                    LogProperty(fieldName, p, propositionGenerationOptions, p.boolValue);
                    break;
                case SerializedPropertyType.Float:
                    IndexNumber(documentIndex, fieldName, (double)p.floatValue);
                    LogProperty(fieldName, p, propositionGenerationOptions, p.floatValue);
                    break;
                case SerializedPropertyType.String:
                    if (IndexPropertyStringComponents(documentIndex, fieldName, p.stringValue))
                        LogProperty(fieldName, p, propositionGenerationOptions, p.stringValue);
                    break;
                case SerializedPropertyType.Enum:
                    if (p.enumValueIndex < 0 || p.type != "Enum")
                        return;
                    IndexEnum(documentIndex, fieldName, p, propositionGenerationOptions, p.enumNames[p.enumValueIndex]);
                    break;
                case SerializedPropertyType.Color:
                    IndexerExtensions.IndexColor(fieldName, p.colorValue, this, documentIndex);
                    LogProperty(fieldName, p, propositionGenerationOptions, p.colorValue);
                    break;
                case SerializedPropertyType.Vector2:
                    IndexerExtensions.IndexVector(fieldName, p.vector2Value, this, documentIndex);
                    LogProperty(fieldName, p, propositionGenerationOptions, p.vector2Value);
                    break;
                case SerializedPropertyType.Vector3:
                    IndexerExtensions.IndexVector(fieldName, p.vector3Value, this, documentIndex);
                    LogProperty(fieldName, p, propositionGenerationOptions, p.vector3Value);
                    break;
                case SerializedPropertyType.Vector4:
                    IndexerExtensions.IndexVector(fieldName, p.vector4Value, this, documentIndex);
                    LogProperty(fieldName, p, propositionGenerationOptions, p.vector4Value);
                    break;
                case SerializedPropertyType.Quaternion:
                    IndexerExtensions.IndexVector(fieldName, p.quaternionValue.eulerAngles, this, documentIndex);
                    LogProperty(fieldName, p, propositionGenerationOptions, p.quaternionValue.eulerAngles);
                    break;
                case SerializedPropertyType.ObjectReference:
                    AddReference(documentIndex, fieldName, p.objectReferenceValue);
                    LogProperty(fieldName, p, propositionGenerationOptions, p.objectReferenceValue);
                    break;
                case SerializedPropertyType.Hash128:
                    IndexProperty(documentIndex, fieldName, p.hash128Value.ToString().ToLowerInvariant(), saveKeyword: true, exact: true);
                    LogProperty(fieldName, p, propositionGenerationOptions, p.hash128Value);
                    break;
            }
        }

        void IndexEnum(in int documentIndex, in string fieldName, in SerializedProperty p, SearchPropositionGenerationOptions propositionGenerationOptions, string enumValue)
        {
            enumValue = enumValue.Replace(" ", "");
            IndexProperty(documentIndex, fieldName, enumValue, saveKeyword: true, exact: true);
            LogProperty(fieldName, p, propositionGenerationOptions, enumValue);
        }

        bool IndexPropertyStringComponents(in int documentIndex, in string fieldName, in string sv)
        {
            using var _ = k_IndexPropertyStringComponentsMarker.Auto();
            if (string.IsNullOrEmpty(sv) || sv.Length > 64)
                return false;
            if (sv.Length > 4 && sv.Length < 32 && char.IsLetter(sv[0]) && sv.IndexOf(' ') == -1)
                IndexPropertyComponents(documentIndex, fieldName, sv);
            else
                IndexProperty(documentIndex, fieldName, sv, saveKeyword: false, exact: true);
            return true;
        }

        void LogProperty(in string fieldName, in SerializedProperty p, SearchPropositionGenerationOptions propositionGenerationOptions = SearchPropositionGenerationOptions.None, object value = null)
        {
            using var _ = k_LogPropertyMarker.Auto();
            var propertyType = SearchUtils.GetPropertyManagedTypeString(p);
            if (propertyType != null)
                MapProperty(fieldName, p.displayName, p.tooltip, propertyType, p.serializedObject?.targetObject?.GetType().AssemblyQualifiedName, propositionGenerationOptions, removeNestedKeys: true);
        }

        internal void AddReference(int documentIndex, string assetPath, bool saveKeyword = false)
        {
            using var _ = k_AddReferenceMarker.Auto();
            if (string.IsNullOrEmpty(assetPath))
                return;

            IndexProperty(documentIndex, "ref", assetPath, saveKeyword, exact: true);
            var assetInstanceID = Utils.GetMainAssetInstanceID(assetPath);
            var gid = GlobalObjectId.GetGlobalObjectIdSlow(assetInstanceID);
            if (gid.identifierType != 0)
                IndexProperty(documentIndex, "ref", gid.ToString(), saveKeyword, exact: true);
            if (settings.options.properties)
                IndexPropertyStringComponents(documentIndex, "ref", Path.GetFileNameWithoutExtension(assetPath));
        }

        internal void AddReference(int documentIndex, string propertyName, Object objRef, in string label = null, in Type ownerPropertyType = null)
        {
            using var _ = k_AddObjectReferenceMarker.Auto();
            if (!objRef)
            {
                if (settings.options.properties)
                    IndexProperty(documentIndex, propertyName, "none", saveKeyword: false, exact: true);
                return;
            }

            var assetPath = SearchUtils.GetObjectPath(objRef);
            if (string.IsNullOrEmpty(assetPath))
                return;

            propertyName = propertyName.ToLowerInvariant();
            IndexProperty(documentIndex, propertyName, assetPath, saveKeyword: false, exact: true);

            var gid = GlobalObjectId.GetGlobalObjectIdSlow(objRef);
            if (gid.identifierType != 0)
                IndexProperty(documentIndex, "ref", gid.ToString(), saveKeyword: false, exact: true);

            if (settings.options.dependencies)
                IndexProperty(documentIndex, "ref", assetPath, saveKeyword: false, exact: true);
            if (settings.options.properties)
            {
                IndexPropertyStringComponents(documentIndex, propertyName, Path.GetFileNameWithoutExtension(Utils.RemoveInvalidCharsFromPath(objRef.name ?? assetPath, '_')));
                if (label != null && ownerPropertyType != null)
                    MapProperty(propertyName, label, null, objRef.GetType().AssemblyQualifiedName, ownerPropertyType.AssemblyQualifiedName);
            }
        }

        /// <summary>
        /// Call all the registered custom indexer for a specific object. See <see cref="CustomObjectIndexerAttribute"/>.
        /// </summary>
        /// <param name="documentId">Document index.</param>
        /// <param name="documentIndex">Document where the indexed object was found.</param>
        /// <param name="obj">Object to index.</param>
        internal void IndexCustomProperties(string documentId, int documentIndex, Object obj)
        {
            using var _ = k_IndexCustomPropertiesMarker.Auto();
            using (var so = new SerializedObject(obj))
            {
                CallCustomIndexers(documentId, documentIndex, obj, so);
            }
        }

        /// <summary>
        /// Call all the registered custom indexer for an object of a specific type. See <see cref="CustomObjectIndexerAttribute"/>.
        /// </summary>
        /// <param name="documentId">Document id.</param>
        /// <param name="obj">Object to index.</param>
        /// <param name="documentIndex">Document where the indexed object was found.</param>
        /// <param name="so">SerializedObject representation of obj.</param>
        /// <param name="multiLevel">If true, calls all the indexer that would fit the type of the object (all assignable type). If false only check for an indexer registered for the exact type of the Object.</param>
        private void CallCustomIndexers(string documentId, int documentIndex, Object obj, SerializedObject so, bool multiLevel = true)
        {
            var objectType = obj.GetType();
            List<CustomIndexerHandler> customIndexers;
            if (!multiLevel)
            {
                if (!CustomIndexers.TryGetValue(objectType, out customIndexers))
                    return;
            }
            else
            {
                customIndexers = new List<CustomIndexerHandler>();
                foreach (var indexerType in CustomIndexers.types)
                {
                    if (indexerType.IsAssignableFrom(objectType))
                        customIndexers.AddRange(CustomIndexers.GetHandlers(indexerType));
                }
            }

            var indexerTarget = new CustomObjectIndexerTarget
            {
                id = documentId,
                documentIndex = documentIndex,
                target = obj,
                serializedObject = so,
                targetType = objectType
            };

            foreach (var customIndexer in customIndexers)
            {
                try
                {
                    customIndexer(indexerTarget, this);
                }
                catch(System.Exception e)
                {
                    Debug.LogError($"Error while using custom asset indexer: {e}");
                }
            }
        }

        /// <summary>
        /// Checks if we have a custom indexer for the specified type.
        /// </summary>
        /// <param name="type">Type to lookup</param>
        /// <param name="multiLevel">Check for subtypes too.</param>
        /// <returns>True if a custom indexer exists, otherwise false is returned.</returns>
        internal bool HasCustomIndexers(Type type, bool multiLevel = true)
        {
            return CustomIndexers.HasCustomIndexers(type, multiLevel);
        }

        private protected override void OnFinish()
        {
            IndexerExtensions.ClearIndexerCaches(this);
        }
    }
}
