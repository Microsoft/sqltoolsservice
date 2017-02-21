﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.Formatter;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.Test.Formatter
{
    public class GeneralFormatterTests : FormatterUnitTestsBase
    {
        [Fact]
        public void KeywordCaseConversionUppercase()
        {
            LoadAndFormatAndCompare("KeywordCaseConversion", 
                GetInputFile("KeywordCaseConversion.sql"),
                GetBaselineFile("KeywordCaseConversion_Uppercase.sql"), 
                new FormatOptions() { KeywordCasing = CasingOptions.Uppercase }, 
                verifyFormat: true);
        }
        [Fact]
        public void KeywordCaseConversionLowercase()
        {
            LoadAndFormatAndCompare("KeywordCaseConversion",
                GetInputFile("KeywordCaseConversion.sql"),
                GetBaselineFile("KeywordCaseConversion_Lowercase.sql"),
                new FormatOptions() { KeywordCasing = CasingOptions.Lowercase },
                verifyFormat: true);
        }
    }
}
