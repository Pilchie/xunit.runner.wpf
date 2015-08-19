using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xunit.runner.worker
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var fileName = @"C:\Users\jaredpar\Documents\Github\VsVim\Test\VimWpfTest\bin\Debug\Vim.UI.Wpf.UnitTest.dll";
            var stream = new MemoryStream();
            Discover.Go(fileName, stream);
        }
    }
}
