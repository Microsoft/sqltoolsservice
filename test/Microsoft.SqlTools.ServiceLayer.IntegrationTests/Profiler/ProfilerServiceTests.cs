//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.XEvent;
using Microsoft.SqlTools.Hosting.Protocol;
using Microsoft.SqlTools.ServiceLayer.Admin;
using Microsoft.SqlTools.ServiceLayer.Admin.Contracts;
using Microsoft.SqlTools.ServiceLayer.Connection;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery;
using Microsoft.SqlTools.ServiceLayer.DisasterRecovery.Contracts;
using Microsoft.SqlTools.ServiceLayer.FileBrowser;
using Microsoft.SqlTools.ServiceLayer.FileBrowser.Contracts;
using Microsoft.SqlTools.ServiceLayer.IntegrationTests.Utility;
using Microsoft.SqlTools.ServiceLayer.Profiler;
using Microsoft.SqlTools.ServiceLayer.Profiler.Contracts;
using Microsoft.SqlTools.ServiceLayer.Test.Common;
using Moq;
using Xunit;

namespace Microsoft.SqlTools.ServiceLayer.IntegrationTests.Profiler
{
    public class ProfilerServiceTests
    {
        /// <summary>
        /// Verify that a start profiling request starts a profiling session
        /// </summary>
        [Fact]
        public async Task TestHandleStartAndStopProfilingRequests()
        {
            // initialize test state
            ProfilerService profilerService = new ProfilerService();
            var liveConnection = LiveConnectionHelper.InitLiveConnectionInfo("master");

            // start a new session
            var startParams = new StartProfilingParams();
            startParams.OwnerUri = liveConnection.ConnectionInfo.OwnerUri;
            startParams.TemplateName = "Standard";

            string sessionId = null;
            var startContext = new Mock<RequestContext<StartProfilingResult>>();
            startContext.Setup(rc => rc.SendResult(It.IsAny<StartProfilingResult>()))
                .Returns<StartProfilingResult>((result) => 
                {
                    // capture the session id for sending the stop message
                    sessionId = result.SessionId;
                    return Task.FromResult(0);
                });

            await profilerService.HandleStartProfilingRequest(startParams, startContext.Object);

            startContext.VerifyAll();

            // wait a bit for the session monitoring to initialize
            Thread.Sleep(TimeSpan.FromHours(1));

            // stop the session
            var stopParams = new StopProfilingParams()
            {
                SessionId = sessionId
            };

            var stopContext = new Mock<RequestContext<StopProfilingResult>>();
            stopContext.Setup(rc => rc.SendResult(It.IsAny<StopProfilingResult>()))
                .Returns(Task.FromResult(0));

            await profilerService.HandleStopProfilingRequest(stopParams, stopContext.Object);


            stopContext.VerifyAll();
        }

        /// <summary>
        /// Verify  the profiler service XEvent session factory
        /// </summary>
        [Fact]
        public void TestCreateXEventSession()
        {
            var liveConnection = LiveConnectionHelper.InitLiveConnectionInfo("master");
            ProfilerService profilerService = new ProfilerService();
            IXEventSession xeSession = profilerService.CreateXEventSession(liveConnection.ConnectionInfo);
            Assert.NotNull(xeSession);
            Assert.NotNull(xeSession.GetTargetXml());
        }
    }
}