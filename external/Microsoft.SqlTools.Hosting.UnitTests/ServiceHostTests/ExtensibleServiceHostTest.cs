﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using Microsoft.SqlTools.Hosting.Channels;
using Microsoft.SqlTools.Hosting.Extensibility;
using Microsoft.SqlTools.Hosting.Protocol;
using Moq;
using NUnit.Framework;

namespace Microsoft.SqlTools.Hosting.UnitTests.ServiceHostTests
{
    [TestFixture]
    public class ExtensibleServiceHostTest
    {
        [Test]
        public void CreateExtensibleHostNullProvider()
        {
            // If: I create an extensible host with a null provider
            // Then: I should get an exception
            var cb = new Mock<ChannelBase>();
            Assert.Throws<ArgumentNullException>(() => new ExtensibleServiceHost(null, cb.Object));
        }
        
        [Test]
        public void CreateExtensibleHost()
        {
            // Setup: 
            // ... Create a mock hosted service that can initialize
            var hs = new Mock<IHostedService>();
            var mockType = typeof(Mock<IHostedService>);
            hs.Setup(o => o.InitializeService(It.IsAny<IServiceHost>()));
            hs.SetupGet(o => o.ServiceType).Returns(mockType);
            
            // ... Create a service provider mock that will return some stuff
            var sp = new Mock<RegisteredServiceProvider>();
            sp.Setup(o => o.GetServices<IHostedService>())
                .Returns(new[] {hs.Object});
            sp.Setup(o => o.RegisterSingleService(mockType, hs.Object));
            
            // If: I create an extensible host with a custom provider
            var cb = new Mock<ChannelBase>();
            var esh = new ExtensibleServiceHost(sp.Object, cb.Object);
            
            // Then:
           
            // ... The service should have been initialized
            hs.Verify(o => o.InitializeService(esh), Times.Once());            
            // ... The service host should have it's provider exposed
            Assert.AreEqual(sp.Object, esh.ServiceProvider);
        }

        [Test]
        public void CreateDefaultExtensibleHostNullAssemblyList()
        {
            // If: I create a default server extensible host with a null provider
            // Then: I should get an exception
            var cb = new Mock<ChannelBase>();
            Assert.Throws<ArgumentNullException>(() => ExtensibleServiceHost.CreateDefaultExtensibleServer(".", null));
        }
        
        [Test]
        public void CreateDefaultExtensibleHost()
        {
            // If: I create a default server extensible host
            var esh = ExtensibleServiceHost.CreateDefaultExtensibleServer(".", new string[] { });
            
            // Then: 
            // ... The service provider should be setup
            Assert.NotNull(esh.ServiceProvider);
            
            var jh = esh.jsonRpcHost as JsonRpcHost;
            Assert.NotNull(jh);
            Assert.That(jh.protocolChannel, Is.InstanceOf<StdioServerChannel>(), "The underlying rpc host should be using the stdio server channel ");
            Assert.False(jh.protocolChannel.IsConnected);
        }
    }
}