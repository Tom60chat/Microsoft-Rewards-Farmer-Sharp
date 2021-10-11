using System;
using System.IO;

namespace MicrosoftRewardsFarmer
{
    public static class ColoredConsole
    {
        //https://stackoverflow.com/a/54123238
        /*public static void Write(string msg)
        {
            string[] ss = msg.Split('{', '}');
            ConsoleColor c;
            foreach (var s in ss)
                if (s.StartsWith("/"))
                    Console.ResetColor();
                else if (s.StartsWith("=") && Enum.TryParse(s.Substring(1), out c))
                    Console.ForegroundColor = c;
                else
                    Console.Write(s);
        }*/

        /// <summary>
        /// Parse text en write into the app console.
        /// </summary>
        /// <example>
        /// ColoredConsole.WriteLigne($"<$Blue;this text is blue> and this one is <$Orange;orange!>");
        /// </example>
        /// <param name="msg">Messeage to write</param>
        public static void Write(string msg)
        {
            int ch;

            using (StringReader reader = new StringReader(msg))
            {
                while ((ch = reader.Read()) > 0)
                {
                    // < ()
                    if (ch == '<')
                    {
                        // <$ ()
                        if (reader.Read() == '$')
                        {
                            StringWriter color = new StringWriter();

                            while ((ch = reader.Read()) > 0)
                            {
                                if (ch == ';')
                                    break;

                                color.Write((char)ch);
                            }

                            // <$Color, ()

                            if (Enum.TryParse(color.ToString(), out ConsoleColor clr))
                                Console.ForegroundColor = clr;

                            while ((ch = reader.Read()) > 0)
                            {
                                if (ch == '>')
                                    break;
                                Console.Write((char)ch);
                            }

                            // <$Color, message> (message)

                            Console.ResetColor();
                        }
                    }
                    else
                    {
                        Console.Write((char)ch);
                    }
                }
            };
        }

        /// <summary>
        /// Parse text en write into the app console.
        /// </summary>
        /// <example>
        /// ColoredConsole.WriteLigne($"<$Blue;this text is blue> and this one is <$Orange;orange!>");
        /// </example>
        /// <param name="msg">Messeage to write</param>
        public static void WriteLigne(string msg)
        {
            Write(msg);
            Console.WriteLine();
        }
    }
}
