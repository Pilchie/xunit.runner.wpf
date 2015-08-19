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

            switch (args[0])
            {
                case Constants.ActionDiscover:
                    Discover(args[1]);
                    break;
                default:
                    Usage();
                    return ExitError;
            }

            return ExitSuccess;
        }

        private static void Discover(string assemblyPath)
        { 
            using (var namedPipeServer = new NamedPipeServerStream(Constants.PipeName))
            {
                namedPipeServer.WaitForConnection();
                Console.WriteLine($"discover started: {assemblyPath}");
                DiscoverUtil.Go(assemblyPath, namedPipeServer);
                Console.WriteLine("discover ended");
                namedPipeServer.Close();
                Console.WriteLine("pipe closed");
            }
        }

        private static void Usage()
        {
            Console.Error.WriteLine("Need at least two arguments");
        }
    }
}
