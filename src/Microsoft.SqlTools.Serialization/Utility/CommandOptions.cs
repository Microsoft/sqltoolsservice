﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.Serialization.Utility
{
    /// <summary>
    /// The command-line options helper class.
    /// </summary>
    internal class CommandOptions
    {
        /// <summary>
        /// Construct and parse command line options from the arguments array
        /// </summary>
        public CommandOptions(string[] args)
        {
            ErrorMessage = string.Empty;

            try
            {
                for (int i = 0; i < args.Length; ++i)
                {
                    string arg = args[i];
                    if (arg.StartsWith("--") || arg.StartsWith("-"))
                    {
                        switch (arg)
                        {
                            case "-enable-logging":
                                EnableLogging = true;
                                break;
                            case "h":
                            case "-help":
                                ShouldExit = true;
                                return;
                            default:
                                ErrorMessage += String.Format("Unknown argument \"{0}\"" + Environment.NewLine, arg);
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage += ex.ToString();
                return;
            }
            finally
            {
                if (!string.IsNullOrEmpty(ErrorMessage) || ShouldExit)
                {
                    Console.WriteLine(Usage);
                    ShouldExit = true;
                }
            }
        }

        public string ErrorMessage { get; private set; }

        /// <summary>
        /// Whether diagnostic logging is enabled
        /// </summary>
        public bool EnableLogging { get; private set; }

        /// <summary>
        /// Whether the program should exit immediately. Set to true when the usage is printed.
        /// </summary>
        public bool ShouldExit { get; private set; }

        /// <summary>
        /// Get the usage string describing command-line arguments for the program
        /// </summary>
        public string Usage
        {
            get
            {
                var str = string.Format("{0}" + Environment.NewLine +
                    "Microsoft.SqlTools.Serialization " + Environment.NewLine +
                    "   Options:" + Environment.NewLine +
                    "        [--enable-logging]" + Environment.NewLine +
                    "        [--help]" + Environment.NewLine,
                    ErrorMessage);
                return str;
            }
        }
    }
}
