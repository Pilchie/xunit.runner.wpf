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
                        case Constants.ActionRunAll:
                            RunAll(stream, argument);
                            break;
                        case Constants.ActionRunSpecific:
                            RunSpecific(stream, argument);
                            break;
                        default:
                            Usage();
                            return ExitError;
                    }

                    connection.WaitForClientDone();
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

        private static void RunAll(Stream stream, string assemblyPath)
        {
            Console.WriteLine($"run all started: {assemblyPath}");
            RunUtil.RunAll(assemblyPath, stream);
            Console.WriteLine("run all ended");
        }

        private static void RunSpecific(Stream stream, string assemblyPath)
        {
            Console.WriteLine($"run specific started: {assemblyPath}");
            RunUtil.RunSpecific(assemblyPath, stream);
            Console.WriteLine("run specific ended");
        }

        private static void Usage()
        {
            Console.WriteLine("xunit.runner.worker [pipe name] [action] [assembly path]");
            Console.WriteLine("\tpipe name:     Name of the pipe this worker should communicate on");
            Console.WriteLine("\taction:        Action performed by the worker (run or discover tests");
            Console.WriteLine("\assembly path:  Path of assembly to perform the action against");
        }
    }
}
