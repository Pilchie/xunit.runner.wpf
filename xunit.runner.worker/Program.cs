using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Xunit.Runner.Worker
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

            var pipeName = args[0];
            var parentPid = Int32.Parse(args[1]);
            var process = Process.GetProcessById(parentPid);
            if (process == null)
            {
                Console.WriteLine($"Invalid parent pid {parentPid}");
                return ExitError;
            }

            Task.WaitAny(
                Task.Run(() => process.WaitForExit()),
                Task.Run(() => new Listener(pipeName).Go()));

            return ExitSuccess;
        }

        private static void Usage()
        {
            Console.WriteLine("xunit.runner.worker [pipe name] [action] [assembly path]");
            Console.WriteLine("\tpipe name:     Name of the pipe this worker should communicate on");
            Console.WriteLine("\taction:        Action performed by the worker (run or discover tests)");
            Console.WriteLine("\tassembly path:  Path of assembly to perform the action against");
        }
    }
}
