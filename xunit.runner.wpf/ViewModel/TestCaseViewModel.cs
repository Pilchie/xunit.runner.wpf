using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace xunit.runner.wpf.ViewModel
{
    public class TestCaseViewModel : ViewModelBase
    {
        public TestCaseViewModel(ITestCase testCase, string assemblyFileName)
        {
            this.TestCase = testCase;
            this.AssemblyFileName = assemblyFileName;
        }

        public string DisplayName => TestCase.DisplayName;

        private TestState state = TestState.NotRun;
        public TestState State
        {
            get { return state; }
            set { Set(ref state, value); }
        }

        public string AssemblyFileName { get; }

        public ITestCase TestCase { get; }
    }
}
