using System;
using System.IO;
using Microsoft.Win32;

namespace SamSoundsReplacer
{
    internal class GetPath
    {
        public static string SteamPath(string path)
        {
            try
            {
                foreach (string directory in Directory.GetDirectories(path))
                {
                    if (directory.EndsWith("workshop\\content\\564310"))
                    {
                        return directory;
                    }
                    else
                    {
                        string result = SteamPath(directory); // 递归扫描子目录
                        if (result != null)
                        {
                            return result;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // MessageBox.Show("Error finding game files " + ex.Message);
            }
            return null;
        }
    }
}
