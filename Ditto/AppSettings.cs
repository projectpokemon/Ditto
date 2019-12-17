using System;
using System.Collections.Generic;
using System.Text;

namespace Ditto
{
    public class AppSettings
    {
        public string BlobConnectionString { get; set; }
        public string BlobContainerName { get; set; }
        public string BlobContainerFolder { get; set; }
    }
}
