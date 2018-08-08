﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Concurrent;
using Microsoft.SqlTools.Hosting.Protocol;
using Xunit;

namespace Microsoft.SqlTools.Hosting.UnitTests.ProtocolTests
{
    public class EventContextTests
    {
        [Fact]
        public void SendEvent()
        {
            // Setup: Create collection
            var bc = new BlockingCollection<Message>(new ConcurrentQueue<Message>());

            // If: I construct an event context with a message writer
            //     And send an event with it
            var eventContext = new EventContext(bc);
            eventContext.SendEvent(CommonObjects.EventType, CommonObjects.TestMessageContents.DefaultInstance);
            
            // Then: The message should be added to the queue
            Assert.Single(bc.ToArray());
            Assert.Equal(MessageType.Event, bc.ToArray()[0].MessageType);
        }
    }
}