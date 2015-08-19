using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace xunit.runner.wpf.ViewModel
{
    public class TestAssemblyViewModel : ViewModelBase
    {
        private readonly AssemblyAndConfigFile assembly;

        public TestAssemblyViewModel(AssemblyAndConfigFile assembly)
        {
            this.assembly = assembly;
        }

        public string FileName => assembly.AssemblyFileName;
        public string ConfigFileName => Path.GetFileNameWithoutExtension(assembly.ConfigFileName);
        public string DisplayName => Path.GetFileNameWithoutExtension(assembly.AssemblyFileName);
    }
}
