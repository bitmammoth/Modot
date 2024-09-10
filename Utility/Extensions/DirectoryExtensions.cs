using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

using JetBrains.Annotations;

namespace Godot.Utility.Extensions
{
    /// <summary>
    /// Contains extension methods for <see cref="Directory"/>.
    /// </summary>
    [PublicAPI]
    public static class DirectoryExtensions
    {
        /// <summary>
        /// Copies all files from the directory at <paramref name="from"/> to the directory at <paramref name="to"/>.
        /// </summary>
        /// <param name="directory">The <see cref="Directory"/> to use when copying files.</param>
        /// <param name="from">The source directory path. It can be an absolute path, or relative to <paramref name="directory"/>.</param>
        /// <param name="to">The destination directory path. It can be an absolute path, or relative to <paramref name="directory"/>.</param>
        /// <param name="recursive">Whether the contents should be copied recursively (i.e. copy files inside subdirectories and so on) or not.</param>
        /// <returns>An array of the paths of all files that were copied from <paramref name="from"/> to <paramref name="to"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string[] CopyContents(this DirAccess dirAccess, string from, string to, bool recursive = false)
        {
            return dirAccess.CopyContentsLazy(from, to, recursive).ToArray();
        }


        /// <summary>
        /// Returns the complete file paths of all files inside <paramref name="directory"/>.
        /// </summary>
        /// <param name="directory">The <see cref="Directory"/> to search in.</param>
        /// <param name="recursive">Whether the search should be conducted recursively (return paths of files inside <paramref name="directory"/>'s subdirectories and so on) or not.</param>
        /// <returns>An array of the paths of all files inside <paramref name="directory"/>.</returns>
        [MustUseReturnValue]
        public static string[] GetFiles(this DirAccess dirAccess, bool recursive = false, params string[] fileExtensions)
        {
            var files = dirAccess.GetFiles(recursive);

            return fileExtensions.Any()
                ? Array.FindAll(files, file => fileExtensions.Any(file.EndsWith))
                : files;
        }

        private static string[] GetFiles(this DirAccess dirAccess, bool recursive = false)
        {
            return recursive
                ? dirAccess.GetElementsNonRecursive(true).ToArray()
                : dirAccess.GetElementsNonRecursive(true).ToArray();
        }


        /// <summary>
        /// Returns the complete directory paths of all directories inside <paramref name="directory"/>.
        /// </summary>
        /// <param name="directory">The <see cref="Directory"/> to search in.</param>
        /// <param name="recursive">Whether the search should be conducted recursively (return paths of directories inside <paramref name="directory"/>'s subdirectories and so on) or not.</param>
        /// <returns>An array of the paths of all files inside <paramref name="directory"/>.</returns>
        [MustUseReturnValue]
        public static string[] GetDirectories(this DirAccess dirAccess, bool recursive = false)
        {
            return recursive
                ? dirAccess
                    .GetElementsNonRecursive(false)
                    .SelectMany(path =>
                    {
                        var recursiveDirAccess = DirAccess.Open(path);
                        if (recursiveDirAccess == null)
                            throw new InvalidOperationException($"Cannot open directory: {path}");

                        return recursiveDirAccess.GetDirectories(true).Prepend(path);
                    })
                    .ToArray()
                : dirAccess
                    .GetElementsNonRecursive(false)
                    .ToArray();
        }


        private static IEnumerable<string> GetElementsNonRecursive(this DirAccess dirAccess, bool trueIfFiles)
        {
            dirAccess.ListDirBegin();
            while (true)
            {
                string next = dirAccess.GetNext();
                if (string.IsNullOrEmpty(next))
                {
                    yield break;
                }
                // Continue if the current element is a file or directory depending on which one is being queried
                if (dirAccess.CurrentIsDir() == trueIfFiles)
                {
                    continue;
                }
                string current = dirAccess.GetCurrentDir();
                yield return current.EndsWith("/") ? $"{current}{next}" : $"{current}/{next}";
            }
        }


        private static IEnumerable<string> CopyContentsLazy(this DirAccess dirAccess, string from, string to, bool recursive = false)
        {
            dirAccess = DirAccess.Open(from);
            if (dirAccess == null)
                throw new InvalidOperationException($"Cannot open directory: {from}");

            // Create destination directory
            var dirAccessTo = DirAccess.Open(to);
            dirAccessTo.MakeDirRecursive(to);

            // Replace paths
            Regex fromReplacement = new(Regex.Escape(from));

            // Copy all files non-recursively
            dirAccess.ListDirBegin();
            string fromFile;
            while ((fromFile = dirAccess.GetNext()) != "")
            {
                if (dirAccess.CurrentIsDir())
                    continue; // Skip directories

                string toFile = fromReplacement.Replace(fromFile, to, 1);
                CopyFile(fromFile, toFile); // Manual file copy
                yield return toFile;
            }
            dirAccess.ListDirEnd();

            if (!recursive)
                yield break;

            // Copy files recursively
            dirAccess.ListDirBegin();
            while ((fromFile = dirAccess.GetNext()) != "")
            {
                if (!dirAccess.CurrentIsDir())
                    continue;

                string fromSubDir = from + "/" + fromFile;
                string toSubDir = to + "/" + fromFile;
                var subDirAccess = DirAccess.Open(fromSubDir);

                foreach (var file in subDirAccess.CopyContentsLazy(fromSubDir, toSubDir, true))
                {
                    yield return file;
                }
            }
            dirAccess.ListDirEnd();
        }

        private static void CopyFile(string fromFile, string toFile)
        {
            using var srcFile = FileAccess.Open(fromFile, FileAccess.ModeFlags.Read);
            using var destFile = FileAccess.Open(toFile, FileAccess.ModeFlags.Write);

            byte[] buffer = srcFile.GetBuffer((int)srcFile.GetLength());
            destFile.StoreBuffer(buffer);
        }

    }
}