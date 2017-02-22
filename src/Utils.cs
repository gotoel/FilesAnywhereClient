using System;

namespace FileAnywhereClient
{
    class Utils
    {
        /// <summary>
        /// Reads out password input "securely".
        /// </summary>
        /// <returns>Password</returns>
        public static string ReadOutPassword()
        {
            Console.Write("Password: ");

            string password = String.Empty;
            ConsoleKeyInfo key = new ConsoleKeyInfo();

            while (key.Key != ConsoleKey.Enter) 
            {
                key = Console.ReadKey(true);

                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    password += key.KeyChar;
                    Console.Write("*");
                }
                else
                {
                    if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                    {
                        password = password.Substring(0, (password.Length - 1));
                        Console.Write("\b \b");
                    }
                }
            }

            return password;
        }
    }
}
