﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.SqlTools.ServiceLayer.SchemaCompare;
using Microsoft.SqlTools.ServiceLayer.SchemaCompare.Contracts;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.SchemaCompare
{
    public class SchemaCompareTests
    {
        [Fact]
        public void FormatScriptAddsGo()
        {
            string script = "EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Primary key for AWBuildVersion records.', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'AWBuildVersion', @level2type = N'COLUMN', @level2name = N'SystemInformationID';";
            Assert.DoesNotContain("GO", script);
            string result = SchemaCompareUtils.FormatScript(script);
            Assert.EndsWith("GO", result);
        }

        [Fact]
        public void FormatScriptDoesNotAddGoForNullScripts()
        {
            string script1 = null;
            string result1 = SchemaCompareUtils.FormatScript(script1);
            Assert.DoesNotContain("GO", result1);
            Assert.Equal(null, result1);

            string script2 = "null";
            string result2 = SchemaCompareUtils.FormatScript(script2);
            Assert.DoesNotContain("GO", result2);
        }

        [Fact]
        public void FormatScriptDoesNotAddGoForEmptyStringScripts()
        {
            string script = string.Empty;
            string result = SchemaCompareUtils.FormatScript(script);
            Assert.DoesNotContain("GO", result);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void FormatScriptDoesNotAddGoForWhitespaceStringScripts()
        {
            string script = "    \t\n";
            Assert.True(string.IsNullOrWhiteSpace(script));
            string result = SchemaCompareUtils.FormatScript(script);
            Assert.DoesNotContain("GO", result);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void RemovesExcessWhitespace()
        {
            // leading whitespace
            string script1 = "\r\n   EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Primary key for AWBuildVersion records.', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'AWBuildVersion', @level2type = N'COLUMN', @level2name = N'SystemInformationID';";
            string result1 = SchemaCompareUtils.RemoveExcessWhitespace(script1);
            Assert.False(script1.Equals(result1));
            Assert.False(result1.StartsWith("\r"));
            Assert.True(result1.StartsWith("EXECUTE"));

            // trailing whitespace
            string script2 = "EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = N'Primary key for AWBuildVersion records.', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'AWBuildVersion', @level2type = N'COLUMN', @level2name = N'SystemInformationID';  \n";
            string result2 = SchemaCompareUtils.RemoveExcessWhitespace(script2);
            Assert.False(script2.Equals(result2));
            Assert.False(result2.EndsWith("\n"));
            Assert.True(result2.EndsWith(";"));

            // non-leading/trailing multiple spaces
            string script3 = @"CREATE TABLE [dbo].[AWBuildVersion] (
     [SystemInformationID] TINYINT IDENTITY (1, 1) NOT NULL,
 [Database Version] NVARCHAR (25)     NOT NULL,
 [VersionDate] DATETIME     NOT NULL,
 [ModifiedDate] DATETIME NOT NULL
);";
            string expected3 = @"CREATE TABLE [dbo].[AWBuildVersion] (
 [SystemInformationID] TINYINT IDENTITY (1, 1) NOT NULL,
 [Database Version] NVARCHAR (25) NOT NULL,
 [VersionDate] DATETIME NOT NULL,
 [ModifiedDate] DATETIME NOT NULL
);";
            string result3 = SchemaCompareUtils.RemoveExcessWhitespace(script3);
            Assert.True(expected3.Equals(result3));
        }

        [Fact]
        public void CreateExcludedObjects()
        {
            //successful creation
            SchemaCompareObjectId validObject = new SchemaCompareObjectId
            {
                NameParts = new string[] { "dbo", "Table1" },
                SqlObjectType = "Microsoft.Data.Tools.Schema.Sql.SchemaModel.SqlTable"
            };

            var validResult = SchemaCompareUtils.CreateExcludedObject(validObject);

            Assert.NotNull(validResult);
            Assert.Equal(validObject.SqlObjectType, validResult.TypeName);
            Assert.Equal(validObject.NameParts.Length, validResult.Identifier.Parts.Count);
            for (int i = 0; i < validObject.NameParts.Length; i++)
            {
                Assert.Equal(validObject.NameParts[i], validResult.Identifier.Parts[i]);
            }

            //null creation
            SchemaCompareObjectId object1 = new SchemaCompareObjectId
            {
                NameParts = null, //null caused by this value
                SqlObjectType = "Microsoft.Data.Tools.Schema.Sql.SchemaModel.SqlTable"
            };

            var nullResult1 = SchemaCompareUtils.CreateExcludedObject(object1);
            Assert.Null(nullResult1);

            //null creation due to argumentException
            SchemaCompareObjectId object2 = new SchemaCompareObjectId
            {
                NameParts = new string[] { "dbo", "Table1" },
                SqlObjectType = "SqlTable" // unll caused by this value
            };

            var nullResult2 = SchemaCompareUtils.CreateExcludedObject(object2);
            Assert.Null(nullResult2);
        }
    }
}
