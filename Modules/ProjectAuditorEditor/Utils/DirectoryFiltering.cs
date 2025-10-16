// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Unity.ProjectAuditor.Editor.Utils
{
    internal enum FilteringMode
    {
        Allow,
        Block
    }

    internal static class DirectoryFiltering
    {
        public readonly struct Rule
        {
            public readonly FilteringMode filterMode;
            public readonly string filterPath;
            public readonly int precedence;
            readonly string[] m_SearchComponents;

            public Rule(FilteringMode mode, string directory)
            {
                filterMode = mode;
                m_SearchComponents = CreateFilterComponentsForString(directory);
                string actualDirectory = "";
                for (var i = 0; i < (m_SearchComponents?.Length ?? 0); i++)
                {
                    actualDirectory += "/";
                    actualDirectory += m_SearchComponents ? [i];
                }
                filterPath = m_SearchComponents == null ? null : actualDirectory;
                precedence = m_SearchComponents?.Length ?? -1;
            }

            public Predicate<string> CreateMatchesPredicate()
            {
                return DirectoryFiltering.CreateMatchesPredicate(m_SearchComponents);
            }

            public static Predicate<string> CreateFilterPredicateForRules(Rule[] rules, bool defaultReturn = false)
            {
                if (rules == null || rules.Length == 0)
                {
                    return defaultReturn ? (Predicate<string>)null : _ => false;
                }
                int totalFilters = 0;
                var filterFunctions = new Tuple<Predicate<string>, FilteringMode>[rules.Length];
                foreach (var rule in rules)
                {
                    var filterFunction = rule.CreateMatchesPredicate();
                    if (filterFunction != null)
                    {
                        filterFunctions[totalFilters] = new Tuple<Predicate<string>, FilteringMode>(filterFunction, rule.filterMode);
                        totalFilters++;
                    }
                }

                if (totalFilters == 0)
                {
                    return defaultReturn ? (Predicate<string>)null : _ => false;
                }

                // code analysis in particular calls a lot of filtering for the same file
                string lastPath = null;
                bool lastResult = false;
                return pathStr =>
                {
                    if (pathStr.Equals(lastPath))
                    {
                        return lastResult;
                    }
                    lastPath = pathStr;

                    for (int i = 0; i < totalFilters; i++)
                    {
                        var result = filterFunctions[i].Item1(pathStr);
                        if (result)
                        {
                            return lastResult = filterFunctions[i].Item2 == FilteringMode.Allow;
                        }
                    }

                    return lastResult = defaultReturn;
                };
            }
        }

        public static Predicate<string> CreateFilterPredicate(this Rule[] rules, bool defaultReturn = false)
        {
            return Rule.CreateFilterPredicateForRules(rules, defaultReturn);
        }

        public static string[] CreateFilterComponentsForString(string filterPath)
        {
            if (filterPath == null || filterPath.Replace('/', ' ').Trim().Length == 0)
            {
                return null;
            }
            var directoryComponents = filterPath.ToLowerInvariant().Split('/');
            int validEntries = 0;
            for (int j = 0; j < directoryComponents.Length; j++)
            {
                var component = directoryComponents[j]?.Trim();
                switch (component)
                {
                    case null :
                    case "":
                    case ".":
                    {
                        directoryComponents[j] = null;
                        continue;
                    }
                    case "..":
                    {
                        directoryComponents[j] = null;
                        for (int k = j - 1; k >= 0; k--)
                        {
                            // remove last directory entry, even if that requires stepping back a few times
                            if (directoryComponents[k] != null)
                            {
                                directoryComponents[k] = null;
                                validEntries--;
                                break;
                            }
                        }
                        continue;
                    }
                    default:
                    {
                        validEntries++;
                        directoryComponents[j] = component;
                        break;
                    }
                }
            }
            if (validEntries == 0)
            {
                return null;
            }
            var searchComponents = new string[validEntries];
            for (int j = 0, k = 0; k < validEntries && j < directoryComponents.Length; j++)
            {
                if (directoryComponents[j] == null)
                {
                    continue;
                }

                searchComponents[k] = directoryComponents[j];
                k++;
            }
            return searchComponents;
        }

        public static Predicate<string> CreateMatchesPredicate(string filterPath)
        {
            return CreateMatchesPredicate(CreateFilterComponentsForString(filterPath));
        }

        public static Predicate<string> CreateMatchesPredicate(string[] filterComponents)
        {
            if (filterComponents == null || filterComponents.Length == 0)
            {
                return null;
            }

            foreach (var component in filterComponents)
            {
                if (component == null)
                {
                    return null;
                }
            }

            if (filterComponents.Length == 1 && filterComponents[0] == "*")
            {
                return _ => true;
            }
            return path =>
            {
                // to avoid allocing strings here, im walking them myself

                int pathLength = path.Length;
                int currentComponentIndex = 0;
                int componentCharacterIndex = 0;
                string currentComponent = filterComponents[0];
                for (int j = 0; j < pathLength; j++)
                {
                    char currentCharacter;
                    char expectedCharacter = componentCharacterIndex < currentComponent.Length ? currentComponent[componentCharacterIndex] : (char)0;

                    if (expectedCharacter == '*')
                    {
                        if (currentComponentIndex == filterComponents.Length - 1 && currentComponent.Length == 1)
                        {
                            // a trailing asterisk will match anything, skip looking at it at all
                            return true;
                        }
                        if (currentComponent.Length - 1 != componentCharacterIndex)
                        {
                            int startingSearchCharacter = j;
                            int startingIndex = componentCharacterIndex;

                            while (true)
                            {
                                if (j >= pathLength)
                                {
                                    if (currentComponentIndex == filterComponents.Length - 1)
                                    {
                                        // finished last component and path at the same time, a match at end that matches at the end of the last component of a path will hit this
                                        // ie: filtering for file extensions '*.png'
                                        if (componentCharacterIndex == currentComponent.Length - 1)
                                        {
                                            return true;
                                        }
                                        // similar to above, but with a trailing asterisks, will also make it a match anywhere
                                        // ie: using '*.json*' to match both .json and .json5 files
                                        //     or for '*CharacterName*' to match both 'ArmTexture_CharacterName.png' and 'ArmModel_CharacterName.fbx'
                                        if (componentCharacterIndex == currentComponent.Length - 2 && currentComponent[currentComponent.Length - 1] == '*')
                                        {
                                            return true;
                                        }
                                    }

                                    // end of asset path without finding a match
                                    return false;
                                }

                                // full component matched, advances to next search component
                                componentCharacterIndex++;
                                if (componentCharacterIndex >= currentComponent.Length)
                                {
                                    if (path[j] != '/')
                                    {
                                        // reset the search from here, try and find it at the end of the component
                                        componentCharacterIndex = 0;
                                        continue;
                                    }

                                    currentComponentIndex++;
                                    if (currentComponentIndex >= filterComponents.Length || path[j] != '/')
                                    {
                                        // returns if this was the last component, or the match wasn't at the end of the component
                                        return j == pathLength - 1;
                                    }
                                    componentCharacterIndex = 0;
                                    currentComponent = filterComponents[currentComponentIndex];
                                    break;
                                }

                                currentCharacter = path[j];
                                expectedCharacter = currentComponent[componentCharacterIndex];

                                // if expecting an asterisk, this pattern is done and a new one will be started
                                // walk back a character and restart the loop handles chained match anywhere and any remainder
                                if (expectedCharacter == '*')
                                {
                                    j--;
                                    break;
                                }
                                // reached the end of this component in the path, without finding a match
                                if (currentCharacter == '/')
                                {
                                    return false;
                                }
                                j++;
                                currentCharacter = FastToLower(currentCharacter);
                                if (currentCharacter != expectedCharacter)
                                {
                                    // when a match fails, we need to walk back to the next potential starting character
                                    // this covers cases like looking for '*TToo' in "TTTooo", without walking back that lookup would fail
                                    // also covers other cases where a match failure happens at the first character that would succeed (ie: '*String' in 'TestString')
                                    j = ++startingSearchCharacter;
                                    startingSearchCharacter = j;
                                    componentCharacterIndex = startingIndex;
                                }
                            }
                            continue;
                        }

                        for (; j < pathLength; j++)
                        {
                            if (path[j] == '/')
                            {
                                break;
                            }
                        }
                        currentComponentIndex++;
                        componentCharacterIndex = 0;
                        if (currentComponentIndex >= filterComponents.Length)
                        {
                            break;
                        }
                        if (j >= pathLength)
                        {
                            return false;
                        }
                        currentComponent = filterComponents[currentComponentIndex];
                        continue;
                    }

                    currentCharacter = path[j];

                    if (currentCharacter == '/')
                    {
                        if (j == 0)
                        {
                            // special case for leading '/' being ignored
                            continue;
                        }
                        // reached end of current path component before reaching the end of the filter component
                        if (currentComponent.Length != componentCharacterIndex)
                        {
                            return false;
                        }

                        currentComponentIndex++;
                        componentCharacterIndex = 0;

                        // path has more components than filter path has, this is a match
                        if (currentComponentIndex >= filterComponents.Length)
                        {
                            break;
                        }
                        currentComponent = filterComponents[currentComponentIndex];
                        continue;
                    }

                    // normal character, no special handling
                    if (componentCharacterIndex >= currentComponent.Length)
                    {
                        return false;
                    }
                    currentCharacter = FastToLower(currentCharacter);
                    if (currentCharacter != expectedCharacter)
                    {
                        return false;
                    }
                    componentCharacterIndex++;
                }

                // in the event of a path with a component is the beginning of the last filter component
                // it will leave the above loop before reaching the end of the component
                // ex: when using a filter of '/Assets/ModelsUnused', '/Assets/Models' will pass the above checks, and be caught here
                if (currentComponentIndex == filterComponents.Length - 1)
                {
                    // ensure we didnt stop short of the last component
                    if (componentCharacterIndex < currentComponent.Length)
                    {
                        // stopping short of the last character of last component, when thats an asterisk, is a match
                        return componentCharacterIndex == currentComponent.Length - 1 && currentComponent[componentCharacterIndex] == '*';
                    }
                }
                else if (currentComponentIndex < filterComponents.Length)
                {
                    return false;
                }

                return true;
            };
        }

        static readonly TextInfo m_InvariantTextInfo = CultureInfo.InvariantCulture.TextInfo;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static char FastToLower(char character)
        {
            if (character >= '\u0080')
            {
                // non-ascii character, use the standard ToLower
                return m_InvariantTextInfo.ToLower(character);
            }

            if ('A' <= character && character <= 'Z')
            {
                // sets bit 6, adding 32, or 'a' - 'A'
                // ' ' is 32, but a char
                // see TextInfo.ToLowerAsciiInvariant
                character |= ' ';
            }
            return character;
        }
    }
}
