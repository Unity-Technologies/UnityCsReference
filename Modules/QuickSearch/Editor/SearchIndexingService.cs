// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;

namespace UnityEditor.Search
{
    static class SearchIndexingService
    {
        private static bool m_IsIndexReadyOverride;
        private static bool m_IsIndexReady;

        public static bool IsDeepIndexingEnabled()
        {
            var db = SearchDatabase.GetDefaultSearchDatabase();
            return db.settings.options.extended;
        }

        public static bool IsPackageIndexingEnabled()
        {
            var db = SearchDatabase.GetDefaultSearchDatabase();
            foreach(var root in db.settings.roots)
            {
                if (root.Equals("Packages", StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }
            return false;
        }

        public static void ChangeIndexingSettings(bool deepIndexing, bool packageIndexing, Action indexingReady = null)
        {
            var settingsDirty = false;
            var db = SearchDatabase.GetDefaultSearchDatabase();
            if (db.settings.options.extended != deepIndexing)
            {
                db.settings.options.extended = deepIndexing;
                settingsDirty = true;
            }

            if (IsPackageIndexingEnabled() != packageIndexing)
            {
                var roots = new List<string>();
                foreach (var root in db.settings.roots)
                {
                    if (root.Equals("Packages", StringComparison.InvariantCultureIgnoreCase))
                        continue;
                    roots.Add(root);
                }
                if (packageIndexing)
                {
                    roots.Add("Packages");
                }
                db.settings.roots = roots.ToArray();
                settingsDirty = true;
            }

            if (settingsDirty)
                db.SaveSettingsOptions(true);

            if (indexingReady != null)
                WaitForForIndexReady(indexingReady);
        }

        public static bool IsIndexReady()
        {
            if (m_IsIndexReadyOverride)
                return m_IsIndexReady;
            return SearchDatabase.GetDefaultSearchDatabase().ready;
        }

        public static void WaitForForIndexReady(Action indexingReady)
        {
            if (IsIndexReady())
            {
                indexingReady?.Invoke();
            }
            else
            {
                Utils.CallDelayed(() => WaitForForIndexReady(indexingReady), 1d);
            }
        }

        public static void ForceRebuildIndex()
        {
            SearchDatabase.ForceRebuildIndex(SearchDatabase.GetDefaultSearchDatabase());
        }

        #region Test API
        internal static void SetIndexReadyOverride(bool useOverride, bool isIndexReady)
        {
            m_IsIndexReadyOverride = useOverride;
            m_IsIndexReady = isIndexReady;
        }
        #endregion
    }
}
