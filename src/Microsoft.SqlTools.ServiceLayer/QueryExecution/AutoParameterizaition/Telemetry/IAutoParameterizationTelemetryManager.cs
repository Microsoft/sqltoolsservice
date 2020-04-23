﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Collections.Generic;

namespace Microsoft.SqlTools.ServiceLayer.QueryExecution.AutoParameterizaition.Telemetry
{
    interface IAutoParameterizationTelemetryManager
    {
        void PostEvent(EventType eventType, List<EventProperty> properties);
    }
}
