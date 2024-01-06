using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FreedomClient.Utilities
{
    public static class FileSystemUtilities
    {
        public static void RemoveEmptyDirectories(string path)
        {
            foreach (var subDirectory in Directory.GetDirectories(path))
            {
                RemoveEmptyDirectories(subDirectory);
                if (Directory.EnumerateFiles(subDirectory, "*", SearchOption.AllDirectories).Count() == 0)
                {
                    Directory.Delete(subDirectory);
                }
            }
        }
    }
}
