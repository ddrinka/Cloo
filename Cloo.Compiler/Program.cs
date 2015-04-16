using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

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
        // OpenCL example error messages:
/*
"C:\Users\xxx\AppData\Local\Temp\OCL916T8.cl", line 6: error: identifier "s"
          is undefined
    if (idx < num) x[idx] = x[idx] + y[idx];s
  	                                        ^

"C:\Users\xxx\AppData\Local\Temp\OCL916T8.cl", line 7: error: expected a ";"
  }
  ^

2 errors detected in the compilation of "C:\Users\xxx\AppData\Local\Temp\OCL916T8.cl".
Frontend phase failed compilation.
*/

        /// <summary>
        /// Matches essential parts of build log messages.
        /// </summary>
        private static readonly Regex ErrorRegex = new Regex(@"^""(.*)"", line (\d+): error: (.*)$", RegexOptions.Multiline);
        
        /// <summary>
        /// Matches the random temporary filename assigned by the OpenCL driver.
        /// </summary>
        private static readonly Regex TempFileRegex = new Regex(@"^(.*OCL.*cl)$");
        
        public static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: Cloo.Compiler <path>");
                Console.WriteLine("Builds all OpenCL source files found within <path>.");
                Console.WriteLine("Error messages are formatted so that Visual Studio can properly parse them into the error list.");
                return;
            }
            // initialize context and command queue for a GPU device
            var platform = ComputePlatform.Platforms[0];
            var context = new ComputeContext(ComputeDeviceTypes.Gpu, new ComputeContextPropertyList(platform), null, IntPtr.Zero);
            // iterate over OpenCL source files
            foreach (var filename in Directory.EnumerateFiles(args[0], "*.cl", SearchOption.AllDirectories))
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
                        //Console.WriteLine("Build result for {0}", device.Name);
                        //Console.WriteLine("Status: {0}", program.GetBuildStatus(device));
                        foreach (var message in ParseBuildLog(program.GetBuildLog(device), filename))
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

        /// <summary>
        /// Parses the OpenCL build log to a format parsable by Visual Studio.
        /// Most likely driver dependent. This works for the current driver from AMD (as of April 2015).
        /// </summary>
        /// <param name="log">The OpenCL build log.</param>
        /// <param name="filename">The filename of the file which was built.</param>
        /// <returns>An enumerable of error messages.</returns>
        private static IEnumerable<string> ParseBuildLog(string log, string filename)
        {
            var fileLine = true;
            Match match = null;
            var message = new List<string>();
            foreach (var line in log.Split('\n'))
            {
                // keep looking for a line that contains filename, line number and the first part of the error message
                if (fileLine)
                {
                    match = ErrorRegex.Match(line);
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
                errorFilename = TempFileRegex.Replace(errorFilename, filename);
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
