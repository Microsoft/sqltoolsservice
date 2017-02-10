﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Composition;
using Microsoft.SqlServer.Management.SqlParser.SqlCodeDom;
using Microsoft.SqlTools.ServiceLayer.Utility;

namespace Microsoft.SqlTools.ServiceLayer.Formatter
{

    [Export(typeof(ASTNodeFormatterFactory))]
    internal class SqlQuerySpecificationFormatterFactory : ASTNodeFormatterFactoryT<SqlQuerySpecification>
    {
        protected override ASTNodeFormatter DoCreate(FormatterVisitor visitor, SqlQuerySpecification codeObject)
        {
            return new SqlQuerySpecificationFormatter(visitor, codeObject);
        }
    }

    internal class SqlQuerySpecificationFormatter : ASTNodeFormatterT<SqlQuerySpecification>
    {
        WhiteSpaceSeparatedListFormatter WhiteSpaceSeparatedListFormatter { get; set; }

        internal SqlQuerySpecificationFormatter(FormatterVisitor visitor, SqlQuerySpecification codeObject)
            : base(visitor, codeObject)
        {
            this.WhiteSpaceSeparatedListFormatter = new NewLineSeparatedListFormatter(visitor, codeObject, false);
        }

        internal override void ProcessChild(SqlCodeObject child)
        {
            Validate.IsNotNull(nameof(child), child);
            this.WhiteSpaceSeparatedListFormatter.ProcessChild(child);
        }

        internal override void ProcessPrefixRegion(int startTokenNumber, int firstChildStartTokenNumber)
        {
            this.WhiteSpaceSeparatedListFormatter.ProcessPrefixRegion(startTokenNumber, firstChildStartTokenNumber);
        }

        internal override void ProcessSuffixRegion(int lastChildEndTokenNumber, int endTokenNumber)
        {
            this.WhiteSpaceSeparatedListFormatter.ProcessSuffixRegion(lastChildEndTokenNumber, endTokenNumber);
        }

        internal override void ProcessInterChildRegion(SqlCodeObject previousChild, SqlCodeObject nextChild)
        {
            Validate.IsNotNull(nameof(previousChild), previousChild);
            Validate.IsNotNull(nameof(nextChild), nextChild);

            this.WhiteSpaceSeparatedListFormatter.ProcessInterChildRegion(previousChild, nextChild);
        }

    }
}
