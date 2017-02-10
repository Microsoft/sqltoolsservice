﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.IO;

namespace Microsoft.SqlTools.ServiceLayer.Test.Common
{
    public class TestUtilities
    {
        
        public static void CompareTestFiles(FileInfo baselinePath, FileInfo outputPath, int maxDiffLines = -1 /* unlimited */)
        {
            if (!baselinePath.Exists)
            {
                throw new ComparisonFailureException("echo Test Failed:   Baseline file " + baselinePath.FullName + " does not exist" +
                   Environment.NewLine + Environment.NewLine + "echo test > \"" + baselinePath.FullName + "\"" +
                   Environment.NewLine + Environment.NewLine + "tf add \"" + baselinePath.FullName + "\"\r\n");
            }

            if (!outputPath.Exists)
            {
                throw new ComparisonFailureException("Test Failed:  output file " + outputPath.FullName + " doesn't exist.");
            }

            string baseline = ReadTextAndNormalizeLineEndings(baselinePath.FullName);
            string actual = ReadTextAndNormalizeLineEndings(outputPath.FullName);

            if (baseline.CompareTo(actual) != 0)
            {
                string header = "Test Failed:  Baseline file " + baselinePath.FullName + " differs from output file " + outputPath.FullName + "\r\n\r\n";
                string editAndCopyMessage =
                    "\r\n\r\n copy \"" + outputPath.FullName + "\" \"" + baselinePath.FullName + "\"" +
                    "\r\n\r\n";
                string diffCmdMessage =
                    "code --diff \"" + baselinePath.FullName + "\" \"" + outputPath.FullName + "\"" +
                    "\r\n\r\n";
                

                throw new ComparisonFailureException(header + diffCmdMessage + editAndCopyMessage, editAndCopyMessage);
            }
        }

        /// <summary>
        /// Normalizes line endings in a file to facilitate comparisons regardless of OS. On Windows line endings are \r\n, while
        /// on other systems only \n is used
        /// </summary>
        private static string ReadTextAndNormalizeLineEndings(string filePath)
        {
            string text = File.ReadAllText(filePath);
            return text.Replace("\r\n", Environment.NewLine);
        }
    }
}
