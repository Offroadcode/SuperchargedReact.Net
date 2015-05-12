using System;
using Newtonsoft.Json;

namespace Orc.SuperchargedReact.Web
{
    public class ReactConfiguration
    {
        public ReactConfiguration()
        {
            EnableCompilation = true;
            EnableFileWatcher = true;
            DisableGlobalMembers = true;
            SerializerSettings = new JsonSerializerSettings();
            JSGlobalVar = "global";
            JSGlobalNamespace = "SuperChargedReact";
            BootStrapMethodName = "bootstrapper";
        }
        public static ReactConfiguration Current { get; set; }

        public string FilePath { get; set; }
        public bool EnableCompilation { get; set; }
        public bool EnableFileWatcher { get; set; }
        public bool DisableGlobalMembers { get; set; }

        public JsonSerializerSettings SerializerSettings { get; set; }
        public string JSGlobalVar { get; set; }
        public string JSGlobalNamespace { get; set; }
        public string BootStrapMethodName { get; set; }

        public string GlobalCommand { get { return JSGlobalVar; } }
        //public string GlobalNamespaceCommand { get { return JSGlobalVar + "." + JSGlobalNamespace; } }
        public string GlobalNamespaceCommand { get { return JSGlobalNamespace; } }
        public string BootStrapperCommand { get { return GlobalNamespaceCommand + "." + BootStrapMethodName; } }
    }
}