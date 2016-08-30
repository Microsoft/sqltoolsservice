﻿using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Threading.Tasks;
using Microsoft.SqlTools.ServiceLayer.QueryExecution.Contracts;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    public interface IFileStreamReader : IDisposable
    {
        Task<object[]> ReadRow(long offset, IEnumerable<DbColumnWrapper> columns);
        Task<FileStreamReadResult<short>>  ReadInt16(long i64Offset);
        Task<FileStreamReadResult<int>>  ReadInt32(long i64Offset);
        Task<FileStreamReadResult<long>> ReadInt64(long i64Offset);
        Task<FileStreamReadResult<byte>> ReadByte(long i64Offset);
        Task<FileStreamReadResult<char>>  ReadChar(long i64Offset);
        Task<FileStreamReadResult<bool>>  ReadBoolean(long i64Offset);
        Task<FileStreamReadResult<float>>  ReadSingle(long i64Offset);
        Task<FileStreamReadResult<double>>  ReadDouble(long i64Offset);
        Task<FileStreamReadResult<SqlDecimal>>  ReadSqlDecimal(long i64Offset);
        Task<FileStreamReadResult<decimal>>  ReadDecimal(long i64Offset);
        Task<FileStreamReadResult<DateTime>>  ReadDateTime(long i64Offset);
        Task<FileStreamReadResult<TimeSpan>>  ReadTimeSpan(long i64Offset);
        Task<FileStreamReadResult<string>>  ReadString(long i64Offset);
        Task<FileStreamReadResult<byte[]>>  ReadBytes(long i64Offset);
        Task<FileStreamReadResult<DateTimeOffset>>  ReadDateTimeOffset(long i64Offset);
    }
}
