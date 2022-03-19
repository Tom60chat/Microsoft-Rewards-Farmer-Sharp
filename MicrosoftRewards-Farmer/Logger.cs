using System;
using System.IO;
using System.Threading.Tasks;

namespace MicrosoftRewardsFarmer
{
    public static class Logger
    {
        private static readonly object crashLoggerLock = new object();
        private static readonly string logPath = AppPath.GetFullPath(@$"\Logs\{DateTimeOffset.Now:yyyy-MM-dd HH.mm.ss}.txt");

        /// <summary>
        /// Write the exeption in the current log file
        /// </summary>
        /// <param name="exception">The exeption to write</param>
        public static void Write(Exception exception, string tag = "")
        {
            Write("Exception", exception.ToString(), tag);
        }

        /// <summary>
        /// Write the log in the current log file
        /// </summary>
        /// <param name="exception">The exeption to write</param>
        public static void Write(string log, string tag = "")
        {
            Write("Log", log, tag);
        }

        private static void Write (string logLevel, string text, string tag)
        {
            lock (crashLoggerLock)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(logPath));
                File.AppendAllText(logPath,
                    $"[{logLevel}] " + tag + Environment.NewLine +
                    text + Environment.NewLine +
                    Environment.NewLine);
            }
        }

        /*/// <summary>
        /// Write the exeption in the current log file
        /// </summary>
        /// <param name="exception">The exeption to write</param>
        public static async Task WriteAsync(Exception exception)
        {
            lock(crashLoggerLock)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(logPath));
                await File.AppendAllTextAsync(logPath, exception.ToString());
            }
        }*/
    }
}
