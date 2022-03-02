using System.IO;

namespace MicrosoftRewardsFarmer
{
    public static class AppPath
    {
        #region Methods
        /// <summary>
        /// Get the full path of a file inside the app directory
        /// </summary>
        /// <param name="FileName">Relative path</param>
        /// <returns>The full path</returns>
        public static string GetFullPath(string FileName)
        {
            string exeFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string workPath = Path.GetDirectoryName(exeFilePath);
            string relativePath = Path.Combine(workPath + FileName);
            string path = Path.GetFullPath(relativePath);

            return path;
        }
        #endregion
    }
}
