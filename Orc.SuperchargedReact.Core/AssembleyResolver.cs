using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Orc.SuperchargedReact.Core
{
    /// <summary>
    /// Assembly resolver
    /// </summary>
    internal static class AssemblyResolver
    {
        /// <summary>
        /// Name of directory, that contains the Microsoft ClearScript.V8 assemblies
        /// </summary>
        private const string ASSEMBLY_DIRECTORY_NAME = "../ClearScript.V8";

        /// <summary>
        /// Name of the ClearScriptV8 assembly
        /// </summary>
        private const string ASSEMBLY_NAME = "ClearScriptV8";

        /// <summary>
        /// Regular expression for working with the `bin` directory path
        /// </summary>
        private static readonly Regex BinDirectoryRegex = new Regex(@"\\bin\\?$", RegexOptions.IgnoreCase);

        private static bool _isLoaded = false;

        /// <summary>
        /// Initialize a assembly resolver
        /// </summary>
        public static void Initialize()
        {
            if (!_isLoaded)
            {
                AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolveHandler;
                _isLoaded = true;
            }
        }

        private static Assembly AssemblyResolveHandler(object sender, ResolveEventArgs args)
        {
            if (args.Name.StartsWith(ASSEMBLY_NAME, StringComparison.OrdinalIgnoreCase))
            {
                var currentDomain = (AppDomain)sender;
                string platform = Environment.Is64BitProcess ? "64" : "32";

                string binDirectoryPath = currentDomain.SetupInformation.PrivateBinPath;
                if (string.IsNullOrEmpty(binDirectoryPath))
                {
                    // `PrivateBinPath` property is empty in test scenarios, so
                    // need to use the `BaseDirectory` property
                    binDirectoryPath = currentDomain.BaseDirectory;
                }

                string assemblyDirectoryPath = Path.Combine(binDirectoryPath, ASSEMBLY_DIRECTORY_NAME);
                string assemblyFileName = string.Format("{0}-{1}.dll", ASSEMBLY_NAME, platform);
                string assemblyFilePath = Path.Combine(assemblyDirectoryPath, assemblyFileName);

                if (!Directory.Exists(assemblyDirectoryPath))
                {
                    if (BinDirectoryRegex.IsMatch(binDirectoryPath))
                    {
                        string applicationRootPath = BinDirectoryRegex.Replace(binDirectoryPath, string.Empty);
                        assemblyDirectoryPath = Path.Combine(applicationRootPath, ASSEMBLY_DIRECTORY_NAME);

                        if (!Directory.Exists(assemblyDirectoryPath))
                        {
                            throw new DirectoryNotFoundException(
                                string.Format("Failed to load the ClearScriptV8 assembly, because the directory '{0}' does not exist.", assemblyDirectoryPath));
                        }

                        assemblyFilePath = Path.Combine(assemblyDirectoryPath, assemblyFileName);
                    }
                    else
                    {
                        throw new DirectoryNotFoundException(
                            string.Format("Failed to load the ClearScriptV8 assembly, because the directory '{0}' does not exist.", assemblyDirectoryPath));
                    }
                }

                if (!File.Exists(assemblyFilePath))
                {
                    throw new FileNotFoundException(
                        string.Format("Failed to load the ClearScriptV8 assembly, because the file '{0}' does not exist.", assemblyFilePath));
                }

                return Assembly.LoadFile(assemblyFilePath);
            }

            return null;
        }
    }
}

