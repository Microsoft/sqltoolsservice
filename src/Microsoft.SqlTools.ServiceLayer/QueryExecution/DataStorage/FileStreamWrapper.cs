﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.DataStorage
{
    /// <summary>
    /// Wrapper for a file stream, providing simplified creation, deletion, read, and write
    /// functionality.
    /// </summary>
    public class FileStreamWrapper : IFileStreamWrapper
    {
        #region Member Variables

        private byte[] buffer;
        private int bufferDataSize;
        private FileStream fileStream;
        private bool readingOnly;
        private long startOffset;
        private long currentOffset;

        #endregion

        /// <summary>
        /// Constructs a new FileStreamWrapper and initializes its state.
        /// </summary>
        public FileStreamWrapper()
        {
            // Initialize the internal state
            bufferDataSize = 0;
            startOffset = 0;
            currentOffset = 0;            
        }

        #region IFileStreamWrapper Implementation

        /// <summary>
        /// Initializes the wrapper by creating the internal buffer and opening the requested file.
        /// If the file does not already exist, it will be created.
        /// </summary>
        /// <param name="fileName">Name of the file to open/create</param>
        /// <param name="bufferLength">The length of the internal buffer</param>
        /// <param name="forReadingOnly">
        /// Whether or not the wrapper will be used for reading. If <c>true</c>, any calls to a
        /// method that writes will cause an InvalidOperationException
        /// </param>
        public void Init(string fileName, int bufferLength, bool forReadingOnly)
        {
            // Sanity check for valid buffer length and filename
            if (bufferLength <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferLength), "Buffer length must be a positive value");
            }
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentNullException(nameof(fileName), "File name cannot be null or whitespace");
            }

            // Setup the buffer
            buffer = new byte[bufferLength];

            // Open the requested file for reading/writing, creating one if it doesn't exist
            fileStream = new FileStream(fileName, FileMode.OpenOrCreate,
                forReadingOnly ? FileAccess.Read : FileAccess.ReadWrite, FileShare.ReadWrite,
                bufferLength, false /*don't use asyncio*/);
            readingOnly = forReadingOnly;

            // make file hidden
            FileInfo fileInfo = new FileInfo(fileName);
            fileInfo.Attributes |= FileAttributes.Hidden;
        }

        /// <summary>
        /// Reads data into a buffer from the current offset into the file
        /// </summary>
        /// <param name="buf">The buffer to output the read data to</param>
        /// <param name="bytes">The number of bytes to read into the buffer</param>
        /// <returns>The number of bytes read</returns>
        public int ReadData(byte[] buf, int bytes)
        {
            return ReadData(buf, bytes, currentOffset);
        }

        /// <summary>
        /// Reads data into a buffer from the specified offset into the file
        /// </summary>
        /// <param name="buf">The buffer to output the read data to</param>
        /// <param name="bytes">The number of bytes to read into the buffer</param>
        /// <param name="offset">The offset into the file to start reading bytes from</param>
        /// <returns>The number of bytes read</returns>
        public int ReadData(byte[] buf, int bytes, long offset)
        {
            // Make sure that we're initialized before performing operations
            if (buffer == null)
            {
                throw new InvalidOperationException("FileStreamWrapper must be initialized before performing operations");
            }

            MoveTo(offset);

            int bytesCopied = 0;
            while (bytesCopied < bytes)
            {
                int bufferOffset = (int)(currentOffset - startOffset);
                int bytesToCopy = (bytes - bytesCopied);
                if (bytesToCopy > (bufferDataSize - bufferOffset))
                {
                    bytesToCopy = bufferDataSize - bufferOffset;
                }
                Buffer.BlockCopy(buffer, bufferOffset, buf, bytesCopied, bytesToCopy);
                bytesCopied += bytesToCopy;

                if (bytesCopied < bytes &&             // did not get all the bytes yet
                    bufferDataSize == buffer.Length)   // since current data buffer is full we should continue reading the file
                {
                    // move forward one full length of the buffer
                    MoveTo(startOffset + buffer.Length);
                }
                else
                {
                    // copied all the bytes requested or possible, adjust the current buffer pointer
                    currentOffset += bytesToCopy;
                    break;
                }
            }
            return bytesCopied;
        }

        /// <summary>
        /// Writes data to the underlying filestream, with buffering.
        /// </summary>
        /// <param name="buf">The buffer of bytes to write to the filestream</param>
        /// <param name="bytes">The number of bytes to write</param>
        /// <returns>The number of bytes written</returns>
        public int WriteData(byte[] buf, int bytes)
        {
            // Make sure that we're initialized before performing operations
            if (buffer == null)
            {
                throw new InvalidOperationException("FileStreamWrapper must be initialized before performing operations");
            }
            if (readingOnly)
            {
                throw new InvalidOperationException("This FileStreamWrapper canot be used for writing");
            }

            int bytesCopied = 0;
            while (bytesCopied < bytes)
            {
                int bufferOffset = (int)(currentOffset - startOffset);
                int bytesToCopy = (bytes - bytesCopied);
                if (bytesToCopy > (buffer.Length - bufferOffset))
                {
                    bytesToCopy = buffer.Length - bufferOffset;
                }
                Buffer.BlockCopy(buf, bytesCopied, buffer, bufferOffset, bytesToCopy);
                bytesCopied += bytesToCopy;

                // adjust the current buffer pointer
                currentOffset += bytesToCopy;

                if (bytesCopied < bytes) // did not get all the bytes yet
                {
                    Debug.Assert((int)(currentOffset - startOffset) == buffer.Length);
                    // flush buffer
                    Flush();
                }
            }
            Debug.Assert(bytesCopied == bytes);
            return bytesCopied;
        }

        /// <summary>
        /// Flushes the internal buffer to the filestream
        /// </summary>
        public void Flush()
        {
            // Make sure that we're initialized before performing operations
            if (buffer == null)
            {
                throw new InvalidOperationException("FileStreamWrapper must be initialized before performing operations");
            }
            if (readingOnly)
            {
                throw new InvalidOperationException("This FileStreamWrapper cannot be used for writing");
            }

            // Make sure we are at the right place in the file
            Debug.Assert(fileStream.Position == startOffset);

            int bytesToWrite = (int)(currentOffset - startOffset);
            fileStream.Write(buffer, 0, bytesToWrite);
            startOffset += bytesToWrite;
            fileStream.Flush();

            Debug.Assert(startOffset == currentOffset);
        }

        /// <summary>
        /// Deletes the given file (ideally, created with this wrapper) from the filesystem
        /// </summary>
        /// <param name="fileName">The path to the file to delete</param>
        public static void DeleteFile(string fileName)
        {
            File.Delete(fileName);
        }

        #endregion

        /// <summary>
        /// Moves the internal buffer to the specified offset into the file
        /// </summary>
        /// <param name="offset">Offset into the file to move to</param>
        private void MoveTo(long offset)
        {
            if (buffer.Length > bufferDataSize ||         // buffer is not completely filled
                offset < startOffset ||                   // before current buffer start
                offset >= (startOffset + buffer.Length))  // beyond current buffer end
            {
                // init the offset
                startOffset = offset;

                // position file pointer
                fileStream.Seek(startOffset, SeekOrigin.Begin);

                // fill in the buffer
                bufferDataSize = fileStream.Read(buffer, 0, buffer.Length);
            }
            // make sure to record where we are
            currentOffset = offset;
        }

        #region IDisposable Implementation

        private bool disposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing && fileStream != null)
            {
                if(!readingOnly) { Flush(); }
                fileStream.Dispose();
            }

            disposed = true;
        }

        ~FileStreamWrapper()
        {
            Dispose(false);
        }

        #endregion
    }
}
