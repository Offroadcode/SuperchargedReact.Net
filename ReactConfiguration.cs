using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Orc.ReactProcessor.Mvc
{
    public class ReactConfiguration
    {
        public ReactConfiguration()
        {
            EnableCompilation = true;
            EnableFileWatcher = true;
            DisableGlobalMembers = true;
        }
        public static ReactConfiguration Current { get; set; }

        public string FilePath { get; set; }
        public bool EnableCompilation { get; set; }
        public bool EnableFileWatcher { get; set; }
        public bool DisableGlobalMembers { get; set; }
    }
}