using System;
using System.IO;

namespace WB.UI.Shared.Enumerator.Services
{
    public class AndroidPathUtils
    {
        public static string GetPathToSubfolderInLocalDirectory(string subFolderName)
        {
            var pathToSubfolderInLocalDirectory = Path.Combine(GetPathToInternalDirectory(), subFolderName);
            if (!Directory.Exists(pathToSubfolderInLocalDirectory))
            {
                Directory.CreateDirectory(pathToSubfolderInLocalDirectory);
            }

            return pathToSubfolderInLocalDirectory;
        }

        public static string GetPathToInternalDirectory()
            => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        public static string GetPathToExternalDirectory()
            => Android.OS.Environment.ExternalStorageDirectory.AbsolutePath;

        public static string GetPathToSubfolderInExternalDirectory(string subFolderName)
        {
            return Path.Combine(GetPathToExternalDirectory(), "Interviewer", subFolderName);
        }
    }
}