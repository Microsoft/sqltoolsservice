﻿namespace Microsoft.Kusto.ServiceLayer.DataSource
{
    /// <summary>
    /// Object metadata information
    /// </summary>
    public class DataSourceObjectMetadata 
    {
        public DataSourceMetadataType MetadataType { get; set; }
    
        public string MetadataTypeName { get; set; }

        public string Name { get; set; }

        public string PrettyName { get; set; }
        
        public string Urn { get; set; }
    }
}