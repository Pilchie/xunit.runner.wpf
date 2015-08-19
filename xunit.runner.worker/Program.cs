using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xunit.runner.data;

namespace xunit.runner.worker
{
    public static class Program
    {
        private const int ExitSuccess = 0;
        private const int ExitError = 1;

        public static int Main(string[] args)
        {
            if (args.Length < 2)
            {
                Usage();
                return ExitError;
            }

            Stream stream = null;
            try
            {
                var namedPipeServerStream = new NamedPipeServerStream(Constants.PipeName);
                namedPipeServerStream.WaitForConnection();
                stream = namedPipeServerStream;

                switch (args[0])
                {
                    case Constants.ActionDiscover:
                        Discover(stream, args[1]);
                        break;
                    case Constants.ActionRun:
                        Run(stream, args[1]);
                        break;
                    default:
                        Usage();
                        return ExitError;
                }
            }
            catch (Exception ex)
            {
                // Errors will happen during a rude shut down from the client. Print out to the screen
                // for diagnostics and continue on.
                Console.Error.WriteLine(ex.Message);
                return ExitError;
            }
            finally
            {
                stream.Close();
            }

            return ExitSuccess;
        }

        private static void Discover(Stream stream, string assemblyPath)
        {
            Console.WriteLine($"discover started: {assemblyPath}");
            DiscoverUtil.Go(assemblyPath, stream);
            Console.WriteLine("discover ended");
        }

        private static void Run(Stream stream, string assemblyPath)
        {
            Console.WriteLine($"run started: {assemblyPath}");
            RunUtil.Go(assemblyPath, stream);
            Console.WriteLine("run ended");
        }

        private static void Usage()
        {
            Console.Error.WriteLine("Need at least two arguments");
        }
    }
}
