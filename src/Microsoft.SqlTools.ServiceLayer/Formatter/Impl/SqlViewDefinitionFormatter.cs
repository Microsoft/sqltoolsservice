﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


using System;
using System.Collections.Generic;
using System.Composition;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Babel.ParserGenerator;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;

namespace Microsoft.SqlTools.ServiceLayer.Formatter
{

    [Export(typeof(ASTNodeFormatterFactory))]
    internal class SqlViewDefinitionFormatterFactory : ASTNodeFormatterFactoryT<SqlViewDefinition>
    {
        protected override ASTNodeFormatter DoCreate(FormatterVisitor visitor, SqlViewDefinition codeObject)
        {
            return new SqlViewDefinitionFormatter(visitor, codeObject);
        }
    }

    class SqlViewDefinitionFormatter : ASTNodeFormatterT<SqlViewDefinition>
    {
        internal CommaSeparatedListFormatter CommaSeparatedList { get; set; }

        internal SqlViewDefinitionFormatter(FormatterVisitor visitor, SqlViewDefinition sqlCodeObject)
            : base(visitor, sqlCodeObject)
        {
            CommaSeparatedList = new CommaSeparatedListFormatter(Visitor, CodeObject, true);
        }

        public override void Format()
        {
            LexLocation loc = CodeObject.Position;

            SqlCodeObject firstChild = CodeObject.Children.FirstOrDefault();
            if (firstChild != null)
            {
                //
                // format the text from the start of the object to the start of its first child
                //
                LexLocation firstChildStart = firstChild.Position;
                ProcessPrefixRegion(loc.startTokenNumber, firstChildStart.startTokenNumber);

                ProcessChild(firstChild);

                // keep track of the next token to process
                int nextToken = firstChildStart.endTokenNumber;

                // process the columns if available
                nextToken = ProcessColumns(nextToken);

                // process options if available
                nextToken = ProcessOptions(nextToken);

                // process the region containing the AS token
                nextToken = ProcessAsToken(nextToken);

                // process the query with clause if present
                nextToken = ProcessQueryWithClause(nextToken);

                // process the query expression
                nextToken = ProcessQueryExpression(nextToken);

                DecrementIndentLevel();

                // format text from end of last child to end of object.
                SqlCodeObject lastChild = CodeObject.Children.LastOrDefault();
                Debug.Assert(lastChild != null, "last child is null.  Need to write code to deal with this case");
                ProcessSuffixRegion(lastChild.Position.endTokenNumber, loc.endTokenNumber);
            }
            else
            {
                // no children
                Visitor.Context.ProcessTokenRange(loc.startTokenNumber, loc.endTokenNumber);
            }
        }

