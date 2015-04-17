using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Cloo.Compiler.Properties;

namespace Cloo.Compiler
{
    /// <summary>
    /// Builds all OpenCL files within a given path and outputs error messages. 
    /// The messages can be parsed by Visual Studio to be properly displayed in the error list.
    /// </summary>
    /// <remarks>
    /// The easiest way to use this feature is to add a reference to Cloo.Compiler so that the executable gets copied to the output folder and add the following Post-build event:<br/>
    /// "$(TargetDir)Cloo.Compiler.exe" "$(ProjectDir)Data"<br/>
    /// This assumes all your OpenCL files are in the project folder "Data".
    /// </remarks>
    public class KernelCompiler
    {
        private static readonly Regex ErrorRegexIntel = new Regex(@"^\d+:(\d+):(\d+): (\w+): (.*)$", RegexOptions.Multiline);
        private static readonly Regex ErrorRegexAmd = new Regex(@"^""(.*)"", line (\d+): error: (.*)$", RegexOptions.Multiline);
        private static readonly Regex TempFileRegexAmd = new Regex(@"^(.*OCL.*cl)$");

        private const string VendorIntel = "Intel(R) Corporation";
        private const string VendorAmd = "Advanced Micro Devices, Inc";

        public static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: Cloo.Compiler <path>");
                Console.WriteLine("Builds all OpenCL source files found within <path>.");
                Console.WriteLine("Error messages are formatted so that Visual Studio can properly parse them into the error list.");
                return;
            }
            // retrieve target platform in the following order:
            // 1. match vendor name to the one given in the settings
            // 2. try to get the intel plattform (because the compiler has nice output)
            // 3. use the first platform available
            var platform = ComputePlatform.Platforms.FirstOrDefault(_ => _.Vendor.Contains(Settings.Default.PlattformVendor)) ??
                           ComputePlatform.Platforms.FirstOrDefault(_ => _.Vendor.Contains(VendorIntel)) ??
                           ComputePlatform.Platforms[0];
            // try to parse device types from settings
            ComputeDeviceTypes deviceType;
            if (!Enum.TryParse(Settings.Default.DeviceType, true, out deviceType))
            {
                // default to All device types
                deviceType = ComputeDeviceTypes.All;
            }
            // get an OpenCL context and build all files within the given path
            using (var context = new ComputeContext(deviceType, new ComputeContextPropertyList(platform), null, IntPtr.Zero))
            {
                BuildFiles(args[0], context);
            }
        }

        private static void BuildFiles(string path, ComputeContext context)
        {
            // iterate over OpenCL source files
            foreach (var filename in Directory.EnumerateFiles(path, "*.cl", SearchOption.AllDirectories))
            {
                var program = new ComputeProgram(context, File.ReadAllText(filename));
                try
                {
                    // provide the path to the file as a compiler options to enable #include-statements
                    var options = string.Format("-I \"{0}\"", Path.GetDirectoryName(filename));
                    // build the program for all devices in the context
                    program.Build(null, options, null, IntPtr.Zero);
                    Console.WriteLine("OpenCL build successful: {0}", filename);
                }
                catch (ComputeException)
                {
                    Console.WriteLine("OpenCL build failed: {0}", filename);
                    foreach (var device in context.Devices)
                    {
                        var log = program.GetBuildLog(device);
                        IEnumerable<string> messages;
                        switch (context.Platform.Vendor)
                        {
                            default:
                            case VendorIntel:
                                messages = ParseBuildLogIntel(log, filename);
                                break;
                            case VendorAmd:
                                messages = ParseBuildLogAmd(log, filename);
                                break;
                        }
                        foreach (var message in messages)
                        {
                            Console.Out.WriteLine(message);
                        }
                    }
                }
                finally
                {
                    program.Dispose();
                }
            }
        }

        private static IEnumerable<string> ParseBuildLogIntel(string log, string filename)
        {
            // filename(line,column): error #: message
            foreach (var line in log.Split('\n'))
            {
                var match = ErrorRegexIntel.Match(line);
                if (!match.Success) continue;
                yield return string.Format("{0}({1},{2}): {3} 0: OpenCL error: {4}", filename, match.Groups[1].Value, match.Groups[2].Value,  match.Groups[3].Value, match.Groups[4].Value);
            }
        }

        /// <summary>
        /// Parses the OpenCL build log generated by the AMD compiler to a format parsable by Visual Studio.
        /// Tested on the current driver from AMD as of April 2015.
        /// </summary>
        /// <param name="log">The OpenCL build log.</param>
        /// <param name="filename">The filename of the file which was built.</param>
        /// <returns>An enumerable of error messages.</returns>
        private static IEnumerable<string> ParseBuildLogAmd(string log, string filename)
        {
            var fileLine = true;
            Match match = null;
            var message = new List<string>();
            foreach (var line in log.Split('\n'))
            {
                // keep looking for a line that contains filename, line number and the first part of the error message
                if (fileLine)
                {
                    match = ErrorRegexAmd.Match(line);
                    if (!match.Success) continue;
                    fileLine = false;
                    message.Clear();
                    message.Add(match.Groups[3].Value);
                    continue;
                }
                // check if current line contains more parts of the error message
                if (!line.Trim().Equals("^"))
                {
                    message.Add(line.Trim());
                    continue;
                }
                // current line contains only the column indicator
                // remove the last message line because it contains the actual code and we don't want that in the message
                message.RemoveAt(message.Count - 1);
                // get the filename from the match
                var errorFilename = match.Groups[1].Value;
                // if it matches the random temporary filename given by the compiler replace it with the correct filename
                errorFilename = TempFileRegexAmd.Replace(errorFilename, filename);
                // now the actual error message can be assembled
                // to have Visual Studio correctly parse the error message it has to be in this format:
                // filename(line,column): error #: message
                // see http://blogs.msdn.com/b/msbuild/archive/2006/11/03/msbuild-visual-studio-aware-error-messages-and-message-formats.aspx
                yield return string.Format("{0}({1},{2}): error 0: OpenCL error: {3}", errorFilename, match.Groups[2].Value, line.Length - 2, string.Join(" ", message));
                // start looking for a filename again
                fileLine = true;
            }
        }
    }
}
