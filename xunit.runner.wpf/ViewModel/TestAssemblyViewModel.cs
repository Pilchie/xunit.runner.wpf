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
        private readonly AssemblyAndConfigFile _assembly;
        private bool _isSelected;

        public TestAssemblyViewModel(AssemblyAndConfigFile assembly)
        {
            _assembly = assembly;
        }

        public string FileName => _assembly.AssemblyFileName;
        public string ConfigFileName => Path.GetFileNameWithoutExtension(_assembly.ConfigFileName);
        public string DisplayName => Path.GetFileNameWithoutExtension(_assembly.AssemblyFileName);

        public bool IsSelected
        {
            get { return _isSelected; }
            set { Set(ref _isSelected, value, nameof(IsSelected)); }
        }
    }
}
