using Newtonsoft.Json;

namespace Orc.ReactProcessor.Web
{
    public class ReactConfiguration
    {
        public ReactConfiguration()
        {
            EnableCompilation = true;
            EnableFileWatcher = true;
            DisableGlobalMembers = true;
            SerializerSettings = new JsonSerializerSettings();
        }
        public static ReactConfiguration Current { get; set; }

        public string FilePath { get; set; }
        public bool EnableCompilation { get; set; }
        public bool EnableFileWatcher { get; set; }
        public bool DisableGlobalMembers { get; set; }
        public JsonSerializerSettings SerializerSettings { get; set; }
    }
}