using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Configuration;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using Newtonsoft.Json;
using Orc.ReactProcessor.Core;

namespace Orc.ReactProcessor.Runner
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting");
            //  ThreadChecker();
            GeneratePerformaceReport();
            Console.WriteLine("end");
        }

        static void ThreadChecker()
        {
            var runner = new ReactRunner(@"C:\Users\Stephen\Documents\visual studio 2013\Projects\Orc.ReactProcessor\Orc.ReactProcessor.ExampleBundle\bundle.js", true, true, true, new JsonSerializerSettings());

            Parallel.For(0, 100, new ParallelOptions() { MaxDegreeOfParallelism = 50 }, (i, state) =>
            {
                Console.WriteLine("Starting:" + i);
                var testString = "helloWorld" + i;
                var browserOutput = "";
                ReactPerformaceMeasurements measurements;
                var output = runner.Execute("reactApp", "/tester", new { testString = testString }, out browserOutput,out measurements);
                if (!output.Contains(testString))
                {
                    throw new Exception("Not Thread Safe.... uh oh!");
                }
                Console.WriteLine("Complete:" + i);
            });
        }

        static void GeneratePerformaceReport()
        {
            var iterations = 50;


            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            using (var writer = new StreamWriter(stream))
            using (var csv = new CsvWriter(writer))
            {
                csv.WriteHeader(typeof(PerformaceTestResult));

              /*  csv.WriteRecords(DoTestsForSettings(iterations, false, false, false));
                csv.WriteRecords(DoTestsForSettings(iterations, false, false, true));
                csv.WriteRecords(DoTestsForSettings(iterations, false, true, false));
                csv.WriteRecords(DoTestsForSettings(iterations, false, true, true));


                csv.WriteRecords(DoTestsForSettings(iterations, true, false, false));
                csv.WriteRecords(DoTestsForSettings(iterations, true, false, true));

                csv.WriteRecords(DoTestsForSettings(iterations, true, true, false));
                */
                csv.WriteRecords(DoTestsForSettings(iterations, true, true, true));
                writer.Flush();
                stream.Position = 0;
                var text = reader.ReadToEnd();
                File.WriteAllText("Report.csv", text);
            }
        }

        static List<PerformaceTestResult> DoTestsForSettings(int testIterations, bool enableFileWatcher, bool enableCompilation, bool disableGlobalMembers)
        {
            List<PerformaceTestResult> results = new List<PerformaceTestResult>();
            for (int testi = 0; testi < testIterations; testi++)
            {
                var model = PerfTest(enableFileWatcher, enableCompilation, disableGlobalMembers);

                model.TestIteration = testi;
                model.EnableFileWatcher = enableFileWatcher;
                model.EnableCompilation = enableCompilation;
                model.DisableGlobalMembers = disableGlobalMembers;
                model.MemoryUsage = GC.GetTotalMemory(true);
                results.Add(model);
            }
            return results;
        }

        static PerformaceTestResult PerfTest(bool enableFileWatcher, bool enableCompilation, bool disableGlobalMembers)
        {
            var result = new PerformaceTestResult();
            Stopwatch init = new Stopwatch();
            init.Start();

            using (var runner =
                new ReactRunner(@"D:\work\Olympic\BookingEngine\Build\Olympic.BookingEngine\assets\js\bundle.js",
                    enableFileWatcher, enableCompilation, disableGlobalMembers, new JsonSerializerSettings()))
            {

                init.Stop();

                result.InitializationTime = init.ElapsedMilliseconds;

                var times = new List<long>();
                Console.WriteLine("Benchmarking html generation");
                var props = File.ReadAllText("props.json");

                for (int i = 0; i < 20; i++)
                {
                    init.Reset();
                    string outputStr = "";
                    init.Start();
                    ReactPerformaceMeasurements measurements;
                    runner.Execute("reactApp", "/search/AccommodationOnly", props, out outputStr, out measurements);
                    init.Stop();
                    times.Add(init.ElapsedMilliseconds);
                    //    Console.WriteLine(init.ElapsedMilliseconds);
                }

                result.Iterations = times.Count;
                result.Average = times.Average();
                result.First5GenerationsAverage = times.Take(5).Average();
                result.Last10GenerationsAverage = times.Skip(times.Count - 10).Average();

                result.QuickestGeneration = times.Min();
                result.LongestGeneration = times.Max();
               
            }
            return result;
        }

        public class PerformaceTestResult
        {
            public bool EnableFileWatcher { get; set; }
            public bool EnableCompilation { get; set; }
            public bool DisableGlobalMembers { get; set; }
            public int TestIteration { get; set; }

            public int Iterations { get; set; }

            public double Average { get; set; }
            public double First5GenerationsAverage { get; set; }
            public double Last10GenerationsAverage { get; set; }
            public long QuickestGeneration { get; set; }
            public long LongestGeneration { get; set; }
            public long InitializationTime { get; set; }

            public long MemoryUsage { get; set; }
        }

    }
}

