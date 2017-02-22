using System;

namespace FileAnywhereClient
{
    class Program
    {
        private static Session session;
        static void Main(string[] args)
        {
            string input = String.Empty;

            Console.Write("Enter a command: ");
            while (!(input = Console.ReadLine()).Equals("quit")) // Loop indefinitely
            {
                if (!string.IsNullOrEmpty(input))
                {
                    // Get command from input.
                    string[] inputArr = input.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                    string cmd = inputArr[0];
                    string[] cmdArgs = null;

                    // If there are arguments copy them into cmdArgs for later use.
                    if (inputArr.Length > 1)
                    {
                        cmdArgs = new string[inputArr.Length - 1];
                        Array.Copy(inputArr, 1, cmdArgs, 0, inputArr.Length - 1);
                    }

                    switch (cmd.ToUpper())
                    {
                        case "HELP":
                            DisplayHelp();
                            break;
                        case "LOGIN":
                            // Create a new session (overwriting existing one).
                            session = new Session(RequestInput("Organization ID"),
                                                  RequestInput("Username"),
                                                  Utils.ReadOutPassword());
                            session.Login();
                            break;
                        case "LIST":
                            if (HasValidSession())
                            {
                                session.ListItems();
                            }
                            break;
                        case "CREATEFOLDER":
                            if (HasValidSession())
                            {
                                session.CreateFolder(RequestInput("Location path"), 
                                                     RequestInput("New folder name"));
                            }
                            break;
                        case "UPLOAD":
                            if (HasValidSession())
                            {
                                session.UploadFile(RequestInput("Destination path"),
                                                   RequestInput("Enter path of file to upload"));
                            }
                            break;
                        case "DELETE":
                            if (HasValidSession())
                            {
                                session.DeleteItem(RequestInput("Item to delete"));
                            }
                            break;
                        default:
                            Console.WriteLine(Environment.NewLine +
                                              "Invalid command entered. Please enter a valid command or enter HELP for a list" +
                                              " of commands.");
                            break;
                    }
                }
                else
                {
                    Console.Write("\r\nNo command input.");
                }

                Console.Write("\r\nEnter a command: ");
            }
        }

        /// <summary>
        /// Requests information from the user in the console.
        /// </summary>
        /// <param name="request">What to print to user.</param>
        /// <returns>User input to request.</returns>
        private static string RequestInput(string request)
        {
            Console.Write(request + ": ");
            return Console.ReadLine();
        }

        /// <summary>
        /// Checks if session exists and is valid (meaing it contains token).
        /// </summary>
        /// <returns>If the current session is valid.</returns>
        private static bool HasValidSession()
        {
            bool isSessionValid = (session != null) && session.IsValid();
            if (isSessionValid)
            {
                return true;
            }
            else
            {
                Console.WriteLine("\r\nNot logged in. Please log in before using this command.");
                return false;
            }
        }

        /// <summary>
        /// Displays list of commands and usage.
        /// </summary>
        static void DisplayHelp()
        {
            Console.WriteLine(Environment.NewLine + "Commands: ");
            Console.WriteLine("- {0,-50} | {1}", "createfolder", "creates a new folder at designated path.");
            Console.WriteLine("- {0,-50} | {1}", "delete", "deletes a file or folder.");
            Console.WriteLine("- {0,-50} | {1}", "login", "logs into specified account.");
            Console.WriteLine("- {0,-50} | {1}", "list", "lists all files and folders in volume.");
            Console.WriteLine("- {0,-50} | {1}", "upload", "uploads file to specified path.");
        }
    }
}
