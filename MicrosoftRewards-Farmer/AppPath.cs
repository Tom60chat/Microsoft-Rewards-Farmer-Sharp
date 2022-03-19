using System;
using System.IO;

namespace MicrosoftRewardsFarmer
{
    public static class AppPath
    {
        #region Methods
        /// <summary>
        /// Get the full path of a file inside the app directory
        /// </summary>
        /// <param name="fileName">Relative path</param>
        /// <returns>The full path</returns>
        public static string GetFullPath(string fileName)
        {
            fileName = fileName.Replace('\\', '/');
            fileName = fileName.TrimStart('/');

            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string relativePath = Path.Combine(baseDirectory, fileName);
            string path = Path.GetFullPath(relativePath);

            return path;
        }
        #endregion
    }
}
