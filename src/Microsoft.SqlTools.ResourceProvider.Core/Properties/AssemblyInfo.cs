//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("Microsoft.SqlTools.ServiceLayer.UnitTests")]
[assembly: InternalsVisibleTo("Microsoft.SqlTools.ServiceLayer.IntegrationTests")]
[assembly: InternalsVisibleTo("Microsoft.SqlTools.ServiceLayer.Test.Common")]

// Allowing internals visible access to Moq library to help testing
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
