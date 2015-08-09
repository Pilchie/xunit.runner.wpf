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
using System.Threading;
using System.Threading.Tasks;

namespace xunit.runner.wpf.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private readonly ObservableCollection<TestCaseViewModel> allTestCases = new ObservableCollection<TestCaseViewModel>();
        private CancellationTokenSource filterCancellationTokenSource = new CancellationTokenSource();

        public MainViewModel()
        {
            if (IsInDesignMode)
            {
                this.Assemblies.Add(new TestAssemblyViewModel(@"C:\Code\TestAssembly.dll"));
            }

            CommandBindings = CreateCommandBindings();
            this.MethodsCaption = "Methods (0)";

            TestCases = new FilteredCollectionView<TestCaseViewModel, Tuple<string, TestState>>(
                allTestCases, TestCaseMatches, Tuple.Create(string.Empty, TestState.All), TestComparer.Instance);

            this.TestCases.CollectionChanged += TestCases_CollectionChanged;
        }

        private static bool TestCaseMatches(TestCaseViewModel testCase, Tuple<string, TestState> filterTextAndTestState)
            => testCase.DisplayName.Contains(filterTextAndTestState.Item1) && (testCase.State & filterTextAndTestState.Item2) == filterTextAndTestState.Item2;

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

        private string searchQuery = string.Empty;
        public string SearchQuery
        {
            get { return searchQuery; }
            set
            {
                if (Set(ref searchQuery, value))
                {
                    FilterAfterDelay();
                }
            }
        }

        private TestState resultFilter = TestState.All;
        public TestState ResultFilter
        {
            get { return resultFilter; }
            set
            {
                if (Set(ref resultFilter, value))
                {
                    this.FilterAfterDelay();
                }
            }
        }

        private void FilterAfterDelay()
        {
            filterCancellationTokenSource.Cancel();
            filterCancellationTokenSource = new CancellationTokenSource();
            var token = filterCancellationTokenSource.Token;

            Task
                .Delay(TimeSpan.FromMilliseconds(500), token)
                .ContinueWith(
                    x =>
                    {
                        TestCases.FilterArgument = Tuple.Create(SearchQuery, ResultFilter);
                    },
                    token,
                    TaskContinuationOptions.None,
                    TaskScheduler.FromCurrentSynchronizationContext());
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
        public FilteredCollectionView<TestCaseViewModel, Tuple<string, TestState>> TestCases { get; }

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
                    useAppDomain: true,
                    assemblyFileName: fileName,
                    shadowCopy: false);
                var testDiscoveryVisitor = new TestDiscoveryVisitor();
                xunit.Find(includeSourceInformation: false, messageSink: testDiscoveryVisitor, discoveryOptions: TestFrameworkOptions.ForDiscovery());
                testDiscoveryVisitor.Finished.WaitOne();

                Assemblies.Add(new TestAssemblyViewModel(fileName));
                allTestCases.AddRange(testDiscoveryVisitor.TestCases.Select(tc => new TestCaseViewModel(tc)));
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

    public enum TestState
    {
        All = 0,
        Passed,
        Failed,
        Skipped,
        NotRun
    }

    public class TestComparer : IComparer<TestCaseViewModel>
    {
        public static TestComparer Instance { get; } = new TestComparer();

        public int Compare(TestCaseViewModel x, TestCaseViewModel y)
            => StringComparer.Ordinal.Compare(x.DisplayName, y.DisplayName);

        private TestComparer() { }
    }
}