using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using xunit.runner.data;
using Xunit.Abstractions;

namespace xunit.runner.wpf.ViewModel
{
    public class TestCaseViewModel : ViewModelBase
    {
        public TestCaseViewModel(string testCase, string displayName, string assemblyFileName)
        {
            this.TestCase = testCase;
            this.DisplayName = displayName;
            this.AssemblyFileName = assemblyFileName;
        }

        public string DisplayName { get; }

        private TestState state = TestState.NotRun;
        public TestState State
        {
            get { return state; }
            set { Set(ref state, value); }
        }

        public string AssemblyFileName { get; }

        public string TestCase { get; }
    }
}
