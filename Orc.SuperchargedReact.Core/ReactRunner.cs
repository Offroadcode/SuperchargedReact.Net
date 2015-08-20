using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ClearScript.V8;
using Newtonsoft.Json;

namespace Orc.SuperchargedReact.Core
{
    public class ReactRunner: IDisposable
    {
        
        /// <summary>
        /// In engine key to grab the html
        /// </summary>
        protected const string ROUTER_OUTPUT_KEY = "SuperChargedReact.renderOutput";

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
        string BootstrapCommand { get; set; }

        /// <summary>
        /// Creates
        /// </summary>
        /// <param name="file">The relative path to your bundle file of js, we only expect your code to be in one file or for it to use requires that this file will handle for us</param>
        /// <param name="enableFileWatcher">If in dev mode good to have the file watcher on, infact just good to have on all the time</param>
        /// <param name="enableCompilation">Highly recommended, caches the parsed code after the first run so skips parsing time, much faster</param>
        /// <param name="disableGlobalMembers">Tells V8 that it won't be able to have a 2 way chat with .net, disabling this makes for quite a speed increase</param>
        /// <param name="bootstrapCommand">The javascript command to run to kick things off</param>
        /// <param name="serializationSettings"></param>
        public ReactRunner(string file, bool enableFileWatcher, bool enableCompilation, bool disableGlobalMembers, string bootstrapCommand, JsonSerializerSettings serializationSettings )
        {
            //setup assembly resolver so it can find the v8 dlls
            AssemblyResolver.Initialize();

            JsFile = file;
            EnableFileWatcher = enableFileWatcher;
            EnableCompilation = enableCompilation;
            DisableGlobalMembers = disableGlobalMembers;
            SerializationSettings = serializationSettings;
            BootstrapCommand = bootstrapCommand;
            
            //Initialize the v8 runtime
            Runtime = new V8Runtime();
            
            //Read the scripts text content
            ScriptRaw = File.ReadAllText(JsFile);

            if (EnableCompilation)
            {
                //If compilation is enabled, we compile the scripts
                CompiledShimms = Runtime.Compile(JavascriptShimms.ConsoleShim);
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
        /// Excutes the passed in script
        /// </summary>
        /// <param name="containerId"></param>
        /// <param name="url"></param>
        /// <param name="props"></param>
        /// <param name="inBrowserScript"></param>
        /// <param name="measurements"></param>
        /// <returns></returns>
        public string Execute( IRenderSettings settings, out string inBrowserScript, out ReactPerformaceMeasurements measurements)
        {
            var bootstrapper = BootstrapCommand + "(" + JsonConvert.SerializeObject(settings, SerializationSettings) + ");";

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
                    stopwatch.Stop();
                    measurements.EngineInitializationTime = stopwatch.ElapsedMilliseconds;

                    //Firstly we'll add the libraries to the engine!
                    if (EnableCompilation)
                    {
                        // Use the pre-compiled ones for speed
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
                        // Use the raw uncompiled ones, these will be parsed a fresh each time by the JS engine
                        stopwatch.Restart();
                        engine.Execute(JavascriptShimms.ConsoleShim);
                        stopwatch.Stop();
                        measurements.ShimmInitializationTime = stopwatch.ElapsedMilliseconds;
                        
                        stopwatch.Restart();
                        engine.Execute(ScriptRaw);
                        stopwatch.Stop();
                        measurements.ScriptsInitializationTime = stopwatch.ElapsedMilliseconds;
                    }

                    // Kick off the render
                    stopwatch.Restart();
                    engine.Execute( bootstrapper );

                    // TODO: Might swap this timeout stuff for an actual Timespan check instead
                    var timeOutCounter = 0;
                    var maxCyclesBeforeTimeout = 10; // TODO: Config this

                    // Poll the engine to see if the router callback has fired
                    while (
                        (
                            !engine.HasVariable(ROUTER_OUTPUT_KEY) 
                            || 
                            string.IsNullOrEmpty(engine.GetVariableValue<string>(ROUTER_OUTPUT_KEY) ) 
                        )
                        && 
                        timeOutCounter <= maxCyclesBeforeTimeout
                        )
                    {
                        timeOutCounter++;
                        Thread.Sleep(10); // DIRTY! But it works, need to bench mark if this is quicker than linking the JS engine to the .net context as then we could ping the value straight out form within JS. Alternatively we could get away with just reducing this sleep time but it depends on how much code we have running, machine speed etc. Its "fast enough" for now though.
                    }

                    stopwatch.Stop();
                    measurements.ComponentGenerationTime = stopwatch.ElapsedMilliseconds;

                    // Default to a timeout message
                    var html =
                        "<p>Router init timed out waiting for response, try increasing the timeout in React.Config</p>";

                    if (timeOutCounter <= maxCyclesBeforeTimeout)
                    {
                        html = engine.ExecuteCommand(ROUTER_OUTPUT_KEY);// engine.GetVariableValue<string>();
                    }

                    //get the console statements out of the engine
                    var consoleStatements = engine.Evaluate<string>("console.getCalls()");

                    inBrowserScript = consoleStatements + ";" + bootstrapper;
                    
                    //Cleanup the engine
                    stopwatch.Restart();
                    Cleanup(engine);
                    stopwatch.Stop();
                    measurements.CleanupTime = stopwatch.ElapsedMilliseconds;
                    
                    return string.Format(
                        "<{2} id=\"{0}\">{1}</{2}>",
                        settings.ContainerId,
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
            engine.CollectGarbage(true);
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
