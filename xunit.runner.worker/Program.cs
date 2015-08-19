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
            if (args.Length < 3)
            {
                Usage();
                return ExitError;
            }

            string pipeName = args[0];
            string action = args[1];
            string argument = args[2];

            try
            {
                using (var connection = CreateConnection(pipeName))
                {
                    connection.WaitForClientConnect();

                    var stream = connection.Stream;

                    switch (action)
                    {
                        case Constants.ActionDiscover:
                            Discover(stream, argument);
                            break;
                        case Constants.ActionRun:
                            Run(stream, argument);
                            break;
                        default:
                            Usage();
                            return ExitError;
                    }

                }
            }
            catch (Exception ex)
            {
                // Errors will happen during a rude shut down from the client. Print out to the screen
                // for diagnostics and continue on.
                Console.Error.WriteLine(ex.Message);
                return ExitError;
            }

            return ExitSuccess;
        }

        private static Connection CreateConnection(string pipeName)
        {
            if (pipeName == "test")
            {
                return new TestConnection();
            }

            return new NamedPipeConnection(pipeName);
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
