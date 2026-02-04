// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace UnityEditor.Search
{
    [Serializable]
    public class SearchTable
    {
        [SerializeField] public string id;
        [SerializeField] public string name;
        [SerializeField] public SearchColumn[] columns;
        [SerializeField] public float itemHeight = 22f;

        public SearchTable(string id, string name, IEnumerable<SearchColumn> columnModels)
        {
            this.id = id;
            this.name = name;
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            columns = columnModels == null ? Array.Empty<SearchColumn>() : columnModels.Where(c => c != null).ToArray();
#pragma warning restore UA2001
            InitFunctors();
        }

        public SearchTable(string name, IEnumerable<SearchColumn> columnModels)
            : this(Guid.NewGuid().ToString("N"), name, columnModels)
        {
        }

        public SearchTable(SearchTable other, string newName = null)
            : this(newName ?? other.name, other.columns)
        {
        }

        public void Assign(SearchTable table)
        {
            itemHeight = table.itemHeight;
            if (table.columns != null)
                #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
                columns = table.columns.ToArray();
#pragma warning restore UA2001
        }

        public SearchTable Clone(string newName = null)
        {
            if (columns == null || columns.Length == 0)
                return null;
            return new SearchTable(this, newName) { itemHeight = itemHeight };
        }

        public void InitFunctors()
        {
            // This is called to ensure members not serialized are properly init.
            foreach (var searchColumn in columns)
                searchColumn.InitFunctors();
        }

        internal static SearchTable CreateDefault(IEnumerable<SearchItem> items = null)
        {
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return new SearchTable("Default", ItemSelectors.Enumerate(items)
#pragma warning restore UA2001
                .Select(c => { c.options |= SearchColumnFlags.Volatile; return c; }));
        }

        internal static SearchTable Import(string sessionTableConfigData)
        {
            var tc = JsonUtility.FromJson<SearchTable>(sessionTableConfigData);
            tc.InitFunctors();
            return tc;
        }

        internal string Export(bool format = false)
        {
            return JsonUtility.ToJson(this, format);
        }

        public static SearchTable LoadFromFile(string stcPath)
        {
            try
            {
                return Import(File.ReadAllText(stcPath));
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load table configuration {stcPath.Replace("\\", "/")}\r\n{ex}");
            }

            return null;
        }

        public override string ToString()
        {
            #pragma warning disable UA2001 // The Banned API Analyzer produces compile errors for any new Linq code. This pre-existing usage has been suppressed, but should be rewritten if possible.
            return $"{name} ({id}): {string.Join(", ", columns.Select(c => c.name))}";
#pragma warning restore UA2001
        }
    }
}
