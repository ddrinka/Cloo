using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Cloo.Compiler
{
    /// <summary>
    /// </summary>
    /// <remarks>
    /// The easiest way to use this feature is to add a reference to ObjectTKC so that the executable gets copied to the output folder and add the following Post-build event:<br/>
    /// "$(TargetDir)ObjectTKC.exe" "$(TargetPath)"
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
        /// Matches the random temporary filename.
        /// </summary>
        private static readonly Regex FileRegex = new Regex(@"^(.*OCL.*cl)", RegexOptions.Multiline);

        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: U mad?");
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
                    Console.WriteLine("Compiled successfully: {0}", filename);
                }
                catch (ComputeException)
                {
                    // write build results for all devices to log
                    foreach (var device in context.Devices)
                    {
                        //Console.WriteLine("Build result for {0}", device.Name);
                        //Console.WriteLine("Status: {0}", program.GetBuildStatus(device));
                        var log = program.GetBuildLog(device);
                        var message = ErrorRegex.Replace(log, "$1($2): error 0: $3");
                        message = FileRegex.Replace(message, filename);
                        Console.Out.WriteLine(message);
                    }
                }
                finally
                {
                    program.Dispose();
                }
            }
        }
    }
}
