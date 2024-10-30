using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Serialization;
using static NiceIO.NPath;
using FILETIME = System.Runtime.InteropServices.ComTypes.FILETIME;

//Lets make it hard to accidentally use System.IO.File & System.IO.Directly, and require that it is always completely spelled out.
using File = NiceIO.Do_Not_Use_File_Directly_Use_FileSystem_Active_Instead;
using Directory = NiceIO.Do_Not_Use_Directory_Directly_Use_FileSystem_Active_Instead;

namespace NiceIO
{
    /// <summary>
    /// A filesystem path.
    /// </summary>
    /// <remarks>
    /// The path can be absolute or relative; the entity it refers to could be a file or a directory, and may or may not
    /// actually exist in the filesystem.
    /// </remarks>

    //niceio gets included willy nilly by various projects, and they end up adding NPath to their public API without realizing it, which can lead to
    //very annoying incompatibilities if you're using two projects that happen to do this. Let's make sure the default that it will not be public,
    //and in the one or two places that we want it to be public be explicit about doing so
    [DebuggerDisplay("{" + nameof(_path) + "}")]
    [DataContract]
    internal class NPath
        : IComparable, IEquatable<NPath>
    {
        // Assume FS is case sensitive on Linux, and case insensitive on macOS and Windows.

        //WARNING: Do not use FileSystem.Active to look for /proc. Since we're doing this in a static initializer, the k_IsWindows is not guaranteed to be set yet,
        //and you can get very hard to track down situations where the currently active filesystem on windows is actually set to PosixFileSystem.
        static readonly bool k_IsCaseSensitiveFileSystem = !CalculateIsWindows() && System.IO.Directory.Exists("/proc");

        static readonly bool k_IsWindows = CalculateIsWindows();

        private static bool CalculateIsWindows() => Environment.OSVersion.Platform == PlatformID.Win32Windows || Environment.OSVersion.Platform == PlatformID.Win32NT;

        private static bool CalculateIsWindows10()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                return false;

            // Environment.OSVersion will only return versions higher than 6.2 if the owning process has been manifested as compatible with Windows 10:
            // https://docs.microsoft.com/en-us/windows/win32/w8cookbook/windows-version-check
            //
            // Because NiceIO is a library, not its own executable, we are at the mercy of the consumer processes to manifest themselves correctly - and
            // many of them don't. So, using Environment.OSVersion is not really safe.
            //
            // StackOverflow suggests using the file version info for a core OS file, such as kernel32.dll:
            // https://stackoverflow.com/a/44665238/860530

            var kernel32 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.SystemX86), "kernel32.dll");
            var versionInfo = FileVersionInfo.GetVersionInfo(kernel32);

            return versionInfo.ProductMajorPart >= 10;
        }

        static readonly StringComparison PathStringComparison =
            k_IsCaseSensitiveFileSystem ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        [DataMember]
        private readonly string _path;

        static NPath Empty => new NPath("");

        #region construction

        /// <summary>
        /// Create a new NPath.
        /// </summary>
        /// <param name="path">The path that this NPath should represent.</param>
        public NPath(string path)
        {
            if (path == null)
                throw new ArgumentNullException();
            _path = MakeCompletelyWellFormatted(path);
        }

        //keep this private, we need to guarantee all NPath's out there are guaranteed well formed.
        private NPath(string path, bool guaranteed_well_formed)
        {
            if (!guaranteed_well_formed)
                throw new ArgumentException("For not well formed paths, use the public NPath constructor");

            _path = path;
        }

        static bool IsUNCPath(string path)
        {
            return path.Length > 2 && path[0] == '\\' && path[1] == '\\';
        }

        static string ConvertToForwardSlashPath(string path)
        {
            if (IsUNCPath(path)) // UNC path
                return @"\\" + path.Substring(2).Replace(@"\", @"/");
            return path.Replace(@"\", @"/");
        }

        static string MakeCompletelyWellFormatted(string path, bool doubleDotsAreCollapsed = false)
        {
            if (path == ".")
                return ".";
            if (path.Length == 0)
                return ".";

            var numberOfForwardSlashes = 0;
            var hasNonDotsOrSeparators = false;
            var startsWithDot = false;
            char previousChar = '\0';
            for (int i = 0; i != path.Length; i++)
            {
                var c = path[i];
                var nextChar = path.Length > (i + 1) ? path[i + 1] : '\0';
                var isDot = c == '.';
                if (isDot && i == 0)
                    startsWithDot = true;
                var isSlash = IsSlash(c);
                hasNonDotsOrSeparators |= !isDot && !isSlash;

                // MakeCompletelyWellFormatted + CollapseDoubleDots is fairly expensive, so only do it when needed:
                // If we have a "..", that is not just a bunch of "../.." in front of the path and nowhere else
                // (these have nothing to collapse on anyway)
                if (!doubleDotsAreCollapsed && (hasNonDotsOrSeparators || !startsWithDot) && isDot &&
                    previousChar == '.')
                    return MakeCompletelyWellFormatted(CollapseDoubleDots(path), true);

                if (isDot && (IsSlash(previousChar) || previousChar == '\0') && (IsSlash(nextChar) || nextChar == '\0'))
                    return MakeCompletelyWellFormatted(CollapseSingleDots(path));

                if (c == '\\' && (!IsUNCPath(path) || i >= 2))
                    return MakeCompletelyWellFormatted(ConvertToForwardSlashPath(path));

                if (c == '/' && previousChar == '/')
                    return MakeCompletelyWellFormatted(CollapseDoubleSlashes(path));

                if (c == '/')
                    numberOfForwardSlashes++;

                previousChar = c;
            }

            var lastChar = path[path.Length - 1];
            var secondToLastChar = path.Length >= 2 ? path[path.Length - 2] : '\0';

            // Remove trailing (non-root significant slash)
            if (lastChar == '/')
            {
                // this is a root path
                if (secondToLastChar == '\0' || secondToLastChar == ':')
                    return path;

                if (numberOfForwardSlashes == 1 && IsUNCPath(path))
                    return path;

                return path.Substring(0, path.Length - 1);
            }

            if (numberOfForwardSlashes == 0 && IsUNCPath(path))
                return path + "/";

            return path;
        }

        static string CollapseSingleDots(string path)
        {
            var result = ConvertToForwardSlashPath(path).Replace("/./", "/");
            if (result.StartsWith("./", StringComparison.Ordinal))
                result = result.Substring(2);
            if (result.EndsWith("/.", StringComparison.Ordinal))
                result = result.Substring(0, result.Length - 2);
            return result;
        }

        static string CollapseDoubleSlashes(string path)
        {
            return ConvertToForwardSlashPath(path).Replace("//", "/");
        }

        static string CollapseDoubleDots(string path)
        {
            path = ConvertToForwardSlashPath(path);
            var isRegularRoot = path[0] == '/';
            var isRootWithDriveLetter = (path[1] == ':' && path[2] == '/');
            var isUNCRoot = IsUNCPath(path);
            bool isRoot = isRegularRoot || isRootWithDriveLetter || isUNCRoot;

            var startIndex = 0;
            if (isRoot) startIndex = 1;
            if (isRootWithDriveLetter) startIndex = 3;
            if (isUNCRoot) startIndex = path.IndexOf('/') + 1;

            var stack = new Stack<string>();
            int segmentStart = startIndex;
            for (int i = startIndex; i != path.Length; i++)
            {
                if (path[i] == '/' || i == path.Length - 1)
                {
                    int extra = (i == path.Length - 1) ? 1 : 0;
                    var substring = path.Substring(segmentStart, i - segmentStart + extra);
                    if (substring == "..")
                    {
                        if (stack.Count == 0)
                        {
                            if (isRoot)
                                throw new ArgumentException(
                                    $"Cannot parse path because it's ..'ing beyond the root: {path}");
                            stack.Push(substring);
                        }
                        else
                        {
                            if (stack.Peek() == "..")
                                stack.Push(substring);
                            else
                                stack.Pop();
                        }
                    }
                    else
                        stack.Push(substring);

                    segmentStart = i + 1;
                }
            }

            return path.Substring(0, startIndex) + string.Join("/", stack.Reverse().ToArray());
        }

        const int MethodImplOptions_AggressiveInlining = 256; // enum value is only in .NET 4.5+

        [MethodImpl(MethodImplOptions_AggressiveInlining)]
        private static bool IsSlash(char c) => c == '/' || c == '\\';

        private static bool IsAbsolute(string path)
        {
            if (path == null) return false;

            if (path.Length > 0 && IsSlash(path[0])) return true;

            if (path.Length >= 3 && Char.IsLetter(path[0]) && path[1] == ':' && IsSlash(path[2])) return true;

            return false;
        }

        // Return a path string that is safe to append to another path string.
        private static string NormaliseRelativePath(string path)
        {
            // Beware of C:foo\bar.txt style relative windows paths.
            if (path.Length >= 3 && Char.IsLetter(path[0]) && path[1] == ':')
                return path.Substring(2);
            if (path.Length == 2 && Char.IsLetter(path[0]) && path[1] == ':')
                return ".";

            // Note: this function doesn't return fully normalised strings. For example, the
            // following are equivalent but this function will happily return any of them:
            // ""
            // "."
            // "././././."

            return path;
        }

        /// <summary>
        /// Create a new NPath by appending a path fragment.
        /// </summary>
        /// <param name="append">The path fragment to append. This can be a filename, or a whole relative path.</param>
        /// <returns>A new NPath which is the existing path with the fragment appended.</returns>
        public NPath Combine(string append)
        {
            var normalisedAppend = NormaliseRelativePath(append);

            if (normalisedAppend == "" || normalisedAppend == ".")
                return new NPath(_path, guaranteed_well_formed: true); // "" is treated as "." - nothing to append.

            if (IsAbsolute(append))
                throw new ArgumentException($"You cannot .Combine an absolute path: {append}");

            return new NPath(_path + "/" + normalisedAppend);
        }

        /// <summary>
        /// Create a new NPath by appending two path fragments, one after the other.
        /// </summary>
        /// <param name="append1">The first path fragment to append.</param>
        /// <param name="append2">The second path fragment to append.</param>
        /// <returns>A new NPath which is the existing path with the first fragment appended, then the second fragment appended.</returns>
        public NPath Combine(string append1, string append2)
        {
            if (IsAbsolute(append1))
                throw new ArgumentException($"You cannot .Combine an absolute path: {append1}");
            if (IsAbsolute(append2))
                throw new ArgumentException($"You cannot .Combine an absolute path: {append2}");

            var normalisedAppend1 = NormaliseRelativePath(append1);
            var normalisedAppend2 = NormaliseRelativePath(append2);

            if (normalisedAppend1 == "" || normalisedAppend1 == ".")
                return Combine(normalisedAppend2);

            if (normalisedAppend2 == "" || normalisedAppend2 == ".")
                return Combine(normalisedAppend1);

            return new NPath(_path + "/" + normalisedAppend1 + "/" + normalisedAppend2);
        }

        /// <summary>
        /// Create a new NPath by appending a path fragment.
        /// </summary>
        /// <param name="append">The path fragment to append.</param>
        /// <returns>A new NPath which is the existing path with the fragment appended.</returns>
        public NPath Combine(NPath append)
        {
            if (append == null)
                throw new ArgumentNullException(nameof(append));

            if (!append.IsRelative)
                throw new ArgumentException($"You cannot .Combine an absolute path: {append}");

            var normalisedAppend = NormaliseRelativePath(append._path);
            if (normalisedAppend == "" || normalisedAppend == ".")
                return new NPath(_path, guaranteed_well_formed: true);

            // If the to-append path starts by going up directories, we need to run
            // our normalizing constructor, if not, we can take the fast path.
            if (append._path[0] == '.' || _path[0] == '.' || _path.Length == 1)
                return new NPath(_path + "/" + normalisedAppend);

            return new NPath(_path + "/" + normalisedAppend, guaranteed_well_formed: true);
        }

        /// <summary>
        /// Create a new NPath by appending multiple path fragments.
        /// </summary>
        /// <param name="append">The path fragments to append, in order.</param>
        /// <returns>A new NPath which is this existing path with all the supplied path fragments appended, in order.</returns>
        public NPath Combine(params NPath[] append)
        {
            var sb = new StringBuilder(ToString());
            foreach (var a in append)
            {
                if (!a.IsRelative)
                    throw new ArgumentException($"You cannot .Combine an absolute path: {a}");

                var normalisedStr = NormaliseRelativePath(a.ToString());
                if (normalisedStr == "" || normalisedStr == ".")
                    continue;

                sb.Append("/");
                sb.Append(normalisedStr);
            }

            return new NPath(sb.ToString());
        }

        /// <summary>
        /// The parent path fragment (i.e. the directory) of the path.
        /// </summary>
        public NPath Parent
        {
            get
            {
                if (IsRoot)
                    throw new ArgumentException($"Parent invoked on {this}");

                for (int i = _path.Length - 1; i >= 0; i--)
                {
                    if (i == 0)
                        return _path[0] == '/' ? new NPath("/") : new NPath("");
                    if (_path[i] != '/') continue;
                    var isRooted = _path[i - 1] == ':' || _path[0] == '/';

                    var length = isRooted ? (i + 1) : i;

                    var substring = _path.Substring(0, length);

                    return new NPath(substring);
                }

                return Empty;
            }
        }

        /// <summary>
        /// Create a new NPath by computing the existing path relative to some other base path.
        /// </summary>
        /// <param name="path">The base path that the result should be relative to.</param>
        /// <returns>A new NPath, which refers to the same target as the existing path, but is described relative to the given base path.</returns>
        public NPath RelativeTo(NPath path)
        {
            if (IsRelative || path.IsRelative)
                return MakeAbsolute().RelativeTo(path.MakeAbsolute());

            var thisString = _path;
            var pathString = path._path;

            if (thisString == pathString)
                return ".";

            if (!HasSameDriveLetter(path) || !HasSameUNCServerName(path))
                return this;

            if (path.IsRoot)
                return new NPath(thisString.Substring(pathString.Length));

            if (thisString.StartsWith(pathString, PathStringComparison))
            {
                if (thisString.Length >= pathString.Length && (IsSlash(thisString[pathString.Length])))
                    return new NPath(thisString.Substring(Math.Min(pathString.Length + 1, thisString.Length)));
            }

            var sb = new StringBuilder();
            foreach (var parent in path.RecursiveParents.ToArray())
            {
                sb.Append("../");
                if (IsChildOf(parent))
                {
                    sb.Append(thisString.Substring(parent.ToString().Length));
                    return new NPath(sb.ToString());
                }
            }

            throw new ArgumentException();
        }

        /// <summary>
        /// Create an NPath by changing the extension of this one.
        /// </summary>
        /// <param name="extension">The new extension to use. Starting it with a "." character is optional. If you pass an empty string, the resulting path will have the extension stripped entirely, including the dot character.</param>
        /// <returns>A new NPath which is the existing path but with the new extension at the end.</returns>
        public NPath ChangeExtension(string extension)
        {
            ThrowIfRoot();

            var s = ToString();
            int lastDot = -1;
            for (int i = s.Length - 1; i >= 0; i--)
            {
                if (s[i] == '.')
                {
                    lastDot = i;
                    break;
                }

                if (s[i] == '/')
                    break;
            }

            var newExtension = extension.Length == 0 ? extension : WithDot(extension);
            if (lastDot == -1)
                return s + newExtension;
            return s.Substring(0, lastDot) + newExtension;
        }

        #endregion construction

        #region inspection

        /// <summary>
        /// Whether this path is relative (i.e. not absolute) or not.
        /// </summary>
        public bool IsRelative
        {
            get
            {
                if (IsSlash(_path[0]))
                    return false;

                // An unusual cases for windows to watch out for:
                // C:foo.txt is relative (`pwd`/foo.txt)
                // https://learn.microsoft.com/en-us/dotnet/standard/io/file-path-formats
                if (_path.Length >= 3 && _path[1] == ':' && IsSlash(_path[2]))
                    return false;

                return true;
            }
        }

        /// <summary>
        /// The name of the file or directory given at the end of this path, including any extension.
        /// </summary>
        public string FileName
        {
            get
            {
                ThrowIfRoot();

                if (_path.Length == 0)
                    return string.Empty;

                if (_path == ".")
                    return string.Empty;

                for (int i = _path.Length - 1; i >= 0; i--)
                {
                    if (_path[i] == '/')
                    {
                        return i == _path.Length - 1 ? string.Empty : _path.Substring(i + 1);
                    }
                }

                return _path;
            }
        }

        /// <summary>
        /// The name of the file or directory given at the end of this path, excluding the extension.
        /// </summary>
        public string FileNameWithoutExtension => Path.GetFileNameWithoutExtension(FileName);

        /// <summary>
        /// Determines whether the given path is, or is a child of, a directory with the given name.
        /// </summary>
        /// <param name="dir">The name of the directory to search for.</param>
        /// <returns>True if the path describes a file/directory that is or is a child of a directory with the given name; false otherwise.</returns>
        public bool HasDirectory(string dir)
        {
            if (dir.Contains("/") || dir.Contains("\\"))
                throw new ArgumentException($"Directory cannot contain slash {dir}");
            if (dir == ".")
                throw new ArgumentException("Single dot is not an allowed argument");

            if (_path.StartsWith(dir + "/", PathStringComparison))
                return true;
            if (_path.EndsWith("/" + dir, PathStringComparison))
                return true;
            return _path.Contains("/" + dir + "/");
        }

        /// <summary>
        /// The depth of the path, determined by the number of path separators present.
        /// </summary>
        public int Depth
        {
            get
            {
                if (IsRoot)
                    return 0;
                if (IsCurrentDir)
                    return 0;

                var depth = IsRelative ? 1 : 0;
                for (var i = 0; i != _path.Length; i++)
                {
                    if (_path[i] == '/')
                        depth++;
                }

                return depth;
            }
        }

        /// <summary>
        /// Tests whether the path is the current directory string ".".
        /// </summary>
        public bool IsCurrentDir => ToString() == ".";

        /// <summary>
        /// Tests whether the path exists.
        /// </summary>
        /// <param name="append">An optional path fragment to append before testing.</param>
        /// <returns>True if the path (with optional appended fragment) exists, false otherwise.</returns>
        public bool Exists(NPath append = null)
        {
            return FileExists(append) || DirectoryExists(append);
        }

        /// <summary>
        /// Tests whether the path exists and is a directory.
        /// </summary>
        /// <param name="append">An optional path fragment to append before testing.</param>
        /// <returns>True if the path (with optional appended fragment) exists and is a directory, false otherwise.</returns>
        public bool DirectoryExists(NPath append = null) => FileSystem.Active.Directory_Exists(append != null ? Combine(append) : this);

        /// <summary>
        /// Tests whether the path exists and is a file.
        /// </summary>
        /// <param name="append">An optional path fragment to append before testing.</param>
        /// <returns>True if the path (with optional appended fragment) exists and is a file, false otherwise.</returns>
        public bool FileExists(NPath append = null) => FileSystem.Active.File_Exists(append != null ? Combine(append) : this);

        /// <summary>
        /// The extension of the file, excluding the initial "." character.
        /// </summary>
        public string Extension
        {
            get
            {
                if (IsRoot)
                    throw new ArgumentException("A root directory does not have an extension");

                for (int i = _path.Length - 1; i >= 0; i--)
                {
                    var c = _path[i];
                    if (c == '.' || c == '/')
                        return _path.Substring(i + 1);
                }

                return string.Empty;
            }
        }

        /// <summary>
        /// UNC server name of the path, if present. Null if not present.
        /// </summary>
        public string UNCServerName
        {
            get
            {
                if (!IsUNC)
                    return null;
                var indexOfFirstSlash = _path.IndexOf('/');
                if (indexOfFirstSlash < 0)
                    indexOfFirstSlash = _path.Length;
                return _path.Substring(2, indexOfFirstSlash - 2);
            }
        }

        bool HasSameUNCServerName(NPath other) => UNCServerName == other.UNCServerName;

        /// <summary>
        /// The Windows drive letter of the path, if present. Null if not present.
        /// </summary>
        public string DriveLetter => _path.Length >= 2 && _path[1] == ':' ? _path[0].ToString() : null;

        bool HasSameDriveLetter(NPath other) => DriveLetter == other.DriveLetter;

        /// <summary>
        /// Provides a quoted version of the path as a string, with the requested path separator type.
        /// </summary>
        /// <param name="slashMode">The path separator to use. See the <see cref="SlashMode">SlashMode</see> enum for an explanation of the values. Defaults to <c>SlashMode.Forward</c>.</param>
        /// <returns>The path, with the requested path separator type, in quotes.</returns>
        public string InQuotes(SlashMode slashMode = SlashMode.Forward)
        {
            return "\"" + ToString(slashMode) + "\"";
        }

        /// <summary>
        /// Convert this path to a string, using forward slashes as path separators.
        /// </summary>
        /// <returns>The string representation of this path.</returns>
        public override string ToString()
        {
            return _path;
        }

        /// <summary>
        /// Convert this path to a string, using the requested path separator type.
        /// </summary>
        /// <param name="slashMode">The path separator type to use. See <see cref="SlashMode">SlashMode</see> for possible values.</param>
        /// <returns>The string representation of this path.</returns>
        public string ToString(SlashMode slashMode)
        {
            if (slashMode == SlashMode.Forward || (slashMode == SlashMode.Native && !k_IsWindows))
                return _path;

            return _path.Replace("/", "\\");
        }

        /// <summary>
        /// Checks if this NPath represents the same path as another object.
        /// </summary>
        /// <param name="obj">The object to compare to.</param>
        /// <returns>True if this NPath represents the same path as the other object; false if it does not, if the other object is not an NPath, or is null.</returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as NPath);
        }

        /// <summary>
        /// Checks if this NPath is equal to another NPath.
        /// </summary>
        /// <param name="p">The path to compare to.</param>
        /// <returns>True if this NPath represents the same path as the other NPath; false otherwise.</returns>
        /// <remarks>Note that the comparison requires that the paths are the same, not just that the targets are the same; "foo/bar" and "foo/baz/../bar" refer to the same target but will not be treated as equal by this comparison. However, this comparison will ignore case differences when the current operating system does not use case-sensitive filesystems.</remarks>
        public bool Equals(NPath p)
        {
            return p != null && string.Equals(p._path, _path, PathStringComparison);
        }

        /// <summary>
        /// Compare two NPaths for equality.
        /// </summary>
        /// <param name="a">The first NPath to compare.</param>
        /// <param name="b">The second NPath to compare.</param>
        /// <returns>True if the NPaths are both equal (or both null), false otherwise. See <see cref="Equals(NPath)">Equals.</see></returns>
        public static bool operator==(NPath a, NPath b)
        {
            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(a, b))
                return true;

            // If one is null, but not both, return false.
            if ((object)a == null || (object)b == null)
                return false;

            // Return true if the fields match:
            return a.Equals(b);
        }

        /// <summary>
        /// Get an appropriate hash value for this NPath.
        /// </summary>
        /// <returns>A hash value for this NPath.</returns>
        public override int GetHashCode()
        {
            if (k_IsCaseSensitiveFileSystem)
                return _path.GetHashCode();

            uint hash = 27644437;
            for (int i = 0, len = _path.Length; i < len; ++i)
            {
                uint c = _path[i];
                if (c > 0x80) c = 0x80; // All non-ASCII chars may (potentially) compare Equal.
                c |= 0x20; // ASCII case folding.
                hash ^= (hash << 5) ^ c;
            }

            return unchecked((int)hash);
        }

        /// <summary>
        /// Compare this NPath to another NPath, returning a value that can be used to sort the two objects in a stable order.
        /// </summary>
        /// <param name="obj">The object to compare to. Note that this object must be castable to NPath.</param>
        /// <returns>A value that indicates the relative order of the two objects. The return value has these meanings:
        /// <list type="table">
        /// <listheader><term>Value</term><description>Meaning</description></listheader>
        /// <item><term>Less than zero</term><description>This instance precedes <c>obj</c> in the sort order.</description></item>
        /// <item><term>Zero</term><description>This instance occurs in the same position as <c>obj</c> in the sort order.</description></item>
        /// <item><term>Greater than zero</term><description>This instance follows <c>obj</c> in the sort order.</description></item>
        /// </list>
        /// </returns>

        public int CompareTo(object obj)
        {
            if (obj == null)
                return -1;

            return string.Compare(_path, ((NPath)obj)._path, PathStringComparison);
        }

        /// <summary>
        /// Compare two NPaths for inequality.
        /// </summary>
        /// <param name="a">The first NPath to compare.</param>
        /// <param name="b">The second NPath to compare.</param>
        /// <returns>True if the NPaths are not equal, false otherwise.</returns>
        public static bool operator!=(NPath a, NPath b)
        {
            return !(a == b);
        }

        /// <summary>
        /// Tests whether this NPath has the provided extension.
        /// </summary>
        /// <param name="extension">The extension to test for.</param>
        /// <returns>True if this NPath has has the provided extension. False otherwise.</returns>
        /// <remarks>The extension "*" is special, and will return true for all paths if specified.</remarks>
        public bool HasExtension(string extension)
        {
            if (extension == "*")
                return true;

            if (IsRoot)
                return false;

            if (extension.Length > _path.Length)
                return false;

            int extensionOffset = _path.Length - extension.Length;
            if (extension.Length == 0 || extension[0] != '.')
            {
                // Supplied extension doesn't have a dot, so we must check
                // that there is a dot or a slash before the extension in our path
                if (extensionOffset < 1)
                    return false;

                char extensionSeparator = _path[extensionOffset - 1];
                if (extensionSeparator != '.' && extensionSeparator != '/')
                    return false;
            }

            return string.Compare(extension, 0, _path, extensionOffset, extension.Length, StringComparison.OrdinalIgnoreCase) == 0;
        }

        /// <summary>
        /// Tests whether this NPath has one of the provided extensions, or if no extensions are provided, whether it has any extension at all.
        /// </summary>
        /// <param name="extensions">The possible extensions to test for.</param>
        /// <returns>True if this NPath has one of the provided extensions; or, if no extensions are provided, true if this NPath has an extension. False otherwise.</returns>
        /// <remarks>The extension "*" is special, and will return true for all paths if specified.</remarks>
        public bool HasExtension(params string[] extensions)
        {
            if (extensions.Length == 0)
                return FileName.Contains(".");

            foreach (var e in extensions)
            {
                if (HasExtension(e))
                    return true;
            }

            return false;
        }

        private static string WithDot(string extension)
        {
            return extension.StartsWith(".", StringComparison.Ordinal) ? extension : "." + extension;
        }

        /// <summary>
        /// Whether this path is rooted or not (begins with a slash character or drive specifier).
        /// </summary>
        public bool IsRoot
        {
            get
            {
                if (_path == "/")
                    return true;

                if (_path.Length == 3 && _path[1] == ':' && _path[2] == '/')
                    return true;

                if (IsUNC && _path.Length == _path.IndexOf('/') + 1)
                    return true;

                return false;
            }
        }

        /// <summary>
        /// Whether this path starts with an UNC path prefix "\\" or not.
        /// </summary>
        public bool IsUNC => IsUNCPath(_path);

        #endregion inspection

        #region directory enumeration

        /// <summary>
        /// Find all files within this path that match the given filter.
        /// </summary>
        /// <param name="filter">The filter to match against the names of files. Wildcards can be included.</param>
        /// <param name="recurse">If true, search recursively inside subdirectories of this path; if false, search only for files that are immediate children of this path. Defaults to false.</param>
        /// <returns>An array of files that were found.</returns>
        public NPath[] Files(string filter, bool recurse = false) => FileSystem.Active.Directory_GetFiles(_path, filter, recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

        /// <summary>
        /// Find all files within this path.
        /// </summary>
        /// <param name="recurse">If true, search recursively inside subdirectories of this path; if false, search only for files that are immediate children of this path. Defaults to false.</param>
        /// <returns>An array of files that were found.</returns>
        public NPath[] Files(bool recurse = false) => Files("*", recurse);

        /// <summary>
        /// Find all files within this path that have one of the provided extensions.
        /// </summary>
        /// <param name="extensions">The extensions to search for.</param>
        /// <param name="recurse">If true, search recursively inside subdirectories of this path; if false, search only for files that are immediate children of this path. Defaults to false.</param>
        /// <returns>An array of files that were found.</returns>
        public NPath[] Files(string[] extensions, bool recurse = false)
        {
            if (!DirectoryExists() || extensions.Length == 0)
                return new NPath[] {};

            return FileSystem.Active.Directory_GetFiles(this, "*", recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).Where(p => extensions.Contains(p.Extension)).ToArray();
        }

        /// <summary>
        /// Find all files or directories within this path that match the given filter.
        /// </summary>
        /// <param name="filter">The filter to match against the names of files and directories. Wildcards can be included.</param>
        /// <param name="recurse">If true, search recursively inside subdirectories of this path; if false, search only for files and directories that are immediate children of this path. Defaults to false.</param>
        /// <returns>An array of files and directories that were found.</returns>
        public NPath[] Contents(string filter, bool recurse = false)
        {
            return Files(filter, recurse).Concat(Directories(filter, recurse)).ToArray();
        }

        /// <summary>
        /// Find all files and directories within this path.
        /// </summary>
        /// <param name="recurse">If true, search recursively inside subdirectories of this path; if false, search only for files and directories that are immediate children of this path. Defaults to false.</param>
        /// <returns>An array of files and directories that were found.</returns>
        public NPath[] Contents(bool recurse = false)
        {
            return Contents("*", recurse);
        }

        /// <summary>
        /// Find all directories within this path that match the given filter.
        /// </summary>
        /// <param name="filter">The filter to match against the names of directories. Wildcards can be included.</param>
        /// <param name="recurse">If true, search recursively inside subdirectories of this path; if false, search only for directories that are immediate children of this path. Defaults to false.</param>
        /// <returns>An array of directories that were found.</returns>
        public NPath[] Directories(string filter, bool recurse = false)
        {
            return FileSystem.Active.Directory_GetDirectories(this, filter, recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
        }

        /// <summary>
        /// Find all directories within this path.
        /// </summary>
        /// <param name="recurse">If true, search recursively inside subdirectories of this path; if false, search only for directories that are immediate children of this path. Defaults to false.</param>
        /// <returns>An array of directories that were found.</returns>
        public NPath[] Directories(bool recurse = false)
        {
            return Directories("*", recurse);
        }

        #endregion

        #region filesystem writing operations

        /// <summary>
        /// Create an empty file at this path.
        /// </summary>
        /// <returns>This NPath, for chaining further operations.</returns>
        /// <remarks>If a file already exists at this path, it will be overwritten.</remarks>
        public NPath CreateFile()
        {
            ThrowIfRoot();
            EnsureParentDirectoryExists();
            FileSystem.Active.File_WriteAllBytes(this, new byte[0]);
            return this;
        }

        /// <summary>
        /// Append the given path fragment to this path, and create an empty file there.
        /// </summary>
        /// <param name="file">The path fragment to append.</param>
        /// <returns>The path to the created file, for chaining further operations.</returns>
        /// <remarks>If a file already exists at that path, it will be overwritten.</remarks>
        public NPath CreateFile(NPath file)
        {
            if (!file.IsRelative)
                throw new ArgumentException(
                    "You cannot call CreateFile() on an existing path with a non relative argument");
            return Combine(file).CreateFile();
        }

        /// <summary>
        /// Create this path as a directory if it does not already exist.
        /// </summary>
        /// <returns>This NPath, for chaining further operations.</returns>
        /// <remark>This is identical to <see cref="EnsureDirectoryExists(NPath)"/>, except that EnsureDirectoryExists triggers "Stat" callbacks and this doesn't.</remark>
        public NPath CreateDirectory()
        {
            if (IsRoot)
                throw new NotSupportedException(
                    "CreateDirectory is not supported on a root level directory because it would be dangerous:" +
                    ToString());

            FileSystem.Active.Directory_CreateDirectory(this);
            return this;
        }

        /// <summary>
        /// Append the given path fragment to this path, and create it as a directory if it does not already exist.
        /// </summary>
        /// <param name="directory">The path fragment to append.</param>
        /// <returns>The path to the created directory, for chaining further operations.</returns>
        public NPath CreateDirectory(NPath directory)
        {
            if (!directory.IsRelative)
                throw new ArgumentException("Cannot call CreateDirectory with an absolute argument");

            return Combine(directory).CreateDirectory();
        }

        /// <summary>
        /// Create this path as a symbolic link to another file or directory.
        /// </summary>
        /// <param name="targetPath">The path that this path should be a symbolic link to. Can be relative or absolute.</param>
        /// <param name="targetIsFile">Specifies whether this link is to a file or to a directory (required on Windows). Defaults to file.</param>
        /// <returns>The path to the created symbolic link, for chaining further operations.</returns>
        public NPath CreateSymbolicLink(NPath targetPath, bool targetIsFile = true)
        {
            ThrowIfRoot();

            if (Exists())
                throw new InvalidOperationException(
                    "Cannot create symbolic link at {this} because it already exists as a file or directory.");

            FileSystem.Active.CreateSymbolicLink(this, targetPath, targetIsFile);
            return this;
        }

        /// <summary>
        /// Checks whether the entity referred to by this path is a symbolic link.
        /// </summary>
        public bool IsSymbolicLink
        {
            get
            {
                return FileSystem.Active.IsSymbolicLink(this);
            }
        }

        /// <summary>
        /// Copy this NPath to the given destination.
        /// </summary>
        /// <param name="dest">The path to copy to.</param>
        /// <returns>The path to the copied result, for chaining further operations.</returns>
        public NPath Copy(NPath dest)
        {
            return Copy(dest, p => true);
        }

        /// <summary>
        /// Copy this NPath to the given destination, applying a filter function to decide which files are copied.
        /// </summary>
        /// <param name="dest">The path to copy to.</param>
        /// <param name="fileFilter">The filter function. Each candidate file is passed to this function; if the function returns true, the file will be copied, otherwise it will not.</param>
        /// <returns></returns>
        public NPath Copy(NPath dest, Func<NPath, bool> fileFilter)
        {
            if (dest.DirectoryExists())
                return CopyWithDeterminedDestination(dest.Combine(FileName), fileFilter);

            return CopyWithDeterminedDestination(dest, fileFilter);
        }

        /// <summary>
        /// Create a new NPath by converting this path into an absolute representation.
        /// </summary>
        /// <param name="base">Optional base to use as a root for relative paths.</param>
        /// <returns></returns>
        public NPath MakeAbsolute(NPath @base = null)
        {
            if (!IsRelative)
                return this;

            return (@base ?? CurrentDirectory).Combine(this);
        }

        NPath CopyWithDeterminedDestination(NPath destination, Func<NPath, bool> fileFilter)
        {
            destination = destination.MakeAbsolute();

            if (FileExists())
            {
                if (!fileFilter(destination))
                    return null;

                destination.EnsureParentDirectoryExists();

                FileSystem.Active.File_Copy(this, destination, true);
                return destination;
            }

            if (DirectoryExists())
            {
                destination.EnsureDirectoryExists();
                foreach (var thing in Contents())
                    thing.CopyWithDeterminedDestination(destination.Combine(thing.RelativeTo(this)),
                        fileFilter);
                return destination;
            }

            throw new ArgumentException("Copy() called on path that doesnt exist: " + ToString());
        }

        /// <summary>
        /// Deletes the file or directory referred to by the NPath.
        /// </summary>
        /// <param name="deleteMode">The deletion mode to use, see <see cref="DeleteMode">DeleteMode.</see> Defaults to DeleteMode.Normal.</param>
        /// <exception cref="System.InvalidOperationException">The path does not exist. See also <see cref="DeleteIfExists">DeleteIfExists</see>.</exception>
        public void Delete(DeleteMode deleteMode = DeleteMode.Normal)
        {
            if (IsRoot)
                throw new NotSupportedException(
                    "Delete is not supported on a root level directory because it would be dangerous:" + ToString());

            try
            {
                if (FileExists())
                    FileSystem.Active.File_Delete(this);
                else if (DirectoryExists())
                    FileSystem.Active.Directory_Delete(this, true);
                else
                    throw new InvalidOperationException("Trying to delete a path that does not exist: " + ToString());
            }
            catch (IOException)
            {
                if (deleteMode == DeleteMode.Normal)
                    throw;
            }
            catch (UnauthorizedAccessException)
            {
                if (deleteMode == DeleteMode.Normal)
                    throw;
            }
        }

        /// <summary>
        /// Deletes the file or directory referred to by the NPath, if it exists.
        /// </summary>
        /// <param name="deleteMode">The deletion mode to use, see <see cref="DeleteMode">DeleteMode.</see> Defaults to DeleteMode.Normal.</param>
        /// <returns>This NPath, for chaining further operations.</returns>
        public NPath DeleteIfExists(DeleteMode deleteMode = DeleteMode.Normal)
        {
            if (FileExists() || DirectoryExists())
                Delete(deleteMode);

            return this;
        }

        /// <summary>
        /// Deletes all files and directories inside the directory referred to by this NPath.
        /// </summary>
        /// <returns>This NPath, for chaining further operations.</returns>
        /// <exception cref="System.InvalidOperationException">This NPath refers to a file, rather than a directory.</exception>
        public NPath DeleteContents()
        {
            if (IsRoot)
                throw new NotSupportedException(
                    "DeleteContents is not supported on a root level directory because it would be dangerous:" +
                    ToString());

            if (FileExists())
                throw new InvalidOperationException("It is not valid to perform this operation on a file");

            if (DirectoryExists())
            {
                try
                {
                    Files().Delete();
                    Directories().Delete();
                }
                catch (IOException)
                {
                    if (Files(true).Any())
                        throw;
                }

                return this;
            }

            return EnsureDirectoryExists();
        }

        /// <summary>
        /// Create a temporary directory in the system temporary location and return the NPath of it.
        /// </summary>
        /// <param name="prefix">A prefix to use for the name of the temporary directory.</param>
        /// <returns>A new NPath which targets the newly created temporary directory.</returns>
        public static NPath CreateTempDirectory(string prefix = "")
        {
            var sb = new StringBuilder();

            sb.Append(Path.GetTempPath());
            sb.Append("/");
            if (!string.IsNullOrEmpty(prefix))
            {
                sb.Append(prefix);
                sb.Append("_");
            }
            sb.Append(Path.GetRandomFileName());

            var path = new NPath(sb.ToString());
            return path.CreateDirectory();
        }

        /// <summary>
        /// Move the file or directory targetted by this NPath to a new location.
        /// </summary>
        /// <param name="dest">The destination for the move.</param>
        /// <returns>An NPath representing the newly moved file or directory.</returns>
        public NPath Move(NPath dest)
        {
            if (IsRoot)
                throw new NotSupportedException(
                    "Move is not supported on a root level directory because it would be dangerous:" + ToString());

            if (dest.DirectoryExists())
                return Move(dest.Combine(FileName));

            if (FileExists())
            {
                dest.EnsureParentDirectoryExists();
                FileSystem.Active.File_Move(this, dest);
                return dest;
            }

            if (DirectoryExists())
            {
                FileSystem.Active.Directory_Move(this, dest);
                return dest;
            }

            throw new ArgumentException("Move() called on a path that doesn't exist: " + ToString());
        }

        #endregion

        #region special paths

        /// <summary>
        /// The current directory in use by the process.
        /// </summary>
        /// <remarks>Note that every read from this property will result in an operating system query, unless <see cref="WithFrozenCurrentDirectory">WithFrozenCurrentDirectory</see> is used.</remarks>
        public static NPath CurrentDirectory => FileSystem.Active.Directory_GetCurrentDirectory();

        class SetCurrentDirectoryOnDispose : IDisposable
        {
            public NPath Directory { get; }

            public SetCurrentDirectoryOnDispose(NPath directory)
            {
                Directory = directory;
            }

            public void Dispose()
            {
                SetCurrentDirectory(Directory);
            }
        }

        /// <summary>
        /// Temporarily change the current directory for the process.
        /// </summary>
        /// <param name="directory">The new directory to set as the current directory.</param>
        /// <returns>A token representing the change in current directory. When this is disposed, the current directory will be returned to its previous value. The usual usage pattern is to capture the token with a <c>using</c> statement, such that it is automatically disposed of when the <c>using</c> block exits.</returns>
        public static IDisposable SetCurrentDirectory(NPath directory)
        {
            var result = new SetCurrentDirectoryOnDispose(CurrentDirectory);
            FileSystem.Active.Directory_SetCurrentDirectory(directory);
            return result;
        }

        /// <summary>
        /// The current user's home directory.
        /// </summary>
        public static NPath HomeDirectory
        {
            get
            {
                if (Path.DirectorySeparatorChar == '\\')
                    return new NPath(Environment.GetEnvironmentVariable("USERPROFILE"));
                return new NPath(Environment.GetEnvironmentVariable("HOME"));
            }
        }

        /// <summary>
        /// The system temporary directory.
        /// </summary>
        public static NPath SystemTemp => new NPath(Path.GetTempPath());

        #endregion

        private void ThrowIfRoot()
        {
            if (IsRoot)
                throw new ArgumentException(
                    "You are attempting an operation that is not valid on a root level directory");
        }

        /// <summary>
        /// Append an optional path fragment to this NPath, then create it as a directory if it does not already exist.
        /// </summary>
        /// <param name="append">The path fragment to append.</param>
        /// <returns>The path to the directory that is now guaranteed to exist.</returns>
        /// <remark>This is identical to <see cref="CreateDirectory()"/>, except that this triggers "Stat" callbacks and CreateDirectory doesn't.</remark>
        public NPath EnsureDirectoryExists(NPath append = null)
        {
            var combined = append != null ? Combine(append) : this;
            if (combined.DirectoryExists())
                return combined;
            combined.EnsureParentDirectoryExists();
            combined.CreateDirectory();
            return combined;
        }

        /// <summary>
        /// Create the parent directory of this NPath if it does not already exist.
        /// </summary>
        /// <returns>This NPath, for chaining further operations.</returns>
        public NPath EnsureParentDirectoryExists()
        {
            Parent.EnsureDirectoryExists();
            return this;
        }

        /// <summary>
        /// Throw an exception if this path does not exist as a file.
        /// </summary>
        /// <returns>This path, in order to chain further operations.</returns>
        /// <exception cref="System.IO.FileNotFoundException">The path does not exist, or is not a file.</exception>
        public NPath FileMustExist()
        {
            if (!FileExists())
                throw new FileNotFoundException("File was expected to exist : " + ToString());

            return this;
        }

        /// <summary>
        /// Throw an exception if this directory does not exist.
        /// </summary>
        /// <returns>This path, in order to chain further operations.</returns>
        /// <exception cref="System.IO.FileNotFoundException">The path does not exist, or is not a directory.</exception>
        public NPath DirectoryMustExist()
        {
            if (!DirectoryExists())
                throw new DirectoryNotFoundException("Expected directory to exist : " + ToString());

            return this;
        }

        /// <summary>
        /// Check if this path is a child of the given path hierarchy root (i.e. is a file or directory that is inside the given hierachy root directory or one of its descendent directories).
        /// </summary>
        /// <param name="potentialBasePath">The path hierarchy root to check.</param>
        /// <returns>True if this path is a child of the given root path, false otherwise.</returns>
        public bool IsChildOf(NPath potentialBasePath)
        {
            if (IsRelative != potentialBasePath.IsRelative)
                return MakeAbsolute().IsChildOf(potentialBasePath.MakeAbsolute());

            if (!IsRelative && !HasSameDriveLetter(potentialBasePath) || !HasSameUNCServerName(potentialBasePath))
                return false;

            if (potentialBasePath.IsRoot)
                return true;

            if (IsRelative && potentialBasePath._path == ".")
                return !_path.StartsWith("..", StringComparison.Ordinal);

            var potentialBaseString = potentialBasePath._path;
            var potentialBaseStringLength = potentialBaseString.Length;

            return _path.Length > potentialBaseStringLength + 1 &&
                _path.StartsWith(potentialBaseString, PathStringComparison) &&
                _path[potentialBaseStringLength] == '/';
        }

        /// <summary>
        /// Check if this path is a child of the given path hierarchy root (i.e. is a file or directory that is inside the given hierachy root directory or one of its descendent directories), or is equal to it.
        /// </summary>
        /// <param name="potentialBasePath">The path hierarchy root to check.</param>
        /// <returns>True if this path is equal to or is a child of the given root path, false otherwise.</returns>
        public bool IsSameAsOrChildOf(NPath potentialBasePath)
        {
            return MakeAbsolute() == potentialBasePath.MakeAbsolute() || IsChildOf(potentialBasePath);
        }

        /// <summary>
        /// Return each parent directory of this path, starting with the immediate parent, then that directory's parent, and so on, until the root of the path is reached.
        /// </summary>
        public IEnumerable<NPath> RecursiveParents
        {
            get
            {
                var candidate = this;
                while (true)
                {
                    if (candidate.IsRoot || candidate._path == ".")
                        yield break;

                    candidate = candidate.Parent;
                    yield return candidate;
                }
            }
        }

        /// <summary>
        /// Search all parent directories of this path for one that contains a file or directory with the given name.
        /// </summary>
        /// <param name="needle">The name of the file or directory to search for.</param>
        /// <returns>The path to the parent directory that contains the file or directory, or null if none of the parents contained a file or directory with the requested name.</returns>
        public NPath ParentContaining(NPath needle)
        {
            return RecursiveParents.FirstOrDefault(p => p.Exists(needle));
        }

        /// <summary>
        /// Open this path as a text file, write the given string to it, then close the file.
        /// </summary>
        /// <param name="contents">The string to write to the text file.</param>
        /// <returns>The path to this file, for use in chaining further operations.</returns>
        public NPath WriteAllText(string contents)
        {
            EnsureParentDirectoryExists();
            FileSystem.Active.File_WriteAllText(this, contents);
            return this;
        }

        /// <summary>
        /// Open this file as a text file, and replace the contents with the provided string, if they do not already match. Then close the file.
        /// </summary>
        /// <param name="contents">The string to replace the file's contents with.</param>
        /// <returns>The path to this file, for use in chaining further operations.</returns>
        /// <remarks>Note that if the contents of the file already match the provided string, the file is not modified - this includes not modifying the file's "last written" timestamp.</remarks>
        public NPath ReplaceAllText(string contents)
        {
            if (FileExists() && ReadAllText() == contents)
                return this;
            WriteAllText(contents);
            return this;
        }

        /// <summary>
        /// Open this path as a file, write the given bytes to it, then close the file.
        /// </summary>
        /// <param name="bytes">The bytes to write to the file.</param>
        /// <returns>The path to this file, for use in chaining further operations.</returns>
        public NPath WriteAllBytes(byte[] bytes)
        {
            EnsureParentDirectoryExists();
            FileSystem.Active.File_WriteAllBytes(this, bytes);
            return this;
        }

        /// <summary>
        /// Opens a text file, reads all the text in the file into a single string, then closes the file.
        /// </summary>
        /// <returns>The contents of the text file, as a single string.</returns>
        public string ReadAllText() => FileSystem.Active.File_ReadAllText(this);

        /// <summary>
        /// Opens a file, reads all the bytes in the file, then closes the file.
        /// </summary>
        /// <returns>The contents of the file, as a bytes array.</returns>
        public byte[] ReadAllBytes() => FileSystem.Active.File_ReadAllBytes(this);

        /// <summary>
        /// Opens a text file, writes all entries of a string array as separate lines into the file, then closes the file.
        /// </summary>
        /// <param name="contents">The entries to write into the file as separate lines.</param>
        /// <returns>The path to this file.</returns>
        public NPath WriteAllLines(string[] contents)
        {
            EnsureParentDirectoryExists();
            FileSystem.Active.File_WriteAllLines(this, contents);
            return this;
        }

        /// <summary>
        /// Opens a text file, reads all lines of the file into a string array, and then closes the file.
        /// </summary>
        /// <returns>A string array containing all lines of the file.</returns>
        public string[] ReadAllLines() => FileSystem.Active.File_ReadAllLines(this);

        /// <summary>
        /// Copy all files in this NPath to the given destination directory.
        /// </summary>
        /// <param name="destination">The directory to copy the files to.</param>
        /// <param name="recurse">If true, files inside subdirectories of this NPath will also be copied. If false, only immediate child files of this NPath will be copied.</param>
        /// <param name="fileFilter">An optional predicate function that can be used to filter files. It is passed each source file path in turn, and if it returns true, the file is copied; otherwise, the file is not copied.</param>
        /// <returns>The paths to all the newly copied files.</returns>
        /// <remarks>Note that the directory structure of the files relative to this NPath will be preserved within the target directory.</remarks>
        public IEnumerable<NPath> CopyFiles(NPath destination, bool recurse, Func<NPath, bool> fileFilter = null)
        {
            destination.EnsureDirectoryExists();
            return Files(recurse).Where(fileFilter ?? AlwaysTrue)
                .Select(file => file.Copy(destination.Combine(file.RelativeTo(this)))).ToArray();
        }

        /// <summary>
        /// Move all files in this NPath to the given destination directory.
        /// </summary>
        /// <param name="destination">The directory to move the files to.</param>
        /// <param name="recurse">If true, files inside subdirectories of this NPath will also be moved. If false, only immediate child files of this NPath will be moved.</param>
        /// <param name="fileFilter">An optional predicate function that can be used to filter files. It is passed each source file path in turn, and if it returns true, the file is moved; otherwise, the file is not moved.</param>
        /// <returns>The paths to all the newly moved files.</returns>
        /// <remarks>Note that the directory structure of the files relative to this NPath will be preserved within the target directory.</remarks>
        public IEnumerable<NPath> MoveFiles(NPath destination, bool recurse, Func<NPath, bool> fileFilter = null)
        {
            if (IsRoot)
                throw new NotSupportedException(
                    "MoveFiles is not supported on this directory because it would be dangerous:" + ToString());

            destination.EnsureDirectoryExists();
            return Files(recurse).Where(fileFilter ?? AlwaysTrue)
                .Select(file => file.Move(destination.Combine(file.RelativeTo(this)))).ToArray();
        }

        static bool AlwaysTrue(NPath p)
        {
            return true;
        }

        /// <summary>
        /// Implicitly construct a new NPath from a string.
        /// </summary>
        /// <param name="input">The string to construct the new NPath from.</param>
        public static implicit operator NPath(string input)
        {
            return input != null ? new NPath(input) : null;
        }

        /// <summary>
        /// Set the last time the file was written to, in UTC.
        /// </summary>
        /// <returns>The last time the file was written to, in UTC.</returns>
        /// <remarks>This is set automatically by the OS when the file is modified, but it can sometimes be useful
        /// to explicitly update the timestamp without modifying the file contents.</remarks>
        public NPath SetLastWriteTimeUtc(DateTime lastWriteTimeUtc)
        {
            FileSystem.Active.File_SetLastWriteTimeUtc(this, lastWriteTimeUtc);
            return this;
        }

        /// <summary>
        /// Get the last time the file was written to, in UTC.
        /// </summary>
        /// <returns>The last time the file was written to, in UTC.</returns>
        public DateTime GetLastWriteTimeUtc()
        {
            return FileSystem.Active.File_GetLastWriteTimeUtc(this);
        }

        /// <summary>
        /// Get the file length in bytes.
        /// </summary>
        /// <returns>The file length in bytes.</returns>
        public long GetFileSize()
        {
            return FileSystem.Active.File_GetSize(this);
        }

        /// <summary>
        /// The filesystem attributes of the given file, assuming it exists. Note that when you set this property, the
        /// OS may still modify the the actual attributes of the file beyond what you requested, so setting and then
        /// getting the property is not guaranteed to roundtrip the value. Note also that some attributes (e.g.
        /// FileAttributes.Hidden) are not supported on every OS, and may throw exceptions or simply be ignored.
        /// </summary>
        public FileAttributes Attributes
        {
            get { return FileSystem.Active.File_GetAttributes(this); }
            set
            {
                FileSystem.Active.File_SetAttributes(this, value);
            }
        }

        /// <summary>
        /// Until .Dispose is invoked on the returnvalue, makes all NPath's on this thread use the provided filesystem implementation for all filesystem access.
        /// </summary>
        /// <param name="fileSystem"></param>
        /// <returns>An object you can invoke .Dispose() on, which will make all NPath filesystem operations stop using the provided filesystem</returns>
        public static IDisposable WithFileSystem(FileSystem fileSystem) => new WithFileSystemHelper(fileSystem);


        /// <summary>
        /// Lets the currently active filesystem resolve the path.
        /// </summary>
        /// <returns></returns>
        public NPath ResolveWithFileSystem() => FileSystem.Active.Resolve(this);

        /// <summary>
        /// Shorthand for .ResolveWithFileSystem.InQuotes()
        /// </summary>
        /// <param name="slashMode"></param>
        /// <returns></returns>
        public string InQuotesResolved(SlashMode slashMode = SlashMode.Forward) => ResolveWithFileSystem().InQuotes(slashMode);

        /// <summary>
        /// Abstract baseclass you can use to plug in different underlying filesystem behaviour for NPath to operate on
        /// </summary>
        public abstract class FileSystem : IDisposable
        {
            [ThreadStatic] internal static FileSystem _active;

            /// <summary>
            /// The currently active filesystem for NPath operations. Set this using NPath.WithFileSystem()
            /// </summary>
            public static FileSystem Active => _active = _active ?? MakeFileSystemForCurrentMachine();

            private static FileSystem MakeFileSystemForCurrentMachine()
            {
                if (CalculateIsWindows())
                    return new WindowsFileSystem();
                else
                    return new PosixFileSystem();
            }

            /// <inheritdoc />
            public virtual void Dispose()
            {
            }

#pragma warning disable 1591
            public abstract NPath[] Directory_GetFiles(NPath path, string filter, SearchOption searchOptions);
            public abstract bool Directory_Exists(NPath path);
            public abstract bool File_Exists(NPath path);
            public abstract void File_WriteAllBytes(NPath path, byte[] bytes);
            public abstract void File_Copy(NPath path, NPath destinationPath, bool overWrite);
            public abstract void File_Delete(NPath path);

            public abstract void File_Move(NPath path, NPath destinationPath);
            public abstract void File_WriteAllText(NPath path, string contents);
            public abstract string File_ReadAllText(NPath path);
            public abstract void File_WriteAllLines(NPath path, string[] contents);
            public abstract byte[] File_ReadAllBytes(NPath path);

            public abstract string[] File_ReadAllLines(NPath path);

            public abstract void File_SetLastWriteTimeUtc(NPath path, DateTime lastWriteTimeUtc);

            public abstract DateTime File_GetLastWriteTimeUtc(NPath path);

            public abstract long File_GetSize(NPath path);

            public abstract void File_SetAttributes(NPath path, FileAttributes value);
            public abstract FileAttributes File_GetAttributes(NPath path);

            public abstract void Directory_CreateDirectory(NPath path);
            public abstract void Directory_Delete(NPath path, bool b);
            public abstract void Directory_Move(NPath path, NPath destPath);

            public abstract NPath Directory_GetCurrentDirectory();
            public abstract void Directory_SetCurrentDirectory(NPath directoryPath);
            public abstract NPath[] Directory_GetDirectories(NPath path, string filter, SearchOption searchOptions);

            /// <summary>
            /// If your filesystem does any kind of redirection or other magic, Resolve() is required to return the path that can be used against the raw lowlevel filesystem of the OS.
            /// </summary>
            /// <param name="path">The path to resolve</param>
            /// <returns>The resolved path that is valid to use against the OS's real filesystem</returns>
            public abstract NPath Resolve(NPath path);

            public abstract bool IsSymbolicLink(NPath path);

            public abstract void CreateSymbolicLink(NPath fromPath, NPath targetPath, bool targetIsFile);
        }
#pragma warning restore 1591

        abstract class SystemIOFileSystem : FileSystem
        {
            public override NPath[] Directory_GetFiles(NPath path, string filter, SearchOption searchOptions) => System.IO.Directory.GetFiles(path.ToString(SlashMode.Native), filter, searchOptions).ToNPaths().ToArray();
            public override bool Directory_Exists(NPath path) => System.IO.Directory.Exists(path.ToString(SlashMode.Native));
            public override bool File_Exists(NPath path) => System.IO.File.Exists(path.ToString(SlashMode.Native));
            public override void File_WriteAllBytes(NPath path, byte[] bytes) => System.IO.File.WriteAllBytes(path.ToString(SlashMode.Native), bytes);
            public override void File_Copy(NPath path, NPath destinationPath, bool overWrite) => System.IO.File.Copy(path.ToString(SlashMode.Native), destinationPath.ToString(SlashMode.Native), overWrite);
            public override void File_Delete(NPath path) => System.IO.File.Delete(path.ToString(SlashMode.Native));
            public override void File_Move(NPath path, NPath destinationPath) => System.IO.File.Move(path.ToString(SlashMode.Native), destinationPath.ToString(SlashMode.Native));
            public override void File_WriteAllText(NPath path, string contents) => System.IO.File.WriteAllText(path.ToString(SlashMode.Native), contents);
            public override string File_ReadAllText(NPath path) => System.IO.File.ReadAllText(path.ToString(SlashMode.Native));
            public override void File_WriteAllLines(NPath path, string[] contents) => System.IO.File.WriteAllLines(path.ToString(SlashMode.Native), contents);
            public override string[] File_ReadAllLines(NPath path) => System.IO.File.ReadAllLines(path.ToString(SlashMode.Native));
            public override byte[] File_ReadAllBytes(NPath path) => System.IO.File.ReadAllBytes(path.ToString(SlashMode.Native));
            public override void File_SetLastWriteTimeUtc(NPath path, DateTime lastWriteTimeUtc) => System.IO.File.SetLastWriteTimeUtc(path.ToString(SlashMode.Native), lastWriteTimeUtc);
            public override DateTime File_GetLastWriteTimeUtc(NPath path) => System.IO.File.GetLastWriteTimeUtc(path.ToString(SlashMode.Native));
            public override void File_SetAttributes(NPath path, FileAttributes value) => System.IO.File.SetAttributes(path.ToString(SlashMode.Native), value);

            public override FileAttributes File_GetAttributes(NPath path) => System.IO.File.GetAttributes(path.ToString(SlashMode.Native));
            public override long File_GetSize(NPath path) => new FileInfo(path.ToString(SlashMode.Native)).Length;
            public override void Directory_CreateDirectory(NPath path) => System.IO.Directory.CreateDirectory(path.ToString(SlashMode.Native));
            public override void Directory_Delete(NPath path, bool b) => System.IO.Directory.Delete(path.ToString(SlashMode.Native), b);
            public override void Directory_Move(NPath path, NPath destPath) => System.IO.Directory.Move(path.ToString(SlashMode.Native), destPath.ToString(SlashMode.Native));
            public override NPath Directory_GetCurrentDirectory() => System.IO.Directory.GetCurrentDirectory();
            public override void Directory_SetCurrentDirectory(NPath path) => System.IO.Directory.SetCurrentDirectory(path.ToString(SlashMode.Native));

            public override NPath[] Directory_GetDirectories(NPath path, string filter, SearchOption searchOptions) => System.IO.Directory.GetDirectories(path.ToString(SlashMode.Native), filter, searchOptions).ToNPaths().ToArray();
            public override NPath Resolve(NPath path) => path;
        }

        class WindowsFileSystem : SystemIOFileSystem
        {
            public override long File_GetSize(NPath path)
            {
                if (path._path.Length < Win32Native.MAX_PATH_LEN)
                    return base.File_GetSize(path);

                var longPath = MakeLongPath(path).TrimEnd('\\');
                Win32Native.FIND_DATA findData;
                IntPtr findHandle = Win32Native.FindFirstFile(longPath, out findData);
                if (findHandle == new IntPtr(-1))
                    throw new FileNotFoundException($"The path {path} does not exist.", path.ToString());
                Win32Native.FindClose(findHandle);

                var fileSizeLow = (long)findData.nFileSizeLow;
                long fileSize;
                if (fileSizeLow < 0 && (long)findData.nFileSizeHigh > 0)
                    fileSize = fileSizeLow + 0x100000000 + findData.nFileSizeHigh * 0x100000000;
                else
                {
                    if ((long)findData.nFileSizeHigh > 0)
                        fileSize = fileSizeLow + findData.nFileSizeHigh * 0x100000000;
                    else if (fileSizeLow < 0)
                        fileSize = fileSizeLow + 0x100000000;
                    else
                        fileSize = fileSizeLow;
                }

                return fileSize;
            }

            public override bool File_Exists(NPath path)
            {
                // Windows .NET implementation of File.Exists() does not handle paths longer than MAX_PATH correctly
                if (path._path.Length < Win32Native.MAX_PATH_LEN)
                    return base.File_Exists(path);

                // If the path is long, fall back to querying with P/Invoke directly
                var longPath = MakeLongPath(path).TrimEnd('\\');
                var attributes = Win32Native.GetFileAttributes(longPath);
                return attributes != Win32Native.INVALID_FILE_ATTRIBUTES && ((attributes & (uint)Win32Native.FileAttributes.Directory) == 0);
            }

            public override void File_Delete(NPath path)
            {
                if (!IsSymbolicLink(path) && path._path.Length < Win32Native.MAX_PATH_LEN)
                {
                    base.File_Delete(path);
                    return;
                }

                InternalFileDelete(path);
            }

            private void InternalFileDelete(NPath path)
            {
                // Cleaning up symlinks requires slightly special handling on Windows
                // Windows .NET implementation of File.Delete() does not handle paths longer than MAX_PATH correctly
                var longPath = MakeLongPath(path).TrimEnd('\\');
                if (!Win32Native.DeleteFile(longPath))
                {
                    // try MoveFile
                    if (!Win32Native.MoveFileEx(longPath, null, Win32Native.MoveFileExFlags.DelayUntilReboot))
                        throw new IOException($"Cannot delete file {path}.", new Win32Exception(Marshal.GetLastWin32Error()));
                }
            }

            public override void File_Copy(NPath path, NPath destinationPath, bool overWrite)
            {
                if (path._path.Length < Win32Native.MAX_PATH_LEN && destinationPath._path.Length < Win32Native.MAX_PATH_LEN)
                {
                    base.File_Copy(path, destinationPath, overWrite);
                    return;
                }

                var longPath = MakeLongPath(path).TrimEnd('\\');
                var longDestPath = MakeLongPath(destinationPath).TrimEnd('\\');
                if (!Win32Native.CopyFile(longPath, longDestPath, !overWrite))
                    throw new IOException($"Cannot copy file {path} to {destinationPath}.", new Win32Exception(Marshal.GetLastWin32Error()));
            }

            public override void File_Move(NPath path, NPath destinationPath)
            {
                if (path._path.Length < Win32Native.MAX_PATH_LEN && destinationPath._path.Length < Win32Native.MAX_PATH_LEN)
                {
                    base.File_Move(path, destinationPath);
                    return;
                }

                var longPath = MakeLongPath(path).TrimEnd('\\');
                var longDestPath = MakeLongPath(destinationPath).TrimEnd('\\');
                if (!Win32Native.MoveFile(longPath, longDestPath))
                {
                    var lastWin32Error = Marshal.GetLastWin32Error();
                    // try copy/delete
                    if (!Win32Native.CopyFile(longPath, longDestPath, true))
                        throw new IOException($"Cannot move file {path} to {destinationPath}.", new Win32Exception(lastWin32Error));

                    if (!Win32Native.DeleteFile(longPath))
                        throw new IOException($"Cannot move file {path} to {destinationPath}.", new Win32Exception(lastWin32Error));
                }
            }

            public override bool Directory_Exists(NPath path)
            {
                // Windows .NET implementation of File.Exists() does not handle paths longer than MAX_PATH correctly
                if (path._path.Length < Win32Native.MAX_PATH_LEN)
                    return base.Directory_Exists(path);

                // If the path is long, fall back to querying with P/Invoke directly
                var longPath = MakeLongPath(path).TrimEnd('\\');
                var attributes = Win32Native.GetFileAttributes(longPath);
                return attributes != Win32Native.INVALID_FILE_ATTRIBUTES &&
                    ((attributes & (uint)Win32Native.FileAttributes.Directory) != 0 || (attributes & (uint)Win32Native.FileAttributes.ReparsePoint) != 0);
            }

            public override void Directory_CreateDirectory(NPath path)
            {
                // See https://docs.microsoft.com/en-us/windows/win32/fileio/maximum-file-path-limitation
                // When using an API to create a directory, the specified path cannot be so long that you cannot append an 8.3 file name
                // (that is, the directory name cannot exceed MAX_PATH minus 12)
                if (path._path.Length < Win32Native.MAX_PATH_LEN - 12)
                {
                    base.Directory_CreateDirectory(path);
                    return;
                }

                InternalCreateDirectory(path);
            }

            private void InternalCreateDirectory(NPath directoryPath)
            {
                if (Directory_Exists(directoryPath))
                    return;

                var path = directoryPath.ToString(SlashMode.Native).TrimEnd('\\');
                var pos = path.LastIndexOf(@"\");
                if (pos > 2)
                    InternalCreateDirectory(new NPath(path.Substring(0, pos)));

                var longPath = MakeLongPath(path, Win32Native.MAX_PATH_LEN - 12);
                if (!Win32Native.CreateDirectory(longPath, IntPtr.Zero))
                {
                    var lastError = Marshal.GetLastWin32Error();
                    if (lastError == Win32Native.ERROR_INVALID_NAME && directoryPath.FileName.Length > 255)
                        throw new PathTooLongException($"Directory name {directoryPath.FileName} exceeds limit of 255 characters.");
                    throw new IOException($"Cannot create directory {directoryPath}.", new Win32Exception(lastError));
                }
            }

            public override void Directory_Delete(NPath path, bool recursive)
            {
                if (recursive)
                {
                    var files = Directory_GetFiles(path, "*", SearchOption.TopDirectoryOnly);
                    var dirs = Directory_GetDirectories(path, "*", SearchOption.TopDirectoryOnly);

                    try
                    {
                        foreach (var file in files)
                        {
	                        try
	                        {
		                        // remove read-only attribute or delete will fail
		                        var attributes = File_GetAttributes(file);
		                        if ((attributes & FileAttributes.ReadOnly) != 0)
			                        File_SetAttributes(file, attributes & ~FileAttributes.ReadOnly);

		                        File_Delete(file);
	                        }
		                    // Another process/thread may have deleted (or be in the process of deleting) the file since the time we listed out the directory, causing any of these exceptions.
	                        catch (Exception e) when (e is InvalidOperationException or FileNotFoundException or UnauthorizedAccessException)
	                        {
		                        if (file.FileExists())
			                        throw;
	                        }
                        }

                        foreach (var dir in dirs)
                            Directory_Delete(dir, true);
                    }
                    catch (IOException e)
                    {
                        throw new IOException($"Cannot delete directory {path}.", e);
                    }
                }

                var longPath = MakeLongPath(path).TrimEnd('\\');
                if (!Win32Native.RemoveDirectory(longPath))
                    throw new IOException($"Cannot delete directory {path}.", new Win32Exception(Marshal.GetLastWin32Error()));
            }

            public override void Directory_Move(NPath path, NPath destinationPath)
            {
                if (path._path.Length < Win32Native.MAX_PATH_LEN && destinationPath._path.Length < Win32Native.MAX_PATH_LEN)
                {
                    base.Directory_Move(path, destinationPath);
                    return;
                }

                var longPath = MakeLongPath(path).TrimEnd('\\');
                var longDestPath = MakeLongPath(destinationPath).TrimEnd('\\');

                if (!Win32Native.MoveFileEx(longPath, longDestPath, Win32Native.MoveFileExFlags.CopyAllowed | Win32Native.MoveFileExFlags.WriteThrough))
                {
                    var lastWin32Error = Marshal.GetLastWin32Error();
                    try
                    {
                        if (!Directory_Exists(destinationPath))
                            Directory_CreateDirectory(destinationPath);

                        var files = Directory_GetFiles(path, "*", SearchOption.AllDirectories);
                        foreach (var file in files)
                        {
                            var dest = destinationPath.Combine(file.RelativeTo(path));
                            dest.Parent.EnsureDirectoryExists();

                            File_Copy(file, dest, false);
                        }

                        Directory_Delete(path, true);
                    }
                    catch (IOException)
                    {
                        throw new IOException($"Cannot move directory {path} to {destinationPath}.", new Win32Exception(lastWin32Error));
                    }
                }
            }

            public override bool IsSymbolicLink(NPath path)
            {
                // Retrieve the reparse tag, using FindFirstFile
                Win32Native.FIND_DATA findData;
                var longPath = MakeLongPath(path).TrimEnd('\\');
                IntPtr findHandle = Win32Native.FindFirstFile(longPath, out findData);
                if (findHandle == new IntPtr(-1))
                    throw new FileNotFoundException($"The path {path} does not exist.", path.ToString());
                Win32Native.FindClose(findHandle);

                // We already tested this above, but retest just in case it changed
                if ((findData.dwFileAttributes & Win32Native.FileAttributes.ReparsePoint) != Win32Native.FileAttributes.ReparsePoint)
                    return false;

                return findData.dwReserved0 == Win32Native.IO_REPARSE_TAG_SYMLINK;
            }

            public override void CreateSymbolicLink(NPath fromPath, NPath targetPath, bool targetIsFile)
            {
                var flags = Win32Native.SymbolicLinkFlags.File;
                if (CalculateIsWindows10())
                    flags |= Win32Native.SymbolicLinkFlags.AllowUnprivilegedCreate;
                if (!targetIsFile)
                    flags |= Win32Native.SymbolicLinkFlags.Directory;

                var path = MakeLongPath(fromPath).TrimEnd('\\');
                var destPath = MakeLongPath(targetPath).TrimEnd('\\');
                if (!Win32Native.CreateSymbolicLink(path, destPath, flags))
                    throw new IOException($"Cannot create symbolic link {path} from {targetPath}.", new Win32Exception(Marshal.GetLastWin32Error()));
            }

            public override FileAttributes File_GetAttributes(NPath path)
            {
                // Windows .NET implementation of File.Exists() does not handle paths longer than MAX_PATH correctly
                if (path._path.Length < Win32Native.MAX_PATH_LEN)
                    return base.File_GetAttributes(path);

                // If the path is long, fall back to querying with P/Invoke directly
                var longPath = MakeLongPath(path).TrimEnd('\\');
                return (FileAttributes)(Win32Native.GetFileAttributes(longPath) & 0x0000FFFF);
            }

            public override void File_SetAttributes(NPath path, FileAttributes value)
            {
                if (path._path.Length < Win32Native.MAX_PATH_LEN)
                {
                    base.File_SetAttributes(path, value);
                    return;
                }

                // If the path is long, fall back to querying with P/Invoke directly
                var longPath = MakeLongPath(path).TrimEnd('\\');
                Win32Native.SetFileAttributes(longPath, (uint)value);
            }

            public override NPath[] Directory_GetFiles(NPath path, string filter, SearchOption searchOptions)
            {
                if (!Directory_Exists(path))
                    throw new DirectoryNotFoundException($"The path {path} does not exist.");

                var results = new List<NPath>();
                Win32Native.FIND_DATA findData;
                var longPath = MakeLongPath(path, Win32Native.MAX_PATH_LEN - filter.Length - 1).TrimEnd('\\') + "\\" + filter;
                IntPtr findHandle = Win32Native.FindFirstFile(longPath, out findData);
                if (findHandle != new IntPtr(-1))
                {
                    try
                    {
                        bool found;
                        do
                        {
                            var currentFileName = findData.cFileName;
                            // if this is a file, find its contents
                            if (((uint)findData.dwFileAttributes & (uint)Win32Native.FileAttributes.Directory) == 0)
                            {
                                results.Add(path.Combine(currentFileName));
                            }

                            // find next
                            found = Win32Native.FindNextFile(findHandle, out findData);
                        }
                        while (found);
                    }
                    finally
                    {
                        // close the find handle
                        Win32Native.FindClose(findHandle);
                    }
                }

                if (searchOptions == SearchOption.AllDirectories)
                {
                    foreach (var dir in Directory_GetDirectories(path, "*", SearchOption.TopDirectoryOnly))
                        results.AddRange(Directory_GetFiles(dir, filter, searchOptions));
                }

                return results.ToArray();
            }

            public override NPath[] Directory_GetDirectories(NPath path, string filter, SearchOption searchOptions)
            {
                var results = new List<NPath>();
                Win32Native.FIND_DATA findData;
                var longPath = MakeLongPath(path, Win32Native.MAX_PATH_LEN - filter.Length - 1).TrimEnd('\\') + "\\" + filter;
                IntPtr findHandle = Win32Native.FindFirstFile(longPath, out findData);
                if (findHandle != new IntPtr(-1))
                {
                    try
                    {
                        bool found;
                        do
                        {
                            var currentFileName = findData.cFileName;
                            // if this is a directory, find its contents
                            if (((uint)findData.dwFileAttributes & (uint)Win32Native.FileAttributes.Directory) != 0)
                            {
                                if (currentFileName != "." && currentFileName != "..")
                                    results.Add(path.Combine(currentFileName));
                            }

                            // find next
                            found = Win32Native.FindNextFile(findHandle, out findData);
                        }
                        while (found);
                    }
                    finally
                    {
                        // close the find handle
                        Win32Native.FindClose(findHandle);
                    }
                }

                if (searchOptions == SearchOption.AllDirectories)
                {
                    foreach (var dir in Directory_GetDirectories(path, "*", SearchOption.TopDirectoryOnly))
                        results.AddRange(Directory_GetDirectories(dir, filter, searchOptions));
                }

                return results.ToArray();
            }

            public override void File_WriteAllText(NPath path, string contents)
            {
                if (path._path.Length < Win32Native.MAX_PATH_LEN)
                {
                    base.File_WriteAllText(path, contents);
                    return;
                }

                var longPath = MakeLongPath(path).TrimEnd('\\');
                using (var handle = CreateFileHandle(path, longPath,
                    Win32Native.CreationDisposition.CreateAlways,
                    Win32Native.FileAccess.GenericWrite,
                    FileShare.Read))
                {
                    using (var fs = new FileStream(handle, FileAccess.Write))
                    {
                        using (var sw = new StreamWriter(fs))
                        {
                            sw.Write(contents, new UTF8Encoding(false, true));
                        }
                    }
                }
            }

            public override void File_WriteAllLines(NPath path, string[] contents)
            {
                File_WriteAllText(path, string.Join(Environment.NewLine, contents));
            }

            public override void File_WriteAllBytes(NPath path, byte[] bytes)
            {
                if (path._path.Length < Win32Native.MAX_PATH_LEN)
                {
                    base.File_WriteAllBytes(path, bytes);
                    return;
                }

                if (!path.Parent.Exists())
                    path.Parent.CreateDirectory();

                var longPath = MakeLongPath(path).TrimEnd('\\');
                using (var handle = CreateFileHandle(path, longPath,
                    Win32Native.CreationDisposition.CreateAlways,
                    Win32Native.FileAccess.GenericWrite,
                    FileShare.Read))
                {
                    using (var fs = new FileStream(handle, FileAccess.Write))
                    {
                        fs.Write(bytes, 0, bytes.Length);
                    }
                }
            }

            public override string File_ReadAllText(NPath path)
            {
                if (path._path.Length < Win32Native.MAX_PATH_LEN)
                    return base.File_ReadAllText(path);

                var longPath = MakeLongPath(path).TrimEnd('\\');
                string contents;
                using (var handle = CreateFileHandle(path, longPath,
                    Win32Native.CreationDisposition.OpenExisting,
                    Win32Native.FileAccess.GenericRead,
                    FileShare.Read))
                {
                    using (var fs = new FileStream(handle, FileAccess.Read))
                    {
                        using (var sr = new StreamReader(fs, new UTF8Encoding(false, true)))
                        {
                            contents = sr.ReadToEnd();
                        }
                    }
                    return contents;
                }
            }

            public override string[] File_ReadAllLines(NPath path)
            {
                if (path._path.Length < Win32Native.MAX_PATH_LEN)
                    return base.File_ReadAllLines(path);

                var lines = new List<string>();
                var longPath = MakeLongPath(path).TrimEnd('\\');
                using (var handle = CreateFileHandle(path, longPath,
                    Win32Native.CreationDisposition.OpenExisting,
                    Win32Native.FileAccess.GenericRead,
                    FileShare.Read))
                {
                    using (var fs = new FileStream(handle, FileAccess.Read))
                    {
                        using (var sr = new StreamReader(fs, new UTF8Encoding(false, true)))
                        {
                            string line;
                            while ((line = sr.ReadLine()) != null)
                                lines.Add(line);
                        }
                    }
                }
                return lines.ToArray();
            }

            public override byte[] File_ReadAllBytes(NPath path)
            {
                if (path._path.Length < Win32Native.MAX_PATH_LEN)
                    return base.File_ReadAllBytes(path);

                byte[] buffer = null;
                var longPath = MakeLongPath(path).TrimEnd('\\');
                using (var handle = CreateFileHandle(path, longPath,
                    Win32Native.CreationDisposition.OpenExisting,
                    Win32Native.FileAccess.GenericRead,
                    FileShare.Read))
                {
                    using (var fs = new FileStream(handle, FileAccess.Read))
                    {
                        buffer = new byte[fs.Length];
                        fs.Read(buffer, 0, buffer.Length);
                    }
                }
                return buffer;
            }

            public override void File_SetLastWriteTimeUtc(NPath path, DateTime lastWriteTimeUtc)
            {
                if (path._path.Length < Win32Native.MAX_PATH_LEN)
                {
                    base.File_SetLastWriteTimeUtc(path, lastWriteTimeUtc);
                    return;
                }

                var longPath = MakeLongPath(path).TrimEnd('\\');
                using (var handle = CreateFileHandle(path, longPath,
                    Win32Native.CreationDisposition.OpenExisting,
                    Win32Native.FileAccess.FileWriteAttributes,
                    FileShare.ReadWrite))
                {
                    var d = lastWriteTimeUtc.ToFileTime();
                    if (!Win32Native.SetLastWriteFileTime(handle.DangerousGetHandle(), IntPtr.Zero, IntPtr.Zero, ref d))
                        throw new IOException($"Cannot set last write time to {path}.", new Win32Exception(Marshal.GetLastWin32Error()));
                }
            }

            public override DateTime File_GetLastWriteTimeUtc(NPath path)
            {
                if (path._path.Length < Win32Native.MAX_PATH_LEN)
                    return base.File_GetLastWriteTimeUtc(path);

                Win32Native.FIND_DATA findData;
                var longPath = MakeLongPath(path).TrimEnd('\\');
                IntPtr findHandle = Win32Native.FindFirstFile(longPath, out findData);
                if (findHandle == new IntPtr(-1))
                    return DateTime.MinValue;

                try
                {
                    if (findHandle.ToInt64() == Win32Native.ERROR_FILE_NOT_FOUND)
                        return DateTime.MinValue;

                    var utcFileTime = ((long)findData.ftLastWriteTime.dwHighDateTime << 32) + findData.ftLastWriteTime.dwLowDateTime;
                    return DateTime.FromFileTimeUtc(utcFileTime);
                }
                finally
                {
                    Win32Native.FindClose(findHandle);
                }
            }

            public override void Directory_SetCurrentDirectory(NPath path)
            {
                if (path._path.Length < Win32Native.MAX_PATH_LEN)
                {
                    base.Directory_SetCurrentDirectory(path);
                    return;
                }


                var shortPath = GetShortName(path);
                if (!Win32Native.SetCurrentDirectory(shortPath))
                    throw new IOException($"Cannot set current directory to {path}.", new Win32Exception(Marshal.GetLastWin32Error()));
            }

            public override NPath Directory_GetCurrentDirectory()
            {
                NPath path = System.IO.Directory.GetCurrentDirectory();
                if (path._path.IndexOf('~') > 0)
                    return GetLongName(path);
                return path;
            }

            private static string GetShortName(NPath path)
            {
                var longPath = MakeLongPath(path).TrimEnd('\\');
                char[] shortPathChars = new char[Win32Native.MAX_PATH_LEN];
                var length = Win32Native.GetShortPathName(longPath, shortPathChars, (uint)shortPathChars.Length);
                if (length <= 0)
                    throw new IOException($"Cannot get short path name for {path}.", new Win32Exception(Marshal.GetLastWin32Error()));

                var shortPath = new string(shortPathChars);
                return shortPath.Substring(0, (int)length);
            }

            private static string GetLongName(NPath path)
            {
                char[] longPathChars = null;
                var length = Win32Native.GetLongPathName(path._path, longPathChars, 0);
                if (length <= 0)
                    throw new IOException($"Cannot get long path name for {path}.", new Win32Exception(Marshal.GetLastWin32Error()));

                longPathChars = new char[length];
                if (Win32Native.GetLongPathName(path._path, longPathChars, length) <= 0)
                    throw new IOException($"Cannot get long path name for {path}.", new Win32Exception(Marshal.GetLastWin32Error()));

                var longPath = new string(longPathChars);
                return longPath.Substring(0, (int)length - 1);
            }

            private static SafeFileHandle CreateFileHandle(
                NPath path,
                string filePath,
                Win32Native.CreationDisposition creationDisposition,
                Win32Native.FileAccess fileAccess,
                FileShare fileShare)
            {
                var fileHandle = Win32Native.CreateFile(filePath, fileAccess, fileShare,
                    IntPtr.Zero,
                    creationDisposition,
                    Win32Native.FileAttributes.Normal,
                    IntPtr.Zero);
                if (fileHandle.IsInvalid)
                {
                    var lastError = Marshal.GetLastWin32Error();
                    if (lastError == Win32Native.ERROR_INVALID_NAME && path.FileName.Length > 255)
                        throw new PathTooLongException($"File name {path.FileName} exceeds limit of 255 characters.");
                    throw new IOException($"Cannot access {filePath}.", new Win32Exception(lastError));
                }

                return fileHandle;
            }

            private static string MakeLongPath(NPath path, int maxLength = Win32Native.MAX_PATH_LEN)
            {
                NPath localPath = path._path;
                if (localPath.IsRelative)
                    localPath = localPath.MakeAbsolute();

                var longPath = localPath.ToString(SlashMode.Native);
                if (string.IsNullOrEmpty(longPath) || longPath.StartsWith(@"\\?\"))
                    return longPath;

                if (longPath.Length >= maxLength)
                    return @"\\?\" + longPath;

                return longPath;
            }

            static class Win32Native
            {
                public const long ERROR_FILE_NOT_FOUND = 2;
                public const int ERROR_INVALID_NAME = 123;
                public const int ERROR_DIR_NOT_EMPTY = 145;

                // See https://docs.microsoft.com/en-us/windows/win32/fileio/naming-a-file#maximum-path-length-limitation
                public const int MAX_PATH_LEN = 260;

                [Flags]
                public enum SymbolicLinkFlags
                {
                    File = 0,
                    Directory = 1,
                    AllowUnprivilegedCreate = 2
                }

                public enum CreationDisposition : uint
                {
                    New = 1,
                    CreateAlways = 2,
                    OpenExisting = 3,
                    OpenAlways = 4,
                    TruncateExisting = 5
                }

                [Flags]
                public enum FileAccess : uint
                {
                    GenericRead = 0x80000000,
                    GenericWrite = 0x40000000,
                    GenericExecute = 0x20000000,
                    GenericAll = 0x10000000,
                    FileReadAttributes = 0x80,
                    FileWriteAttributes = 0x100,
                    FileAppendData = 4
                }

                [Flags]
                public enum FileAttributes : uint
                {
                    Readonly = 0x00000001,
                    Hidden = 0x00000002,
                    System = 0x00000004,
                    Directory = 0x00000010,
                    Archive = 0x00000020,
                    Device = 0x00000040,
                    Normal = 0x00000080,
                    Temporary = 0x00000100,
                    SparseFile = 0x00000200,
                    ReparsePoint = 0x00000400,
                    Compressed = 0x00000800,
                    Offline = 0x00001000,
                    NotContentIndexed = 0x00002000,
                    Encrypted = 0x00004000,
                    Write_Through = 0x80000000,
                    Overlapped = 0x40000000,
                    NoBuffering = 0x20000000,
                    RandomAccess = 0x10000000,
                    SequentialScan = 0x08000000,
                    DeleteOnClose = 0x04000000,
                    BackupSemantics = 0x02000000,
                    PosixSemantics = 0x01000000,
                    OpenReparsePoint = 0x00200000,
                    OpenNoRecall = 0x00100000,
                    FirstPipeInstance = 0x00080000
                }

                [Flags]
                public enum MoveFileExFlags : uint
                {
                    None = 0x0,
                    ReplaceExisting = 0x1,
                    CopyAllowed = 0x2,
                    DelayUntilReboot = 0x4,
                    WriteThrough = 0x8,
                    CreateHardlink = 0x10,
                    FailIfNotTrackable = 0x20
                }

                [DllImport(@"kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
                public static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, SymbolicLinkFlags dwFlags);

                [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
                public struct FIND_DATA
                {
                    public FileAttributes dwFileAttributes;
                    public FILETIME ftCreationTime;
                    public FILETIME ftLastAccessTime;
                    public FILETIME ftLastWriteTime;
                    public uint nFileSizeHigh;
                    public uint nFileSizeLow;
                    public uint dwReserved0;
                    public uint dwReserved1;

                    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
                    public string cFileName;

                    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
                    public string cAlternateFileName;
                }

                [DllImport(@"kernel32.dll", CharSet = CharSet.Unicode)]
                public static extern IntPtr FindFirstFile(string fileName, out FIND_DATA findData);

                [DllImport(@"kernel32.dll", CharSet = CharSet.Unicode)]
                [return : MarshalAs(UnmanagedType.Bool)]
                public static extern bool FindNextFile(IntPtr handle, out FIND_DATA findData);

                [DllImport(@"kernel32.dll", CharSet = CharSet.Unicode)]
                public static extern bool FindClose(IntPtr handle);

                public const uint IO_REPARSE_TAG_SYMLINK = 0xA000000C;

                [DllImport(@"kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
                public static extern uint GetFileAttributes(string fileName);

                [DllImport(@"kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
                public static extern void SetFileAttributes(string fileName, uint attributes);

                public const uint INVALID_FILE_ATTRIBUTES = 0xffffffff;

                [DllImport(@"kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
                [return : MarshalAs(UnmanagedType.Bool)]
                public static extern bool DeleteFile(string lpFileName);

                [DllImport(@"kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
                [return : MarshalAs(UnmanagedType.Bool)]
                public static extern bool CopyFile(string sourceFileName, string destFileName, bool failIfExists);

                [DllImport(@"kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
                [return : MarshalAs(UnmanagedType.Bool)]
                public static extern bool MoveFile(string existingFileName, string newFileName);

                [DllImport(@"kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
                [return : MarshalAs(UnmanagedType.Bool)]
                public static extern bool MoveFileEx(string existingFileName, string newFileName, MoveFileExFlags exFlags);

                [DllImport(@"kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
                [return : MarshalAs(UnmanagedType.Bool)]
                public static extern bool CreateDirectory(string lpPathName, IntPtr lpSecurityAttributes);

                [DllImport(@"kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
                [return : MarshalAs(UnmanagedType.Bool)]
                public static extern bool RemoveDirectory(string lpPathName);

                [DllImport(@"kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
                [return : MarshalAs(UnmanagedType.Bool)]
                public static extern bool SetCurrentDirectory(string lpPathName);

                [DllImport(@"kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
                internal static extern uint GetCurrentDirectory(uint nBufferLength, char[] lpBuffer);

                [DllImport(@"kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
                public static extern SafeFileHandle CreateFile(
                    string lpFileName,
                    FileAccess dwDesiredAccess,
                    FileShare dwShareMode,
                    IntPtr lpSecurityAttributes,
                    CreationDisposition dwCreationDisposition,
                    FileAttributes dwFlagsAndAttributes,
                    IntPtr hTemplateFile);

                [DllImport(@"kernel32.dll", EntryPoint = "SetFileTime", SetLastError = true, CharSet = CharSet.Unicode)]
                [return : MarshalAs(UnmanagedType.Bool)]
                public static extern bool SetLastWriteFileTime(
                    IntPtr hFile,
                    IntPtr lpCreationTime,
                    IntPtr lpLastAccessTime,
                    ref long lpLastWriteTime);

                [DllImport(@"kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
                public static extern uint GetShortPathName(
                    string lpszLongPath,
                    char[] lpszShortPath,
                    uint cchBuffer);

                [DllImport(@"kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
                public static extern uint GetLongPathName(
                    string lpszShortPath,
                    char[] lpszLongPath,
                    uint cchBuffer);
            }
        }

        class PosixFileSystem : SystemIOFileSystem
        {
            public override bool IsSymbolicLink(NPath path)
            {
                var pathInfo = new FileInfo(path.ToString());
                return pathInfo.Attributes.HasFlag(FileAttributes.ReparsePoint);
            }

            public override void CreateSymbolicLink(NPath fromPath, NPath targetPath, bool targetIsFile)
            {
                int retVal = PosixNative.symlink(targetPath.ToString(SlashMode.Native), fromPath.ToString(SlashMode.Native));
                if (retVal != 0)
                {
                    var errorCode = Marshal.GetLastWin32Error();
                    throw new IOException($"Failed to create symlink (error code {errorCode})", errorCode);
                }
            }

            public override void File_SetAttributes(NPath path, FileAttributes value)
            {
                if ((value & FileAttributes.Hidden) != 0)
                    throw new NotSupportedException($"{FileAttributes.Hidden} file attribute is only supported on Windows.");
                base.File_SetAttributes(path, value);
            }

            static class PosixNative
            {
                [DllImport("libc", SetLastError = true)]
                public static extern int symlink([MarshalAs(UnmanagedType.LPStr)] string targetPath,
                    [MarshalAs(UnmanagedType.LPStr)] string linkPath);
            }
        }

        /// <summary>
        /// A Filesystem that forwards all calls to another filesytem. Derive your own filesystem from this if you only want to
        /// change the behaviour of a few methods.
        /// </summary>
        public abstract class RelayingFileSystem : FileSystem
        {
            /// <summary>
            /// The filesystem all methods will be forwarded to
            /// </summary>
            protected FileSystem BaseFileSystem { get; }

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="baseFileSystem">the filesystem all calls will be forwarded to</param>
            protected RelayingFileSystem(NPath.FileSystem baseFileSystem) => BaseFileSystem = baseFileSystem;
            /// <inheritdoc />
            public override NPath[] Directory_GetFiles(NPath path, string filter, SearchOption searchOptions) => BaseFileSystem.Directory_GetFiles(path, filter, searchOptions);
            /// <inheritdoc />
            public override bool Directory_Exists(NPath path) => BaseFileSystem.Directory_Exists(path);
            /// <inheritdoc />
            public override bool File_Exists(NPath path) => BaseFileSystem.File_Exists(path);
            /// <inheritdoc />
            public override void File_WriteAllBytes(NPath path, byte[] bytes) => BaseFileSystem.File_WriteAllBytes(path, bytes);
            /// <inheritdoc />
            public override void File_Copy(NPath path, NPath destinationPath, bool overWrite) => BaseFileSystem.File_Copy(path, destinationPath, overWrite);
            /// <inheritdoc />
            public override void File_Delete(NPath path) => BaseFileSystem.File_Delete(path);
            /// <inheritdoc />
            public override void File_Move(NPath path, NPath destinationPath) => BaseFileSystem.File_Move(path, destinationPath);
            /// <inheritdoc />
            public override void File_WriteAllText(NPath path, string contents) => BaseFileSystem.File_WriteAllText(path, contents);
            /// <inheritdoc />
            public override string File_ReadAllText(NPath path) => BaseFileSystem.File_ReadAllText(path);
            /// <inheritdoc />
            public override void File_WriteAllLines(NPath path, string[] contents) => BaseFileSystem.File_WriteAllLines(path, contents);
            /// <inheritdoc />
            public override string[] File_ReadAllLines(NPath path) => BaseFileSystem.File_ReadAllLines(path);
            /// <inheritdoc />
            public override byte[] File_ReadAllBytes(NPath path) => BaseFileSystem.File_ReadAllBytes(path);
            /// <inheritdoc />
            public override void File_SetLastWriteTimeUtc(NPath path, DateTime lastWriteTimeUtc) => BaseFileSystem.File_SetLastWriteTimeUtc(path, lastWriteTimeUtc);
            /// <inheritdoc />
            public override DateTime File_GetLastWriteTimeUtc(NPath path) => BaseFileSystem.File_GetLastWriteTimeUtc(path);
            /// <inheritdoc />
            public override void File_SetAttributes(NPath path, FileAttributes value) => BaseFileSystem.File_SetAttributes(path, value);
            /// <inheritdoc />
            public override FileAttributes File_GetAttributes(NPath path) => BaseFileSystem.File_GetAttributes(path);
            /// <inheritdoc />
            public override long File_GetSize(NPath path) => BaseFileSystem.File_GetSize(path);
            /// <inheritdoc />
            public override void Directory_CreateDirectory(NPath path) => BaseFileSystem.Directory_CreateDirectory(path);
            /// <inheritdoc />
            public override void Directory_Delete(NPath path, bool b) => BaseFileSystem.Directory_Delete(path, b);
            /// <inheritdoc />
            public override void Directory_Move(NPath path, NPath destPath) => BaseFileSystem.Directory_Move(path, destPath);
            /// <inheritdoc />
            public override NPath Directory_GetCurrentDirectory() => BaseFileSystem.Directory_GetCurrentDirectory();
            /// <inheritdoc />
            public override void Directory_SetCurrentDirectory(NPath directoryPath) => BaseFileSystem.Directory_SetCurrentDirectory(directoryPath);
            /// <inheritdoc />
            public override NPath[] Directory_GetDirectories(NPath path, string filter, SearchOption searchOptions) => BaseFileSystem.Directory_GetDirectories(path, filter, searchOptions);

            /// <inheritdoc />
            public override NPath Resolve(NPath path) => BaseFileSystem.Resolve(path);
            /// <inheritdoc />
            public override bool IsSymbolicLink(NPath path) => BaseFileSystem.IsSymbolicLink(path);
            /// <inheritdoc />
            public override void CreateSymbolicLink(NPath fromPath, NPath targetPath, bool targetIsFile) => BaseFileSystem.CreateSymbolicLink(fromPath, targetPath, targetIsFile);
        }

        class WithFileSystemHelper : IDisposable
        {
            private FileSystem _previousFileSystem;
            private FileSystem _newFileSystem;

            public WithFileSystemHelper(FileSystem newFileSystem)
            {
                _previousFileSystem = FileSystem.Active;
                _newFileSystem = newFileSystem;
                FileSystem._active = newFileSystem;
            }

            public void Dispose()
            {
                if (FileSystem._active != _newFileSystem)
                    throw new InvalidOperationException("While disposing WithFileSystem result, the originally set FileSystem was not the active one.");
                FileSystem._active = _previousFileSystem;
            }
        }

        /// <summary>
        /// Temporarily assume that the current directory is a given value, instead of querying it from the environment when needed, in order to improve performance.
        /// </summary>
        /// <param name="frozenCurrentDirectory">The current directory to assume.</param>
        /// <returns>A token representing the registered callback. This should be disposed of when the assumption is no longer required. The usual usage pattern is to capture the token with a <c>using</c> statement, such that it is automatically disposed of when the <c>using</c> block exits.</returns>
        [Obsolete("Obsolete. If you need this behaviour you can implement a custom NPath.FileSystem and install it with NPath.WithFileSystem()", true)]
        public static IDisposable WithFrozenCurrentDirectory(NPath frozenCurrentDirectory) => throw null;
    }

    /// <summary>
    /// NPath-related extension methods for other common types.
    /// </summary>
    internal static class Extensions
    {
        /// <summary>
        /// Copy these NPaths into the given directory.
        /// </summary>
        /// <param name="self">An enumerable sequence of NPaths.</param>
        /// <param name="dest">The path to the target directory.</param>
        /// <returns>The paths to the newly copied files.</returns>
        /// <remarks>All path information in the source paths is ignored, other than the final file name; the resulting copied files and directories will all be immediate children of the target directory.</remarks>
        public static IEnumerable<NPath> Copy(this IEnumerable<NPath> self, NPath dest)
        {
            if (dest.IsRelative)
                throw new ArgumentException("When copying multiple files, the destination cannot be a relative path");
            dest.EnsureDirectoryExists();
            return self.Select(p => p.Copy(dest.Combine(p.FileName))).ToArray();
        }

        /// <summary>
        /// Move these NPaths into the given directory.
        /// </summary>
        /// <param name="self">An enumerable sequence of NPaths.</param>
        /// <param name="dest">The path to the target directory.</param>
        /// <returns>The paths to the newly moved files.</returns>
        /// <remarks>All path information in the source paths is ignored, other than the final file name; the resulting moved files and directories will all be immediate children of the target directory.</remarks>
        public static IEnumerable<NPath> Move(this IEnumerable<NPath> self, NPath dest)
        {
            if (dest.IsRelative)
                throw new ArgumentException("When moving multiple files, the destination cannot be a relative path");
            dest.EnsureDirectoryExists();
            return self.Select(p => p.Move(dest.Combine(p.FileName))).ToArray();
        }

        /// <summary>
        /// Delete the files/directories targetted by these paths.
        /// </summary>
        /// <param name="self">The paths to delete.</param>
        /// <returns>All paths that were passed in to the method.</returns>
        public static IEnumerable<NPath> Delete(this IEnumerable<NPath> self)
        {
            foreach (var p in self)
                p.Delete();
            return self;
        }

        /// <summary>
        /// Convert all these paths to quoted strings, using the requested path separator type.
        /// </summary>
        /// <param name="self">The paths to convert.</param>
        /// <param name="slashMode">The path separator type to use. Defaults to <c>SlashMode.Forward</c>.</param>
        /// <returns>The paths, converted to quoted strings.</returns>
        public static IEnumerable<string> InQuotes(this IEnumerable<NPath> self,
            SlashMode slashMode = SlashMode.Forward)
        {
            return self.Select(p => p.InQuotes(slashMode));
        }

        /// <summary>
        /// Construct a new NPath from this string.
        /// </summary>
        /// <param name="path">The string to construct the path from.</param>
        /// <returns>A new NPath constructed from this string.</returns>
        public static NPath ToNPath(this string path)
        {
            return new NPath(path);
        }

        /// <summary>
        /// Construct new NPaths from each of these strings.
        /// </summary>
        /// <param name="paths">The strings to construct NPaths from.</param>
        /// <returns>The newly constructed NPaths.</returns>
        public static IEnumerable<NPath> ToNPaths(this IEnumerable<string> paths)
        {
            return paths.Select(p => new NPath(p));
        }

        /// <summary>
        /// Invokes .ResolveWithFileSystem on all NPaths
        /// </summary>
        /// <param name="paths"></param>
        /// <returns></returns>
        public static NPath[] ResolveWithFileSystem(this IEnumerable<NPath> paths) => paths.Select(p => p.ResolveWithFileSystem()).ToArray();

        /// <summary>
        /// Invokes InQuotesResolved on all NPaths
        /// </summary>
        /// <param name="paths"></param>
        /// <returns></returns>
        public static string[] InQuotesResolved(this IEnumerable<NPath> paths) => paths.Select(p => p.InQuotesResolved()).ToArray();
    }

    /// <summary>
    /// Describes the different kinds of path separators that can be used when converting NPaths back into strings.
    /// </summary>
    internal enum SlashMode
    {
        /// <summary>
        /// Use the slash mode that is native for the current platform - backslashes on Windows, forward slashes on macOS and Linux systems.
        /// </summary>
        Native,

        /// <summary>
        /// Use forward slashes as path separators.
        /// </summary>
        Forward,

        /// <summary>
        /// Use backslashes as path separators.
        /// </summary>
        Backward
    }

    /// <summary>
    /// Specifies the way that directory deletion should be performed.
    /// </summary>
    internal enum DeleteMode
    {
        /// <summary>
        /// When deleting a directory, if an IOException occurs, rethrow it.
        /// </summary>
        Normal,

        /// <summary>
        /// When deleting a directory, if an IOException occurs, ignore it. The deletion request may or may not be later fulfilled by the OS.
        /// </summary>
        Soft
    }

    internal struct Do_Not_Use_File_Directly_Use_FileSystem_Active_Instead
    {
    }
    internal struct Do_Not_Use_Directory_Directly_Use_FileSystem_Active_Instead
    {
    }
}
