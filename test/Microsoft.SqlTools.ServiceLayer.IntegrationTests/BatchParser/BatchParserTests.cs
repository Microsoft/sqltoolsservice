//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.SqlTools.ServiceLayer.BatchParser;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Microsoft.SqlTools.ServiceLayer.Test.Common.Baselined;
using Xunit;
using Microsoft.SqlTools.ServiceLayer.QueryExecution;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.BatchParser
{
    public class BatchParserTests : BaselinedTest
    {
        private bool testFailed = false;

        public BatchParserTests()
        {
            InitializeTest();
        }

        public void InitializeTest()
        {
            CategoryName = "BatchParser";
            this.TraceOutputDirectory = RunEnvironmentInfo.GetTraceOutputLocation();
            TestInitialize();
        }

        [Fact]
        public void VerifyThrowOnUnresolvedVariable()
        {
            string script = "print '$(NotDefined)'";
            StringBuilder output = new StringBuilder();

            TestCommandHandler handler = new TestCommandHandler(output);
            IVariableResolver resolver = new TestVariableResolver(new StringBuilder());
            Parser p = new Parser(
                handler,
                resolver,
                new StringReader(script),
                "test");
            p.ThrowOnUnresolvedVariable = true;

            handler.SetParser(p);

            Assert.Throws<BatchParserException>(() => p.Parse());
        }

        public static Stream GenerateStreamFromString(string s)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        public void TokenizeWithLexer(string filename, StringBuilder output)
        {

            //var inputFile = File.Open(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
            string input = File.ReadAllText(filename).Replace("\r\n", "\n");
            var inputStream = GenerateStreamFromString(input);
            using (Lexer lexer = new Lexer(new StreamReader(inputStream), filename))
            {
                
                string inputText = File.ReadAllText(filename);
                inputText = inputText.Replace("\r\n", "\n");
                StringBuilder roundtripTextBuilder = new StringBuilder();
                StringBuilder outputBuilder = new StringBuilder();
                StringBuilder tokenizedInput = new StringBuilder();
                bool lexerError = false;

                Token token = null;
                try
                {
                    do
                    {
                        lexer.ConsumeToken();
                        token = lexer.CurrentToken;            
                        roundtripTextBuilder.Append(token.Text.Replace("\r\n", "\n"));
                        outputBuilder.AppendLine(GetTokenString(token));
                        tokenizedInput.Append('[').Append(GetTokenCode(token.TokenType)).Append(':').Append(token.Text.Replace("\r\n", "\n")).Append(']');
                    } while (token.TokenType != LexerTokenType.Eof);
                }
                catch (BatchParserException ex)
                {
                    lexerError = true;
                    outputBuilder.AppendLine(string.Format(CultureInfo.CurrentCulture, "[ERROR: code {0} at {1} - {2} in {3}, message: {4}]", ex.ErrorCode, GetPositionString(ex.Begin), GetPositionString(ex.End), GetFilenameOnly(ex.Begin.Filename), ex.Message));
                }
                output.AppendLine("Lexer tokenized input:");
                output.AppendLine("======================");
                output.AppendLine(tokenizedInput.ToString());
                output.AppendLine("Tokens:");
                output.AppendLine("=======");
                output.AppendLine(outputBuilder.ToString());

                if (lexerError == false)
                {
                    // Verify that all text from tokens can be recombined into original string
                    Assert.Equal<string>(inputText, roundtripTextBuilder.ToString());
                }
            }
        }

        private string GetTokenCode(LexerTokenType lexerTokenType)
        {
            switch (lexerTokenType)
            {
                case LexerTokenType.Text:
                    return "T";
                case LexerTokenType.Whitespace:
                    return "WS";
                case LexerTokenType.NewLine:
                    return "NL";
                case LexerTokenType.Comment:
                    return "C";
                default:
                    return lexerTokenType.ToString();
            }
        }

        static void CopyToOutput(string sourceDirectory, string filename)
        {
            File.Copy(Path.Combine(sourceDirectory, filename), filename, true);
            FileUtilities.SetFileReadWrite(filename);
        }

        [Fact]
        public void BatchParserTest()
        {
            CopyToOutput(FilesLocation, "TS-err-cycle1.txt");
            CopyToOutput(FilesLocation, "cycle2.txt");

            Start("err-blockComment");
            Start("err-blockComment2");
            Start("err-varDefinition");
            Start("err-varDefinition2");
            Start("err-varDefinition3");
            Start("err-varDefinition4");
            Start("err-varDefinition5");
            Start("err-varDefinition6");
            Start("err-varDefinition7");
            Start("err-varDefinition8");
            Start("err-varDefinition9");
            Start("err-variableRef");
            Start("err-variableRef2");
            Start("err-variableRef3");
            Start("err-variableRef4");
            Start("err-cycle1");
            Start("input");
            Start("input2");
            Start("pass-blockComment");
            Start("pass-lineComment");
            Start("pass-lineComment2");
            Start("pass-noBlockComments");
            Start("pass-noLineComments");
            Start("pass-varDefinition");
            Start("pass-varDefinition2");
            Start("pass-varDefinition3");
            Start("pass-varDefinition4");
            Start("pass-command-and-comment");
            Assert.False(testFailed, "At least one of test cases failed. Check output for details.");
        }

        public void TestParser(string filename, StringBuilder output)
        {
            try
            {
                TestCommandHandler commandHandler = new TestCommandHandler(output);

                Parser parser = new Parser(
                    commandHandler,
                    new TestVariableResolver(output),
                    new StreamReader(File.Open(filename, FileMode.Open)),
                    filename);

                commandHandler.SetParser(parser);

                parser.Parse();
            }
            catch (BatchParserException ex)
            {
                output.AppendLine(string.Format(CultureInfo.CurrentCulture, "[PARSER ERROR: code {0} at {1} - {2} in {3}, token text: {4}, message: {5}]", ex.ErrorCode, GetPositionString(ex.Begin), GetPositionString(ex.End), GetFilenameOnly(ex.Begin.Filename), ex.Text, ex.Message));
            }
        }

        private string GetPositionString(PositionStruct pos)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}:{1} [{2}]", pos.Line, pos.Column, pos.Offset);
        }

        private string GetTokenString(Token token)
        {
            if (token == null)
            {
                return "(null)";
            }
            else
            {
                string tokenText = token.Text;
                if (tokenText != null)
                {
                    tokenText = tokenText.Replace("\r\n", "\\n").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
                }
                string tokenFilename = token.Filename;
                tokenFilename = GetFilenameOnly(tokenFilename);
                return string.Format(CultureInfo.CurrentCulture, "[Token {0} at {1}({2}:{3} [{4}] - {5}:{6} [{7}]): '{8}']", 
                    token.TokenType, 
                    tokenFilename,
                    token.Begin.Line, token.Begin.Column, token.Begin.Offset,
                    token.End.Line, token.End.Column, token.End.Offset, 
                    tokenText);
            }
        }

        internal static string GetFilenameOnly(string fullPath)
        {
            return fullPath != null ? Path.GetFileName(fullPath) : null;
        }
        
        public override void Run()
        {
            string inputFilename = GetTestscriptFilePath(CurrentTestName);
            StringBuilder output = new StringBuilder();

            TokenizeWithLexer(inputFilename, output);
            TestParser(inputFilename, output);

            string baselineFilename = GetBaselineFilePath(CurrentTestName);
            string baseline;

            try
            {
                baseline = GetFileContent(baselineFilename).Replace("\r\n", "\n");
            }
            catch (FileNotFoundException)
            {
                baseline = string.Empty;
            }

            string outputString = output.ToString().Replace("\r\n", "\n");

            Console.WriteLine(baselineFilename);

            if (string.Compare(baseline, outputString, StringComparison.Ordinal) != 0)
            {
                Console.WriteLine("baseline:" + "\n" + baseline);
                Console.WriteLine("-------------------");
                Console.Write("outputString:" + "\n" + outputString);
                DumpToTrace(CurrentTestName, outputString);
                string outputFilename = Path.Combine(TraceFilePath, GetBaselineFileName(CurrentTestName));
                Console.WriteLine(":: Output does not match the baseline!");
                Console.WriteLine("code --diff \"" + baselineFilename + "\" \"" + outputFilename + "\"");
                Console.WriteLine();
                Console.WriteLine(":: To update the baseline:");
                Console.WriteLine("copy \"" + outputFilename + "\" \"" + baselineFilename + "\"");
                Console.WriteLine();
                testFailed = true;
            }
        }
    }
}
