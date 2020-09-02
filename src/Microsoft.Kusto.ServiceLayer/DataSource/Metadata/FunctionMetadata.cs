﻿namespace Microsoft.Kusto.ServiceLayer.DataSource.Metadata
{
    public class FunctionMetadata : DatabaseMetadata
    {
        public string DatabaseName { get; set; }
        
        public string Parameters { get; set; }
        
        public string Body { get; set; }
    }
}