﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.SqlTools.ServiceLayer.LanguageServices.Completion.Extension
{
    public interface ICompletionExtensionProvider
    {
        /// <summary>
        /// Called when the extension is loaded for the completion service.
        /// </summary>
        Task<ICompletionExtension> CreateAsync(IReadOnlyDictionary<string, object> properties, CancellationToken cancellationToken = default(CancellationToken));
    }

}
