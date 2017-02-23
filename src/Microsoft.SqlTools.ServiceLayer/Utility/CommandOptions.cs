//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Globalization;

namespace Microsoft.SqlTools.ServiceLayer.Utility
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
            Locale = string.Empty;

            try
            {
                for (int i = 0; i < args.Length; ++i)
                {
                    string arg = args[i];
                    if (arg.StartsWith("--") || arg.StartsWith("-"))
                    {
                        // Extracting arguments and properties
                        arg = arg.Substring(1).ToLowerInvariant();
                        string argName = arg;
                        string argProperty = "";
                        int splitIndex = arg.IndexOf(' ');
                        if (splitIndex > 0)
                        {
                            argName = arg.Substring(0, splitIndex);
                            argProperty = arg.Substring(splitIndex + 1);
                        }

                        switch (argName)
                        {
                            case "-enable-logging":
                                EnableLogging = true;
                                break;
                            case "-locale":
                                SetLocale(argProperty);
                                break;
                            case "h":
                            case "-help":
                                ShouldExit = true;
                                return;
                            default:
                                ErrorMessage += String.Format("Unknown argument \"{0}\" with property \"{1}\"" + Environment.NewLine, argName, argProperty);
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

        internal string ErrorMessage { get; private set; }


        /// <summary>
        /// Whether diagnostic logging is enabled
        /// </summary>
        public bool EnableLogging { get; private set; }

        /// <summary>
        /// Whether the program should exit immediately. Set to true when the usage is printed.
        /// </summary>
        public bool ShouldExit { get; private set; }

        /// <summary>
        /// The locale our we should instantiate this service in 
        /// </summary>
        public string Locale { get; private set; }

        /// <summary>
        /// Get the usage string describing command-line arguments for the program
        /// </summary>
        public string Usage
        {
            get
            {
                var str = string.Format("{0}" + Environment.NewLine +
                    "Microsoft.SqlTools.ServiceLayer.exe " + Environment.NewLine +
                    "   Options:" + Environment.NewLine +
                    "        [--enable-logging]" + Environment.NewLine +
                    "        [--help]" + Environment.NewLine +
                    "        [--locale **] (default: 'en')" + Environment.NewLine,
                    ErrorMessage);
                return str;
            }
        }

        private void SetLocale(string locale)
        {
            try
            {
                // Creating cultureInfo from our given locale
                CultureInfo language = new CultureInfo(locale);
                Locale = locale;

                // Setting our language globally 
                CultureInfo.CurrentCulture = language;
                CultureInfo.CurrentUICulture = language;

                // Setting our internal SR culture to our global culture
                SR.Culture = CultureInfo.CurrentCulture;
            }
            catch(Exception ex)
            {
                // Warn user of invalid locale, and fall back to english 
                Console.WriteLine(ex);
            }
        }
    }
}
