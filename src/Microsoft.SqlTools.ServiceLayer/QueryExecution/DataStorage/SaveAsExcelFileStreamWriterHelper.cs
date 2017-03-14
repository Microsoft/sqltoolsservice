﻿/**
 * A simple library to write xlsx file directly. It tries to be minimal, 
 * both in implementation and runtime allocation.
 * 
 * A xlsx file is a zip with specific file structure.
 * http://www.ecma-international.org/publications/standards/Ecma-376.htm
 * 
 * The page number is in the 
 * ECMA-376, Fifth Edition, Part 1 - Fundamentals And Markup Language Reference 
 * 
 * Page 75
 * /
 * |- [Content_Types].xml
 * |- _rels
 *    |- .rels
 * |- xl
 *    |- workbook.xml
 *    |- styles.xml
 *    |- _rels
 *       |- workbook.xml.rels
 *    |- worksheets
 *       |- sheet1.xml
 * 
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Xml;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    public sealed class SaveAsExcelFileStreamWriterHelper : IDisposable
    {
        public sealed class ExcelSheet : IDisposable
        {
            /// <summary>
            /// Start a new row
            /// </summary>
            public void AddRow()
            {
                EndRowIfNeeded();

                _referenceManager.AssureRowReference();

                _writer.WriteStartElement("row");
                _referenceManager.WriteAndIncreaseRowReference();
            }

            /// <summary>
            /// Write a string cell
            /// </summary>
            /// This only increases the internal bookmark and doesn't arcturally write out anything.
            public void AddCellEmpty()
            {
                _referenceManager.IncreaseColumnReference();
            }
            /// <summary>
            /// Write a string cell
            /// </summary>
            /// <param name="value"></param>
            public void AddCell(string value)
            {
                _referenceManager.AssureColumnReference();
                if (value == null)
                {
                    AddCellEmpty();
                    return;
                }

                _writer.WriteStartElement("c");

                _referenceManager.WriteAndIncreaseColumnReference();

                _writer.WriteAttributeString("t", "inlineStr");

                _writer.WriteStartElement("is");
                _writer.WriteStartElement("t");
                _writer.WriteValue(value);
                _writer.WriteEndElement();
                _writer.WriteEndElement();

                _writer.WriteEndElement();
            }
            /// <summary>
            /// Write a DateTime cell.
            /// </summary>
            /// <param name="dateTime">Datetime</param>
            /// <remark>
            /// If the DateTime do not have date part, it will be written as datetime and show as time only
            /// If the DateTime is before 1900-03-01, save as string because excel doesn't support them.
            /// Otherwise, save as datetime, and if the time is 00:00:00, show as yyyy-MM-dd.
            /// Show the datetime as yyyy-MM-dd HH:mm:ss if none of the previous situations
            /// </remark>
            public void AddCell(DateTime dateTime)
            {
                _referenceManager.AssureColumnReference();
                long ticks = dateTime.Ticks;
                Style style = Style.DateTime;
                double excelDate;
                if (ticks < TicksPerDay) //date empty, time only
                {
                    style = Style.Time;
                    excelDate = ((double)ticks) / (double)TicksPerDay;
                }
                else if (ticks < ExcelDateCutoffTick) //before excel cut-off, use string
                {
                    AddCell(dateTime.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
                    return;
                }
                else
                {
                    if (ticks % TicksPerDay == 0) //time empty, date only
                    {
                        style = Style.Date;
                    }
                    excelDate = ((double)(ticks - ExcelEpochTick)) / (double)TicksPerDay;
                }
                AddCellDateTimeInternal(excelDate, style);
            }

            /// <summary>
            /// Write a object cell
            /// </summary>
            /// The program will try to output number/datetime, otherwise, call the ToString 
            /// <param name="o"></param>
            public void AddCell(object o)
            {
                if (o == null)
                {
                    AddCellEmpty();
                    return;
                }
                switch (Type.GetTypeCode(o.GetType()))
                {
                    case TypeCode.Byte:
                    case TypeCode.SByte:
                    case TypeCode.UInt16:
                    case TypeCode.UInt32:
                    case TypeCode.UInt64:
                    case TypeCode.Int16:
                    case TypeCode.Int32:
                    case TypeCode.Int64:
                    case TypeCode.Decimal:
                    case TypeCode.Double:
                    case TypeCode.Single:
                        AddCellBoxedNumber(o);
                        return;
                    case TypeCode.DateTime:
                        AddCell((DateTime)o);
                        return;
                    default:
                        AddCell(o.ToString());
                        return;
                }
            }

            private void AddCellBoxedNumber(object number)
            {
                _referenceManager.AssureColumnReference();

                _writer.WriteStartElement("c");

                _referenceManager.WriteAndIncreaseColumnReference();

                _writer.WriteStartElement("v");
                _writer.WriteValue(number);
                _writer.WriteEndElement();

                _writer.WriteEndElement();
            }

            // The excel epoch is 1/1/1900, but it has 1/0/1900 and 2/29/1900
            // which is equal to set the epoch back two days to 12/30/1899
            // new DateTime(1899,12,30).Ticks
            private const long ExcelEpochTick = 599264352000000000L;

            // Excel can not use date before 1/0/1900 and
            // date before 3/1/1900 is wrong, off by 1 because of 2/29/1900
            // thus, for any date before 3/1/1900, use string for date
            // new DateTime(1900,3,1).Ticks
            private const long ExcelDateCutoffTick = 599317056000000000L;

            // new TimeSpan(24,0,0).Ticks
            private const long TicksPerDay = 864000000000L;

            // datetime need <c r="A1" s="2"><v>26012.451</v></c>
            private void AddCellDateTimeInternal(double excelDate, Style style)
            {
                _writer.WriteStartElement("c");

                _referenceManager.WriteAndIncreaseColumnReference();

                _writer.WriteStartAttribute("s");
                _writer.WriteValue((int)style);
                _writer.WriteEndAttribute();

                _writer.WriteStartElement("v");
                _writer.WriteValue(excelDate);
                _writer.WriteEndElement();

                _writer.WriteEndElement();
            }

            private void EndRowIfNeeded()
            {
                if (_referenceManager.PenddingRowEndTag()) //there are previous rows
                {
                    _writer.WriteEndElement();
                }
            }

            internal ExcelSheet(XmlWriter writer)
            {
                _writer = writer;
                writer.WriteStartDocument();
                writer.WriteStartElement("worksheet", "http://schemas.openxmlformats.org/spreadsheetml/2006/main");
                writer.WriteAttributeString("xmlns", "r", null, "http://schemas.openxmlformats.org/officeDocument/2006/relationships");
                writer.WriteStartElement("sheetData");
                _referenceManager = new ReferenceManager(writer);
            }

            public void Dispose()
            {
                EndRowIfNeeded();
                _writer.WriteEndElement(); // sheetData 
                _writer.WriteEndElement(); // worksheet 
                _writer.Dispose();
            }

            private XmlWriter _writer;
            private ReferenceManager _referenceManager;
        }

        public class ExporterException : Exception
        {
            public ExporterException(string message)
                    : base(message)
            {
            }
        }
        public SaveAsExcelFileStreamWriterHelper(Stream stream)
        {
            zipArchive = new ZipArchive(stream, ZipArchiveMode.Create, true);
        }
        public SaveAsExcelFileStreamWriterHelper(Stream stream, bool leaveOpen)
        {
            zipArchive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen);
        }

        public ExcelSheet AddSheet(string sheetName = null)
        {
            string sheetFileName = "sheet" + (sheetNames.Count + 1);
            if (sheetName == null)
            {
                sheetName = sheetFileName;
            }
            EnsureValidSheetName(sheetName);

            sheetNames.Add(sheetName);
            XmlWriter sheetWriter = AddEntry($"xl/worksheets/{sheetFileName}.xml");
            return new ExcelSheet(sheetWriter);
        }

        public void Dispose()
        {
            WriteMinimalTemplate();
            zipArchive.Dispose();
        }


        internal class ReferenceManager
        {
            public ReferenceManager(XmlWriter writer)
            {
                _writer = writer;
            }
            private int _currColumn; // 0 is invalid, the first AddRow will set to 1
            private int _currRow = 1;
            private char[] _currReference = new char[3 + 7]; //maximal XFD1048576
            private int _currReferenceRowLength;
            private int _currReferenceColumnLength;
            private XmlWriter _writer;

            public void AssureColumnReference()
            {
                if (_currColumn == 0)
                {
                    throw new ExporterException("AddRow must be called before AddCell");

                }
                if (_currColumn > 16384)
                {
                    throw new ExporterException("max column number is 16384, see https://support.office.com/en-us/article/Excel-specifications-and-limits-1672b34d-7043-467e-8e27-269d656771c3");
                }
            }

            public void WriteAndIncreaseColumnReference()
            {
                _writer.WriteStartAttribute("r");
                _writer.WriteChars(_currReference, 3 - _currReferenceColumnLength, _currReferenceRowLength + _currReferenceColumnLength);
                _writer.WriteEndAttribute();
                IncreaseColumnReference();
            }

            public void IncreaseColumnReference()
            {
                AssureColumnReference();
                char[] reference = _currReference;
                _currColumn++;
                if ('Z' == reference[2]++)
                {
                    reference[2] = 'A';
                    if (_currReferenceColumnLength < 2)
                    {
                        _currReferenceColumnLength = 2;
                    }
                    if ('Z' == reference[1]++)
                    {
                        reference[0]++;
                        reference[1] = 'A';
                        _currReferenceColumnLength = 3;
                    }
                }
            }
            private void ResetColumnReference()
            {
                _currColumn = 1;
                _currReference[0] = _currReference[1] = (char)('A' - 1);
                _currReference[2] = 'A';
                _currReferenceColumnLength = 1;

                string rowReference = _currRow.ToString();
                _currReferenceRowLength = rowReference.Length;
                rowReference.CopyTo(0, _currReference, 3, rowReference.Length);
            }

            public void AssureRowReference()
            {
                if (_currRow > 1048576)
                {
                    throw new ExporterException("max row number is 1048576, see https://support.office.com/en-us/article/Excel-specifications-and-limits-1672b34d-7043-467e-8e27-269d656771c3");
                }
            }
            public void WriteAndIncreaseRowReference()
            {
                _writer.WriteStartAttribute("r");
                _writer.WriteValue(_currRow);
                _writer.WriteEndAttribute();

                ResetColumnReference(); //This need to be called before the increase

                _currRow++;
            }

            public bool PenddingRowEndTag()
            {
                return _currRow != 1;
            }
        }

        private ZipArchive zipArchive;
        private List<string> sheetNames = new List<string>();

        XmlWriterSettings _writeSetting = new XmlWriterSettings()
        {
            CloseOutput = true,

        };
        private XmlWriter AddEntry(string entryName)
        {
            ZipArchiveEntry entry = zipArchive.CreateEntry(entryName, CompressionLevel.Fastest);
            return XmlWriter.Create(entry.Open(), _writeSetting);
        }

        //ECMA-376 page 75
        private void WriteMinimalTemplate()
        {
            WriteTopRel();
            WriteWorkbook();
            WriteStyle();
            WriteContentType();
            WriteWorkbookRel();
        }

        /// <summary>
        /// write [Content_Types].xml
        /// </summary>
        /// <remarks>
        /// This file need to describe all the files in the zip.
        /// </remarks>
        private void WriteContentType()
        {
            using (XmlWriter xw = AddEntry("[Content_Types].xml"))
            {
                xw.WriteStartDocument();
                xw.WriteStartElement("Types", "http://schemas.openxmlformats.org/package/2006/content-types");

                xw.WriteStartElement("Default");
                xw.WriteAttributeString("Extension", "rels");
                xw.WriteAttributeString("ContentType", "application/vnd.openxmlformats-package.relationships+xml");
                xw.WriteEndElement();

                xw.WriteStartElement("Override");
                xw.WriteAttributeString("PartName", "/xl/workbook.xml");
                xw.WriteAttributeString("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml");
                xw.WriteEndElement();

                xw.WriteStartElement("Override");
                xw.WriteAttributeString("PartName", "/xl/styles.xml");
                xw.WriteAttributeString("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml");
                xw.WriteEndElement();

                for (int i = 1; i <= sheetNames.Count; ++i)
                {
                    xw.WriteStartElement("Override");
                    xw.WriteAttributeString("PartName", "/xl/worksheets/sheet" + i + ".xml");
                    xw.WriteAttributeString("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml");
                    xw.WriteEndElement();
                }
                xw.WriteEndElement();
                xw.WriteEndDocument();
            }
        }
        /// <summary>
        /// Write _rels/.rels. This file only need to reference main workbook
        /// </summary>
        private void WriteTopRel()
        {
            using (XmlWriter xw = AddEntry("_rels/.rels"))
            {
                xw.WriteStartDocument();

                xw.WriteStartElement("Relationships", "http://schemas.openxmlformats.org/package/2006/relationships");

                xw.WriteStartElement("Relationship");
                xw.WriteAttributeString("Id", "rId1");
                xw.WriteAttributeString("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument");
                xw.WriteAttributeString("Target", "xl/workbook.xml");
                xw.WriteEndElement();

                xw.WriteEndElement();

                xw.WriteEndDocument();
            }
        }

        private static char[] _invalidSheetNameCharacters = new char[]
        {
            '\\', '/','*','[',']',':','?'
        };
        private void EnsureValidSheetName(string sheetName)
        {
            if (sheetName.IndexOfAny(_invalidSheetNameCharacters) != -1)
            {
                throw new ExporterException($"Invalid sheetname: sheetName");
            }
            if (sheetNames.IndexOf(sheetName) != -1)
            {
                throw new ExporterException($"Duplicate sheetName: {sheetName}");
            }
        }

        /// <summary>
        /// Write xl/workbook.xml. This file will references the sheets through ids in xl/_rels/workbook.xml.rels
        /// </summary>
        private void WriteWorkbook()
        {
            using (XmlWriter xw = AddEntry("xl/workbook.xml"))
            {
                xw.WriteStartDocument();
                xw.WriteStartElement("workbook", "http://schemas.openxmlformats.org/spreadsheetml/2006/main");
                xw.WriteAttributeString("xmlns", "r", null, "http://schemas.openxmlformats.org/officeDocument/2006/relationships");
                xw.WriteStartElement("sheets");
                for (int i = 1; i <= sheetNames.Count; i++)
                {
                    xw.WriteStartElement("sheet");
                    xw.WriteAttributeString("name", sheetNames[i - 1]);
                    xw.WriteAttributeString("sheetId", i.ToString());
                    xw.WriteAttributeString("r", "id", null, "rId" + i);
                    xw.WriteEndElement();
                }
                xw.WriteEndDocument();
            }
        }

        /// <summary>
        /// Write xl/_rels/workbook.xml.rels. This file will have the paths of the style and sheets.
        /// </summary>
        private void WriteWorkbookRel()
        {
            using (XmlWriter xw = AddEntry("xl/_rels/workbook.xml.rels"))
            {
                xw.WriteStartDocument();
                xw.WriteStartElement("Relationships", "http://schemas.openxmlformats.org/package/2006/relationships");

                xw.WriteStartElement("Relationship");
                xw.WriteAttributeString("Id", "rId0");
                xw.WriteAttributeString("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles");
                xw.WriteAttributeString("Target", "styles.xml");
                xw.WriteEndElement();

                for (int i = 1; i <= sheetNames.Count; i++)
                {
                    xw.WriteStartElement("Relationship");
                    xw.WriteAttributeString("Id", "rId" + i);
                    xw.WriteAttributeString("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet");
                    xw.WriteAttributeString("Target", "worksheets/sheet" + i + ".xml");
                    xw.WriteEndElement();
                }
                xw.WriteEndElement();
                xw.WriteEndDocument();
            }
        }

        private enum Style
        {
            Normal = 0,
            Date = 1,
            Time = 2,
            DateTime = 3,
        }

        private void WriteStyle()
        {
            // the style 0 is used for general case, style 1 for date, style 2 for time and style 3 for datetime see Enum Style
            // reference chain: (index start with 0)
            // <c>(in sheet1.xml) --> (by s) <cellXfs> --> (by xfId) <cellStyleXfs>
            //                                               --> (by numFmtId) <numFmts>
            // that is <c s="1"></c> will reference the second element of <cellXfs> <xf numFmtId=""162"" xfId=""0"" applyNumberFormat=""1""/>
            // then, this xf reference numFmt by name and get formatCode "hh:mm:ss"

            string content = @"
<styleSheet xmlns=""http://schemas.openxmlformats.org/spreadsheetml/2006/main"">
  <numFmts count=""3"">
    <numFmt numFmtId=""166"" formatCode=""yyyy-mm-dd""/>
    <numFmt numFmtId=""167"" formatCode=""hh:mm:ss""/>
    <numFmt numFmtId=""168"" formatCode=""yyyy-mm-dd hh:mm:ss""/>
  </numFmts>
  <fonts count=""1"">
    <font>
      <sz val=""11""/>
      <color theme=""1""/>
      <name val=""Calibri""/>
      <family val=""2""/>
      <scheme val=""minor""/>
    </font>
  </fonts>
  <fills count=""1"">
    <fill>
      <patternFill patternType=""none""/>
    </fill>
  </fills>
  <borders count=""1"">
    <border>
      <left/>
      <right/>
      <top/>
      <bottom/>
      <diagonal/>
    </border>
  </borders>
  <cellStyleXfs count=""1"">
    <xf numFmtId=""0"" fontId=""0"" fillId=""0"" borderId=""0""/>
  </cellStyleXfs>
  <cellXfs count=""4"">
    <xf xfId=""0""/>
    <xf numFmtId=""166"" xfId=""0"" applyNumberFormat=""1""/>
    <xf numFmtId=""167"" xfId=""0"" applyNumberFormat=""1""/>
    <xf numFmtId=""168"" xfId=""0"" applyNumberFormat=""1""/>
  </cellXfs>
  <cellStyles count=""1"">
    <cellStyle name=""Normal"" builtinId=""0"" xfId=""0"" />
  </cellStyles>
</styleSheet>";

            using (XmlWriter xw = AddEntry("xl/styles.xml"))
            {
                xw.WriteRaw(content);
            }
        }
    }
}
