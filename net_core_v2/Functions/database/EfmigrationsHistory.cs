using System;
using System.Collections.Generic;

namespace Functions.database
{
    public partial class EfmigrationsHistory
    {
        public string MigrationId { get; set; }
        public string ProductVersion { get; set; }
    }
}
