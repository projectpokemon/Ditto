using System;
using System.Collections.Generic;
using System.Text;

namespace Ditto
{
    public class AppSettings
    {
        public bool UseBlobStorage { get; set; }
        public string BlobConnectionString { get; set; }
        public string BlobContainerName { get; set; }
        public string BlobContainerFolder { get; set; }
    }
}
