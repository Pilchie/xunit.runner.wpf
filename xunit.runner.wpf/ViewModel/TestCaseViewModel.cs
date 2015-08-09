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
        private readonly ITestCase testCase;

        public TestCaseViewModel(ITestCase testCase)
        {
            this.testCase = testCase;
        }

        public string DisplayName => testCase.DisplayName;

        public TestState State => TestState.NotRun;
    }
}
