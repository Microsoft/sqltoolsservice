﻿using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Moq;
using System;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Xml;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.UnitTests.QueryExecution.DataStorage
{
    public class SaveAsExcelFileStreamWriterHelperTests : IDisposable
    {
        private Stream _stream;
        public SaveAsExcelFileStreamWriterHelperTests()
        {
            _stream = new MemoryStream();
            using (var helper = new SaveAsExcelFileStreamWriterHelper(_stream, true))
            using (var sheet = helper.AddSheet())
            {
                DbCellValue value = new DbCellValue();
                sheet.AddRow();

                value.IsNull = true;
                sheet.AddCell(value);

                value.IsNull = false;
                value.RawObject = "";
                sheet.AddCell(value);

                value.RawObject = "test string";
                sheet.AddCell(value);

                value.RawObject = 3;
                sheet.AddCell(value);

                value.RawObject = 3.5;
                sheet.AddCell(value);

                sheet.AddRow();

                value.RawObject = new DateTime(1900, 2, 28);
                sheet.AddCell(value);

                value.RawObject = new DateTime(1900, 3, 1);
                sheet.AddCell(value);

                value.RawObject = new DateTime(1900, 3, 1, 15, 00, 00);
                sheet.AddCell(value);

                value.RawObject = new DateTime(1, 1, 1, 15, 00, 00);
                sheet.AddCell(value);

                value.RawObject = new TimeSpan(15, 00, 00);
                sheet.AddCell(value);
            }
        }
        Regex contentRemoveLinebreakLeadingSpace = new Regex(@"\r?\n\s*");
        private void ContentMatch(string fileName)
        {
            string referencePath = Path.Combine(RunEnvironmentInfo.GetTestDataLocation(),
                "DataStorage",
                "SaveAsExcelFileStreamWriterHelperTests",
                fileName);
            string referenceContent = File.ReadAllText(referencePath);
            referenceContent = contentRemoveLinebreakLeadingSpace.Replace(referenceContent, "");

            using (ZipArchive zip = new ZipArchive(_stream, ZipArchiveMode.Read, true))
            {
                using (var reader = new StreamReader(zip.GetEntry(fileName).Open()))
                {
                    string realContent = reader.ReadToEnd();
                    Assert.Equal(referenceContent, realContent);
                }
            }
        }
        [Fact]
        public void CheckContentType()
        {
            ContentMatch("[Content_Types].xml");
        }
        [Fact]
        public void CheckTopRels()
        {
            ContentMatch("_rels/.rels");
        }
        [Fact]
        public void CheckWorkbookRels()
        {
            ContentMatch("xl/_rels/workbook.xml.rels");
        }
        [Fact]
        public void CheckStyles()
        {
            ContentMatch("xl/styles.xml");
        }
        [Fact]
        public void CheckWorkbook()
        {
            ContentMatch("xl/workbook.xml");
        }
        [Fact]
        public void CheckSheet1()
        {
            ContentMatch("xl/worksheets/sheet1.xml");
        }

        public void Dispose()
        {
            _stream.Dispose();
        }
    }

    public class SaveAsExcelFileStreamWriterHelperReferenceManagerTests
    {
        private Mock<XmlWriter> _xmlWriterMock;
        private string LastWrittenReference { get; set; }
        private int LastWrittenRow { get; set; }

        public SaveAsExcelFileStreamWriterHelperReferenceManagerTests()
        {
            _xmlWriterMock = new Mock<XmlWriter>(MockBehavior.Strict);
            _xmlWriterMock
                .Setup(_xmlWriter => _xmlWriter.WriteChars(
                    It.IsAny<char[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Callback<char[], int, int>((array, index, count) =>
                    LastWrittenReference = new string(array, index, count));
            _xmlWriterMock.Setup(a => a.WriteStartAttribute(null, "r", null));
            _xmlWriterMock.Setup(a => a.WriteEndAttribute());
            _xmlWriterMock.Setup(a => a.WriteValue(It.IsAny<int>()))
                .Callback<int>(row => LastWrittenRow = row);

        }

        [Fact]
        public void ReferenceA1()
        {
            var xmlWriter = _xmlWriterMock.Object;
            var manager = new SaveAsExcelFileStreamWriterHelper.ReferenceManager(xmlWriter);
            manager.WriteAndIncreaseRowReference();
            manager.WriteAndIncreaseColumnReference();
            Assert.Equal("A1", LastWrittenReference);
        }
        [Fact]
        public void ReferenceZ1()
        {
            var xmlWriter = _xmlWriterMock.Object;
            var manager = new SaveAsExcelFileStreamWriterHelper.ReferenceManager(xmlWriter);
            manager.WriteAndIncreaseRowReference();
            for (int i = 0; i < 26 - 1; i++)
            {
                manager.IncreaseColumnReference();
            }
            manager.WriteAndIncreaseColumnReference();
            Assert.Equal("Z1", LastWrittenReference);
            manager.WriteAndIncreaseColumnReference();
            Assert.Equal("AA1", LastWrittenReference);
        }
        [Fact]
        public void ReferenceZZ1()
        {
            var xmlWriter = _xmlWriterMock.Object;
            var manager = new SaveAsExcelFileStreamWriterHelper.ReferenceManager(xmlWriter);
            manager.WriteAndIncreaseRowReference();

            for (int i = 0; i < 27 * 26 - 1; i++)
            {
                manager.IncreaseColumnReference();
            }
            manager.WriteAndIncreaseColumnReference();
            Assert.Equal("ZZ1", LastWrittenReference);
            manager.WriteAndIncreaseColumnReference();
            Assert.Equal("AAA1", LastWrittenReference);
        }
        [Fact]
        public void ReferenceXFD()
        {
            var xmlWriter = _xmlWriterMock.Object;
            var manager = new SaveAsExcelFileStreamWriterHelper.ReferenceManager(xmlWriter);
            manager.WriteAndIncreaseRowReference();

            for (int i = 0; i < 16384 - 1; i++)
            {
                manager.IncreaseColumnReference();
            }
            //The 16384 should be the maximal column and not throw
            manager.AssureColumnReference();
            manager.WriteAndIncreaseColumnReference();
            Assert.Equal("XFD1", LastWrittenReference);
            var ex = Assert.Throws<InvalidOperationException>(
                () => manager.AssureColumnReference());
            Assert.Contains("max column number is 16384", ex.Message);
        }
        [Fact]
        public void ReferenceRowReset()
        {
            var xmlWriter = _xmlWriterMock.Object;
            var manager = new SaveAsExcelFileStreamWriterHelper.ReferenceManager(xmlWriter);
            manager.WriteAndIncreaseRowReference();
            Assert.Equal(1, LastWrittenRow);
            manager.WriteAndIncreaseColumnReference();
            Assert.Equal("A1", LastWrittenReference);
            manager.WriteAndIncreaseColumnReference();
            Assert.Equal("B1", LastWrittenReference);

            //add row should reset column reference 
            manager.WriteAndIncreaseRowReference();
            Assert.Equal(2, LastWrittenRow);
            manager.WriteAndIncreaseColumnReference();
            Assert.Equal("A2", LastWrittenReference);
        }

        [Fact]
        public void AddRowMustBeCalledBeforeAddCellException()
        {
            var xmlWriter = _xmlWriterMock.Object;
            var manager = new SaveAsExcelFileStreamWriterHelper.ReferenceManager(xmlWriter);

            var ex = Assert.Throws<InvalidOperationException>(
                () => manager.WriteAndIncreaseColumnReference());
            Assert.Contains("AddRow must be called before AddCell", ex.Message);
        }

    }
}
