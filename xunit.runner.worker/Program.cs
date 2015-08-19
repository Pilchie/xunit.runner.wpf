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
        public static void Main(string[] args)
        {
            using (var namedPipeServer = new NamedPipeServerStream(Constants.PipeName))
            {
                namedPipeServer.WaitForConnection();
                var fileName = args.Length == 0
                    ? @"C:\Users\jaredpar\Documents\Github\VsVim\Test\VimWpfTest\bin\Debug\Vim.UI.Wpf.UnitTest.dll"
                    : args[0];
                Console.WriteLine($"discover started: {fileName}");
                Discover.Go(fileName, namedPipeServer);
                Console.WriteLine("discover ended");
                namedPipeServer.Close();
                Console.WriteLine("pipe closed");
            }
        }
    }
}
