//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

namespace Microsoft.SqlTools.ServiceLayer.Agent
{
	/// <summary>
	/// defines notion of sitable object
	/// </summary>
	public interface IObjectWithSite
	{
	    void SetSite(System.IServiceProvider sp);
    }
    
	/// <summary>
	/// ISqlControlCollection allows access to a collection of dialog views
	/// </summary>
	public interface ISqlControlCollection : IObjectWithSite
	{

        /// <summary>
        /// How many controls are in the collection
        /// </summary>
        int ViewsCount
        { get; }

        /// <summary>
        /// How many TreeNodes are in the collection
        /// </summary>
        int NodesCount
        { get; }

    }
}