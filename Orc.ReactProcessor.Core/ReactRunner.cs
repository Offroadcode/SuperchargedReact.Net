using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private static List<WeakReference> instances = new List<WeakReference>();
        /// <summary>
        /// In engine key to grab the html
        /// </summary>
        protected const string ROUTER_OUTPUT_KEY = "_ReactNET_RouterOutput_Html";

        /// <summary>
        /// Client side scripts variable where it dumps the data
        /// </summary>
        protected const string IN_BROWSER_DATA_KEY = "_ReactNET_InitialData_JSON";

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

        public ReactRunner(string file, bool enableFileWatcher, bool enableCompilation, bool disableGlobalMembers, JsonSerializerSettings serializationSettings)
        {
            //setup assembly resolver so it can find the v8 dlls
            AssemblyResolver.Initialize();

            JsFile = file;
            EnableFileWatcher = enableFileWatcher;
            EnableCompilation = enableCompilation;
            DisableGlobalMembers = disableGlobalMembers;
            SerializationSettings = serializationSettings;
            
            //Initialize the v8 runtime
            Runtime = new V8Runtime();
            
            //Read the scripts text content
            ScriptRaw = File.ReadAllText(JsFile);

            if (EnableCompilation)
            {
                //If compilation is enabled, we compile the scripts
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

        

        /// <summary>
        /// Execute the bundle with the given settings
        /// </summary>
        public string Execute(string containerId, string url, object props, out string inBrowserScript, out ReactPerformaceMeasurements measurements)
        {
            return Execute(containerId, url, JsonConvert.SerializeObject(props, SerializationSettings), out inBrowserScript, out measurements);
        }

        public string Execute(string containerId, string url, string encodedProps, out string inBrowserScript, out ReactPerformaceMeasurements measurements)
        {
            try
            {
                measurements = new ReactPerformaceMeasurements();
                var stopwatch = new Stopwatch();

                var engineFlags = V8ScriptEngineFlags.None;
                if (DisableGlobalMembers)
                {
                    engineFlags = V8ScriptEngineFlags.DisableGlobalMembers;
                }

                stopwatch.Start();
                using (var engine = Runtime.CreateScriptEngine(engineFlags))
                {
                    instances.Add(new WeakReference(engine));

                    stopwatch.Stop();
                    measurements.EngineInitializationTime = stopwatch.ElapsedMilliseconds;

                    //Firstly we'll add the libraries to the engine!
                    if (EnableCompilation)
                    {
                        stopwatch.Restart();
                        engine.Execute(CompiledShimms);
                        stopwatch.Stop();
                        measurements.ShimmInitializationTime = stopwatch.ElapsedMilliseconds;

                        stopwatch.Restart();
                        engine.Execute(Compiled);
                        stopwatch.Stop();
                        measurements.ScriptsInitializationTime = stopwatch.ElapsedMilliseconds;

                    }
                    else
                    {
                        stopwatch.Restart();
                        engine.Execute(JavascriptShimms.ConsoleShim);
                        stopwatch.Stop();
                        measurements.ShimmInitializationTime = stopwatch.ElapsedMilliseconds;

                        stopwatch.Restart();
                        engine.Execute(ScriptRaw);
                        stopwatch.Stop();
                        measurements.ScriptsInitializationTime = stopwatch.ElapsedMilliseconds;

                    }

                    //we generate the code to execute in the engine here

                    var routerInitCode =
                        String.Format(
                            @"Router.run( reactRoutesConfig, '{0}', function( Handler ) {{ 
                        {1} = React.renderToString(React.createElement(Handler, {2} ));
                    }});",
                            url,
                            ROUTER_OUTPUT_KEY,
                            encodedProps
                            );
                    stopwatch.Restart();
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
                    stopwatch.Stop();
                    measurements.ComponentGenerationTime = stopwatch.ElapsedMilliseconds;

                    // Default to a timeout message
                    var html =
                        "<p>Router init timed out waiting for response, try increasing the timeout in React.Config</p>";

                    if (timeOutCounter <= maxCyclesBeforeTimeout)
                    {
                        html = engine.GetVariableValue<string>(ROUTER_OUTPUT_KEY);
                    }

                    //get the console statements out of the engine
                    var consoleStatements = engine.Evaluate<string>("console.getCalls()");

                    //generate the scripts to render in the browser
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
                    
                    //Cleanup the engine
                    stopwatch.Restart();
                    Cleanup(engine);
                    stopwatch.Stop();
                    measurements.CleanupTime = stopwatch.ElapsedMilliseconds;
                    measurements.InstanceCount = instances.Count(x => x.IsAlive);
                    
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

    public class ReactPerformaceMeasurements
    {
        public long EngineInitializationTime { get; set; }
        public long ShimmInitializationTime { get; set; }
        public long ScriptsInitializationTime { get; set; }
        public long ComponentGenerationTime { get; set; }
        public long CleanupTime { get; set; }
        public int InstanceCount { get; set; }
    }
}
