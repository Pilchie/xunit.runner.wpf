using GalaSoft.MvvmLight;
using System.Windows.Input;
using System;
using System.Windows;
using GalaSoft.MvvmLight.CommandWpf;
using Microsoft.Win32;
using Xunit;
using Xunit.Abstractions;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Specialized;

namespace xunit.runner.wpf.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        public MainViewModel()
        {
            if (IsInDesignMode)
            {
                this.Assemblies.Add(new TestAssemblyViewModel(@"C:\Code\TestAssembly.dll"));
                this.TestCases.Add("A.B.Test()");
            }
            ////else
            ////{
            ////    // Code runs "for real"
            ////}

            CommandBindings = CreateCommandBindings();
            this.MethodsCaption = "Methods (0)";

            this.TestCases.CollectionChanged += TestCases_CollectionChanged;
        }

        private void TestCases_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            MethodsCaption = $"Methods ({TestCases.Count})";
        }

        public ICommand ExitCommand { get; } = new RelayCommand(OnExecuteExit);

        public CommandBindingCollection CommandBindings { get; }

        private string methodsCaption;
        public string MethodsCaption
        {
            get { return methodsCaption; }
            private set { Set(ref methodsCaption, value); }
        }

        private CommandBindingCollection CreateCommandBindings()
        {
            var openBinding = new CommandBinding(ApplicationCommands.Open, OnExecuteOpen);
            CommandManager.RegisterClassCommandBinding(typeof(MainViewModel), openBinding);

            return new CommandBindingCollection
            {
                openBinding,
            };
        }

        public ObservableCollection<TestAssemblyViewModel> Assemblies { get; } = new ObservableCollection<TestAssemblyViewModel>();
        public ObservableCollection<string> TestCases { get; } = new ObservableCollection<string>();

        private void OnExecuteOpen(object sender, ExecutedRoutedEventArgs e)
        {
            var fileDialog = new OpenFileDialog
            {
                Filter = "Unit Test Assemblies|*.dll",
            };

            if (fileDialog.ShowDialog(Application.Current.MainWindow) != true)
            {
                return;
            }

            var fileName = fileDialog.FileName;

            try
            {
                var xunit = new XunitFrontController(
                    useAppDomain: false,
                    assemblyFileName: fileName,
                    shadowCopy: false);
                var testDiscoveryVisitor = new TestDiscoveryVisitor();
                xunit.Find(includeSourceInformation: false, messageSink: testDiscoveryVisitor, discoveryOptions: TestFrameworkOptions.ForDiscovery());
                testDiscoveryVisitor.Finished.WaitOne();

                Assemblies.Add(new TestAssemblyViewModel(fileName));
                TestCases.AddRange(testDiscoveryVisitor.TestCases.Select(tc => tc.DisplayName));
            }
            catch(Exception ex)
            {
                MessageBox.Show(Application.Current.MainWindow, ex.ToString());
            }
        }

        private class TestDiscoveryVisitor : TestMessageVisitor<IDiscoveryCompleteMessage>
        {
            public IList<ITestCase> TestCases { get; } = new List<ITestCase>();
            public IDictionary<string, IList<string>> Traits { get; } = new Dictionary<string, IList<string>>();

            protected override bool Visit(ITestCaseDiscoveryMessage testCaseDiscovered)
            {
                var testCase = testCaseDiscovered.TestCase;
                TestCases.Add(testCase);

                foreach (var k in testCase.Traits.Keys)
                {
                    IList<string> value;
                    if (!Traits.TryGetValue(k, out value))
                    {
                        value = new List<string>();
                        Traits[k] = value;
                    }

                    value.AddRange(testCase.Traits[k]);
                }

                return true;
            }
        }

        private static void OnExecuteExit()
        {
            Application.Current.Shutdown();
        }
    }
}