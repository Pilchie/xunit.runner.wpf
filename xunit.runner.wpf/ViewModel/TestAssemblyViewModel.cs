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
        public TestAssemblyViewModel(string fileName)
        {
            this.FileName = fileName;
        }

        public string FileName { get; }

        public string DisplayName => Path.GetFileName(FileName);
    }
}
