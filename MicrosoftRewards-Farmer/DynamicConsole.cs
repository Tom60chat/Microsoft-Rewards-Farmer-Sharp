using System;
using System.Collections.Generic;
using System.Text;

namespace MicrosoftRewardsFarmer
{
    public static class DynamicConsole
    {
        private static readonly object DynamicConsoleLock = new object();

        /// <summary>
        ///     Sets the position of the cursor and writes the specified string value to the standard output stream.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <param name="left">
        ///     The column position of the cursor. Columns are numbered from left to right starting
        ///     at 0.
        /// </param>
        /// <param name="top">
        ///     The row position of the cursor. Rows are numbered from top to bottom starting
        ///     at 0.
        /// </param>
        /// <return>
        /// The new column position of the cursor after the writing.
        /// </return>
        public static int Write(string value, int left, int top) => Write(value, left, top, false);

        /// <summary>
        ///     Sets the position of the cursor and writes the specified string value, followed by the current line terminator, to the standard output stream.
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <param name="left">
        ///     The column position of the cursor. Columns are numbered from left to right starting
        ///     at 0.
        /// </param>
        /// <param name="top">
        ///     The row position of the cursor. Rows are numbered from top to bottom starting
        ///     at 0.
        /// </param>
        /// <return>
        /// The new column position of the cursor after the writing.
        /// </return>
        public static int WriteLine(string value, int left, int top) => Write(value, left, top, true);

        private static int Write(string value, int left, int top, bool newLine)
        {
            int oldLeft = Console.CursorLeft;
            int oldTop = Console.CursorTop;
            int newLeft = 0;

            lock (DynamicConsoleLock)
            {
                Console.SetCursorPosition(left, top);
                Console.Write(value);
                newLeft = Console.CursorLeft;
                if (newLine)
                    Console.WriteLine();
                else
                    Console.SetCursorPosition(oldLeft, oldTop);
            }

            return newLeft;
        }

        /// <summary>
        /// Clear the specified line
        /// </summary>
        /// <param name="top">Line</param>
        public static void ClearLine(int top)
        {
            // You can't use System.Console with xUnit
            if (Console.Title.EndsWith("testhost.exe"))
                return;

            int oldLeft = Console.CursorLeft;
            int oldTop = Console.CursorTop;

            lock (DynamicConsoleLock)
            {
                Console.SetCursorPosition(0, top);
                Console.Write(new string(' ', Console.BufferWidth)); // https://stackoverflow.com/a/15421600/11873025
                Console.SetCursorPosition(oldLeft, oldTop);
            }
        }

        /// <summary>
        ///     Sets the position of the cursor and invoke the given action
        /// </summary>
        /// <param name="value">The value to write.</param>
        /// <param name="left">
        ///     The column position of the cursor. Columns are numbered from left to right starting
        ///     at 0.
        /// </param>
        /// <param name="top">
        ///     The row position of the cursor. Rows are numbered from top to bottom starting
        ///     at 0.
        /// </param>
        /// <return>
        /// The new column position of the cursor after the writing.
        /// </return>
        public static int CustomAction(Action action, int left, int top)
        {
            // You can't use System.Console with xUnit
            if (Console.Title.EndsWith("testhost.exe"))
                return 0;

            int newLeft = 0;

            lock (DynamicConsoleLock)
            {
                Console.SetCursorPosition(left, top);
                action();
                newLeft = Console.CursorLeft;
                //Console.SetCursorPosition(oldLeft, oldTop);
            }

            return newLeft;
        }
    }
}