        private int ProcessColumns(int nextToken)
        {
            if (CodeObject.ColumnList != null && CodeObject.ColumnList.Count > 0)
            {
                #region Find the open parenthesis
                int openParenIndex = nextToken;

                TokenData td = TokenManager.TokenList[openParenIndex];
                while (td.TokenId != 40 && openParenIndex < CodeObject.Position.endTokenNumber)
                {
                    DebugAssertTokenIsWhitespaceOrComment(td, openParenIndex);
                    ++openParenIndex;
                    td = TokenManager.TokenList[openParenIndex];
                }
                Debug.Assert(openParenIndex < CodeObject.Position.endTokenNumber, "No open parenthesis in the columns definition.");
                #endregion // Find the open parenthesis


                // Process tokens before the open parenthesis
                ProcessAndNormalizeTokenRange(nextToken, openParenIndex, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
                              

                #region Process open parenthesis
                // if there was no whitespace before the parenthesis to be converted into a newline, then append a newline
                if (nextToken >= openParenIndex
                    || !TokenManager.IsTokenWhitespace(TokenManager.TokenList[openParenIndex - 1].TokenId))
                {
                    td = TokenManager.TokenList[openParenIndex];
                    AddIndentedNewLineReplacement(td.StartIndex);
                }
                Visitor.Context.ProcessTokenRange(openParenIndex, openParenIndex + 1);
                IncrementIndentLevel();

                nextToken = openParenIndex + 1;
                Debug.Assert(nextToken < CodeObject.Position.endTokenNumber, "Unexpected end of View Definition after open parenthesis in the columns definition.");

                // Ensure a newline after the open parenthesis
                if (!TokenManager.IsTokenWhitespace(TokenManager.TokenList[nextToken].TokenId))
                {
                    td = TokenManager.TokenList[nextToken];
                    AddIndentedNewLineReplacement(td.StartIndex);
                }
                #endregion // Process open parenthesis
                
                // find where the columns start
                IEnumerator<SqlIdentifier> columnEnum = CodeObject.ColumnList.GetEnumerator();
                if (columnEnum.MoveNext())
                {
                    ProcessAndNormalizeTokenRange(nextToken, columnEnum.Current.Position.startTokenNumber,
                        FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);

                    ProcessChild(columnEnum.Current);
                    SqlIdentifier previousColumn = columnEnum.Current;
                    while (columnEnum.MoveNext())
                    {
                        CommaSeparatedList.ProcessInterChildRegion(previousColumn, columnEnum.Current);
                        ProcessChild(columnEnum.Current);
                        previousColumn = columnEnum.Current;
                    }
                    nextToken = previousColumn.Position.endTokenNumber;
                }

                #region Find closed parenthesis
                int closedParenIndex = nextToken;
                td = TokenManager.TokenList[closedParenIndex];
                while (td.TokenId != 41 && closedParenIndex < CodeObject.Position.endTokenNumber)
                {
                    Debug.Assert(
                        TokenManager.IsTokenComment(td.TokenId)
                     || TokenManager.IsTokenWhitespace(td.TokenId)
                     , string.Format(CultureInfo.CurrentCulture, "Unexpected token \"{0}\" before the closed parenthesis.", Visitor.Context.GetTokenRangeAsOriginalString(closedParenIndex, closedParenIndex + 1))
                     );
                    ++closedParenIndex;
                    td = TokenManager.TokenList[closedParenIndex];
                }
                Debug.Assert(closedParenIndex < CodeObject.Position.endTokenNumber, "No closing parenthesis after the columns definition.");
                #endregion // Find closed parenthesis

                #region Process region between columns and the closed parenthesis
                for (int i = nextToken; i < closedParenIndex - 1; i++)
                {
                    Debug.Assert(
                        TokenManager.IsTokenComment(TokenManager.TokenList[i].TokenId)
                     || TokenManager.IsTokenWhitespace(TokenManager.TokenList[i].TokenId)
                     , string.Format(CultureInfo.CurrentCulture, "Unexpected token \"{0}\" before the closed parenthesis.", Visitor.Context.GetTokenRangeAsOriginalString(i, i + 1))
                     );
                    SimpleProcessToken(i, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
                }
                DecrementIndentLevel();
                if (nextToken < closedParenIndex)
                {
                    SimpleProcessToken(closedParenIndex - 1, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
                }

                // Ensure a newline before the closing parenthesis

                td = TokenManager.TokenList[closedParenIndex - 1];
                if (!TokenManager.IsTokenWhitespace(td.TokenId))
                {
                    td = TokenManager.TokenList[closedParenIndex];
                    AddIndentedNewLineReplacement(td.StartIndex);
                }
                #endregion // Process region between columns and the closed parenthesis

                #region Process closed parenthesis
                Visitor.Context.ProcessTokenRange(closedParenIndex, closedParenIndex + 1);
                nextToken = closedParenIndex + 1;
                #endregion // Process closed parenthesis
            }
            return nextToken;
        }

        private int ProcessOptions(int nextToken)
        {
            if (CodeObject.Options != null && CodeObject.Options.Count > 0)
            {
                #region Find the "WITH" token
                int withTokenIndex = nextToken;
                TokenData td = TokenManager.TokenList[withTokenIndex];
                while (td.TokenId != FormatterTokens.TOKEN_WITH && withTokenIndex < CodeObject.Position.endTokenNumber)
                {
                    Debug.Assert(
                        TokenManager.IsTokenComment(td.TokenId)
                     || TokenManager.IsTokenWhitespace(td.TokenId)
                     , string.Format(CultureInfo.CurrentCulture, "Unexpected token \"{0}\" before the WITH token.", Visitor.Context.GetTokenRangeAsOriginalString(withTokenIndex, withTokenIndex + 1))
                     );
                    ++withTokenIndex;
                    td = TokenManager.TokenList[withTokenIndex];
                }
                Debug.Assert(withTokenIndex < CodeObject.Position.endTokenNumber , "No WITH token in the options definition.");
                #endregion // Find the "WITH" token

                #region Process the tokens before "WITH"
                for (int i = nextToken; i < withTokenIndex; i++)
                {
                    Debug.Assert(
                        TokenManager.IsTokenComment(TokenManager.TokenList[i].TokenId)
                     || TokenManager.IsTokenWhitespace(TokenManager.TokenList[i].TokenId)
                     , string.Format(CultureInfo.CurrentCulture, "Unexpected token \"{0}\" before the WITH token.", Visitor.Context.GetTokenRangeAsOriginalString(i, i + 1))
                     );
                    SimpleProcessToken(i, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
                }
                #endregion // Process the tokens before "WITH"

                #region Process "WITH"
                if (nextToken >= withTokenIndex
                    || !TokenManager.IsTokenWhitespace(TokenManager.TokenList[withTokenIndex - 1].TokenId))
                {
                    td = TokenManager.TokenList[withTokenIndex];
                    AddIndentedNewLineReplacement(td.StartIndex);
                }
                Visitor.Context.ProcessTokenRange(withTokenIndex, withTokenIndex + 1);
                IncrementIndentLevel();

                nextToken = withTokenIndex + 1;
                Debug.Assert(nextToken < CodeObject.Position.endTokenNumber, "View definition ends unexpectedly after the WITH token.");

                // Ensure a whitespace after the "WITH" token
                if (!TokenManager.IsTokenWhitespace(TokenManager.TokenList[nextToken].TokenId))
                {
                    td = TokenManager.TokenList[nextToken];
                    AddIndentedNewLineReplacement(td.StartIndex);
                }
                #endregion // Process "WITH"
                
                // find where the options start
                IEnumerator<SqlModuleOption> optionEnum = CodeObject.Options.GetEnumerator();
                if (optionEnum.MoveNext())
                {
                    ProcessAndNormalizeTokenRange(nextToken, optionEnum.Current.Position.startTokenNumber,
                        FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
                    
                    // Process options
                    ProcessChild(optionEnum.Current);
                    SqlModuleOption previousOption = optionEnum.Current;
                    while (optionEnum.MoveNext())
                    {
                        CommaSeparatedList.ProcessInterChildRegion(previousOption, optionEnum.Current);
                        ProcessChild(optionEnum.Current);
                        previousOption = optionEnum.Current;
                    }
                    nextToken = previousOption.Position.endTokenNumber;
                }
                DecrementIndentLevel();
            }
            return nextToken;
        }

        private int ProcessAsToken(int nextToken)
        {
            #region Find the "AS" token
            int asTokenIndex = nextToken;
            TokenData td = TokenManager.TokenList[asTokenIndex];
            while (td.TokenId != FormatterTokens.TOKEN_AS && asTokenIndex < CodeObject.Position.endTokenNumber)
            {
                Debug.Assert(
                        TokenManager.IsTokenComment(td.TokenId)
                     || TokenManager.IsTokenWhitespace(td.TokenId)
                     , string.Format(CultureInfo.CurrentCulture, "Unexpected token \"{0}\" before the AS token.", Visitor.Context.GetTokenRangeAsOriginalString(asTokenIndex, asTokenIndex + 1))
                     );
                ++asTokenIndex;
                td = TokenManager.TokenList[asTokenIndex];
            }
            Debug.Assert(asTokenIndex < CodeObject.Position.endTokenNumber, "No AS token.");
            #endregion // Find the "AS" token

            #region Process the tokens before the "AS" token
            for (int i = nextToken; i < asTokenIndex; i++)
            {
                Debug.Assert(
                        TokenManager.IsTokenComment(TokenManager.TokenList[i].TokenId)
                     || TokenManager.IsTokenWhitespace(TokenManager.TokenList[i].TokenId)
                     , string.Format(CultureInfo.CurrentCulture, "Unexpected token \"{0}\" before the AS token.", Visitor.Context.GetTokenRangeAsOriginalString(i, i + 1))
                     );
                SimpleProcessToken(i, FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
            }
            #endregion // Process the tokens before the "AS" token

            #region Process the "AS" token
            if (nextToken >= asTokenIndex
                || !TokenManager.IsTokenWhitespace(TokenManager.TokenList[asTokenIndex - 1].TokenId))
            {
                td = TokenManager.TokenList[asTokenIndex];
                AddIndentedNewLineReplacement(td.StartIndex);
            }
            Visitor.Context.ProcessTokenRange(asTokenIndex, asTokenIndex + 1);
            IncrementIndentLevel();

            nextToken = asTokenIndex + 1;
            Debug.Assert(nextToken < CodeObject.Position.endTokenNumber, "View Definition ends unexpectedly after the AS token.");
            // Ensure a newline after the "AS" token
            if (!TokenManager.IsTokenWhitespace(TokenManager.TokenList[nextToken].TokenId))
            {
                td = TokenManager.TokenList[nextToken];
                AddIndentedNewLineReplacement(td.StartIndex);
            }
            #endregion // Process the "AS" token
            return nextToken;
        }

        private int ProcessQueryWithClause(int nextToken)
        {
            return ProcessQuerySection(nextToken, CodeObject.QueryWithClause);
        }

        private int ProcessQueryExpression(int nextToken)
        {
            return ProcessQuerySection(nextToken, CodeObject.QueryExpression);
        }
        
        /// <summary>
        /// processes any section in a query, since the basic behavior is constant
        /// </summary>
        private int ProcessQuerySection(int nextToken, SqlCodeObject queryObject)
        {
            if (queryObject != null)
            {
                ProcessAndNormalizeTokenRange(nextToken, queryObject.Position.startTokenNumber,
                    FormatterUtilities.NormalizeNewLinesEnsureOneNewLineMinimum);
                ProcessChild(queryObject);
                nextToken = queryObject.Position.endTokenNumber;
            }
            return nextToken;
        }  
    }
}
