using System;
using System.Collections.Generic;
using System.Dynamic;
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

        public ReactRunner(string file, bool enableFileWatcher, bool enableCompilation, bool disableGlobalMembers, JsonSerializerSettings serializationSettings)
        {
            AssemblyResolver.Initialize();

            JsFile = file;
            EnableFileWatcher = enableFileWatcher;
            EnableCompilation = enableCompilation;
            DisableGlobalMembers = disableGlobalMembers;
            SerializationSettings = serializationSettings;
            Runtime = new V8Runtime();

            ScriptRaw = File.ReadAllText(JsFile);

            if (EnableCompilation)
            {
                Compiled = Runtime.Compile(ScriptRaw);
                CompiledShimms = Runtime.Compile(JavascriptShimms.ConsoleShim);

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
        JsonSerializerSettings SerializationSettings { get; set; }
        V8Runtime Runtime { get; set; }

        V8Script CompiledShimms { get; set; }
        V8Script Compiled { get; set; }
        string ScriptRaw { get; set; }
        FileSystemWatcher fileWatcher;

        void fileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            //wait for the file to be fully written
            var didRead = false;
            var readAttempts = 0;
            while (!didRead)
            {
                readAttempts++;
                try
                {
                    ScriptRaw = File.ReadAllText(JsFile);
                    didRead = true;
                }
                catch (Exception exception)
                {
                    if (readAttempts >= 10)
                    {
                        break;
                    }
                    Thread.Sleep(500);
                }
            }


            if (didRead && EnableCompilation)
            {
                Compiled = Runtime.Compile(ScriptRaw);
            }
        }

        public string Execute(string containerId, string url, object props, out string inBrowserScript)
        {
            return Execute(containerId, url, JsonConvert.SerializeObject(props, SerializationSettings), out inBrowserScript);
        }

        public string Execute(string containerId, string url, string encodedProps, out string inBrowserScript)
        {
            try
            {


                var engineFlags = V8ScriptEngineFlags.None;
                if (DisableGlobalMembers)
                {
                    engineFlags = V8ScriptEngineFlags.DisableGlobalMembers;
                }
                using (var engine = Runtime.CreateScriptEngine(engineFlags))
                {

                    if (EnableCompilation)
                    {
                        engine.Execute(CompiledShimms);
                        engine.Execute(Compiled);
                    }
                    else
                    {
                        engine.Execute(JavascriptShimms.ConsoleShim);
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
                    var html =
                        "<p>Router init timed out waiting for response, try increasing the timeout in React.Config</p>";

                    if (timeOutCounter <= maxCyclesBeforeTimeout)
                    {
                        html = engine.GetVariableValue<string>(ROUTER_OUTPUT_KEY);
                    }
                    var consoleStatements = engine.Evaluate<string>("console.getCalls()");

                    inBrowserScript =
                        String.Format(
                            @"
                            var {0} = {1};

                            Router.run( reactRoutesConfig, Router.HistoryLocation, function( Handler ) {{ 
                                React.render(
                                    React.createElement(Handler, {0} ), 
                                    document.getElementById( '{2}' )
                                );
                            }});",
                            
                            IN_BROWSER_DATA_KEY,
                            encodedProps,
                            containerId
                            );

                    inBrowserScript = consoleStatements + inBrowserScript;
                    Cleanup(engine);

                    return string.Format(
                        "<{2} id=\"{0}\">{1}</{2}>",
                        containerId,
                        html,
                        "div"
                        );
                }
            }
            catch (Microsoft.ClearScript.ScriptEngineException e)
            {
                throw new Exception(e.ErrorDetails);
            }
        }
        private static void Cleanup(V8ScriptEngine engine)
        {
            var data = engine.Script as DynamicObject;
            var cleanup = new StringBuilder();

            foreach (var item in data.GetDynamicMemberNames())
            {
                if (item != "EngineInternal")
                {
                    cleanup.Append("delete " + item + ";");
                }
            }
            
            engine.Execute(cleanup.ToString());
         //   engine.CollectGarbage(true);
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
