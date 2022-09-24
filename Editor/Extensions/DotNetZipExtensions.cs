using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Studious
{
    public static class ZipExtensions
    {
        public static IEnumerable<FileSystemInfo> AllFilesAndFolders(this DirectoryInfo dir)
        {
            foreach (var file in dir.GetFiles())
            {
                yield return file;
            }

            foreach (var directory in dir.GetDirectories())
            {
                yield return directory;
                foreach (var info in AllFilesAndFolders(directory))
                {
                    yield return info;
                }
            }
        }
    }
}
