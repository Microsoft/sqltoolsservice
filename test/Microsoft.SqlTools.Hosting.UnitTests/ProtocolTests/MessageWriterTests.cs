﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlTools.Hosting.Protocol;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Microsoft.SqlTools.Hosting.UnitTests.ProtocolTests
{
    [TestFixture]
    public class MessageWriterTests
    {
        #region Construction Tests
        
        [Test]
        public void ConstructMissingOutputStream()
        {
            // If: I attempt to create a message writer without an output stream
            // Then: I should get an exception
            Assert.Throws<ArgumentNullException>(() => new MessageWriter(null));
        }
        
        #endregion
        
        #region WriteMessageTests

        [Test]
        public async Task WriteMessageNullMessage()
        {
            // If: I write a null message
            // Then: I should get an exception
            var mw = new MessageWriter(Stream.Null);            
            Assert.ThrowsAsync<ArgumentNullException>(() => mw.WriteMessage(null));
        }
        
        [TestCaseSource(nameof(WriteMessageData))]
        public async Task WriteMessage(object contents, Dictionary<string, object> expectedDict)
        {
            // NOTE: This technically tests the ability of the Message class to properly serialize
            //       various types of message contents. This *should* be part of the message tests
            //       but it is simpler to test it here.
            
            // Setup: Create a stream to capture the output
            var output = new byte[8192];
            using (var outputStream = new MemoryStream(output))
            {
                // If: I write a message
                var mw = new MessageWriter(outputStream);
                await mw.WriteMessage(Message.CreateResponse(CommonObjects.MessageId, contents));

                // Then:
                // ... The returned bytes on the stream should compose a valid message
                Assert.That(outputStream.Position, Is.Not.EqualTo(0), "outputStream.Position after WriteMessage");
                var messageDict = ValidateMessageHeaders(output, (int) outputStream.Position);
                
                // ... ID, Params, Method should be present
                AssertMessage(messageDict, expectedDict);
            }
        }
        
        public static IEnumerable<object[]> WriteMessageData
        {
            get
            {
                yield return new object[]
                {
                    null, 
                    new Dictionary<string, object> {{"id", "123"}, {"result", null}}
                };
                yield return new object[]
                {
                    "simple param",
                    new Dictionary<string, object> {{"id", "123"}, {"result", "simple param"}}
                };
                yield return new object[]
                {
                    CommonObjects.TestMessageContents.DefaultInstance,
                    new Dictionary<string, object> {{"id", "123"}, {"result", CommonObjects.TestMessageContents.SerializedContents}}
                };
            }
        }
        
        #endregion
        
        #region Private Helpers
        
        private static void AssertMessage(Dictionary<string, object> messageDict, Dictionary<string, object> expectedDict)
        {
            // Add the jsonrpc property to the expected dict
            expectedDict.Add("jsonrpc", "2.0");
            
            // Make sure the number of elements in both dictionaries are the same
            Assert.AreEqual(expectedDict.Count, messageDict.Count);
            
            // Make sure the elements match
            foreach (var kvp in expectedDict)
            {
                Assert.AreEqual(expectedDict[kvp.Key], messageDict[kvp.Key]);
            }
        }
        
        private static Dictionary<string, object> ValidateMessageHeaders(byte[] outputBytes, int bytesWritten)
        {
            // Convert the written bytes to a string
            string outputString = Encoding.UTF8.GetString(outputBytes, 0, bytesWritten);
            
            // There should be two sections to the message
            string[] outputParts = outputString.Split("\r\n\r\n");
            Assert.AreEqual(2, outputParts.Length);
            
            // The first section is the headers
            string[] headers = outputParts[0].Split("\r\n");
            Assert.AreEqual(2, outputParts.Length);
            
            // There should be a content-type and a content-length 
            int? contentLength = null;
            bool contentTypeCorrect = false;
            foreach (string header in headers)
            {
                // Headers should look like "Header-Key: HeaderValue"
                string[] headerParts = header.Split(':');
                Assert.AreEqual(2, headerParts.Length);

                string headerKey = headerParts[0];
                string headerValue = headerParts[1].Trim();

                if (headerKey == "Content-Type" && headerValue.StartsWith("application/json"))
                {
                    contentTypeCorrect = true;
                } 
                else if (headerKey == "Content-Length")
                {
                    contentLength = int.Parse(headerValue);
                }
                else
                {
                    throw new Exception($"Invalid header provided: {headerKey}");
                }
            }            

            // Make sure the headers are correct
            Assert.True(contentTypeCorrect);
            Assert.AreEqual(outputParts[1].Length, contentLength);
            
            // Deserialize the body into a dictionary
            return JsonConvert.DeserializeObject<Dictionary<string, object>>(outputParts[1]);
        }
        
        #endregion
    }
}