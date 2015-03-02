using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ClearScript.V8;
using Newtonsoft.Json;

namespace Orc.ReactProcessor.Core
{
    public class ReactRunner: IDisposable
    {

        protected const string ROUTER_OUTPUT_KEY = "_ReactNET_RouterOutput_Html";
        protected const string IN_BROWSER_DATA_KEY = "_ReactNET_InitialData_JSON";

        public ReactRunner(string file, bool enableFileWatcher, bool enableCompilation, bool disableGlobalMembers)
        {
            JsFile = file;
            EnableFileWatcher = enableFileWatcher;
            EnableCompilation = enableCompilation;
            DisableGlobalMembers = disableGlobalMembers;

            Runtime = new V8Runtime();

            ScriptRaw = File.ReadAllText(JsFile);

            if (EnableCompilation)
            {
                Compiled = Runtime.Compile(ScriptRaw);
            }

            if (EnableFileWatcher)
            {
                fileWatcher = new FileSystemWatcher();
                fileWatcher.Path = Path.GetDirectoryName(JsFile);
                fileWatcher.Filter = Path.GetFileName(JsFile);
                fileWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Attributes | NotifyFilters.Size;
                fileWatcher.Changed += fileWatcher_Changed;
                fileWatcher.EnableRaisingEvents = true;
            }

        }


        string JsFile { get; set; }
        bool EnableFileWatcher { get; set; }
        bool EnableCompilation { get; set; }
        bool DisableGlobalMembers { get; set; }

        V8Runtime Runtime { get; set; }

        V8Script Compiled { get; set; }
        string ScriptRaw { get; set; }
        FileSystemWatcher fileWatcher;

        void fileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            ScriptRaw = File.ReadAllText(JsFile);

            if (EnableCompilation)
            {
                Compiled = Runtime.Compile(ScriptRaw);
            }
        }

        public string Execute(string containerId, string url, object props)
        {
            return Execute(containerId, url, JsonConvert.SerializeObject(props));
        }

        public string Execute(string containerId, string url, string encodedProps, out string inBrowserScript)
        {

            V8ScriptEngine engine;
            if (DisableGlobalMembers)
            {
                engine = Runtime.CreateScriptEngine(V8ScriptEngineFlags.DisableGlobalMembers);
            }
            else
            {
                engine = Runtime.CreateScriptEngine();
            }



            if (EnableCompilation)
            {
                engine.Execute(Compiled);
            }
            else
            {
                engine.Execute(ScriptRaw);
            }


            var routerInitCode =
                String.Format(
                    @"Router.run( reactRoutesConfig, '{0}', function( Handler ) {{ 
                        {1} = React.renderToString(React.createElement(Handler, {2} ));
                    }});",
                    url,
                    ROUTER_OUTPUT_KEY,
                    encodedProps
                    );

            engine.Execute(routerInitCode);

            inBrowserScript =
                String.Format(
                    @"
                    var {1} = {3};

                    Router.run( reactRoutesConfig, {0}, function( Handler ) {{ 
                        React.render(React.createElement(Handler,{1}, document.getElementById( '{2}' ) ));
                    }});",
                   "Router.HistoryLocation",
                   IN_BROWSER_DATA_KEY,
                   encodedProps,
                   containerId
               );

            // TODO: Might swap this timeout stuff for an actual Timespan check instead
            var timeOutCounter = 0;
            var maxCyclesBeforeTimeout = 10; // TODO: Config this

            // Poll the engine to see if the router callback has fired

            while (!engine.HasVariable(ROUTER_OUTPUT_KEY) && timeOutCounter <= maxCyclesBeforeTimeout)
            {
                timeOutCounter++;
                Thread.Sleep(10); // DIRTY!
            }

            // Default to a timeout message
            var html = "<p>Router init timed out waiting for response, try increasing the timeout in React.Config</p>";

            if (timeOutCounter <= maxCyclesBeforeTimeout)
            {
                html = engine.GetVariableValue<string>(ROUTER_OUTPUT_KEY);
            }

            return string.Format(
                    "<{2} id=\"{0}\">{1}</{2}>",
                    containerId,
                    html,
                    "div"
                );
        }

        public void Dispose()
        {
            if (fileWatcher != null)
            {
                fileWatcher.EnableRaisingEvents = false;
                fileWatcher.Dispose();
            }
        }
    }
}
