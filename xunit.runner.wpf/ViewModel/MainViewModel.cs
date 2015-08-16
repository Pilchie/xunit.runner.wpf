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
using System.Diagnostics;
using System.IO;
using System.Text;

namespace xunit.runner.wpf.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private readonly ObservableCollection<TestCaseViewModel> allTestCases = new ObservableCollection<TestCaseViewModel>();
        private CancellationTokenSource filterCancellationTokenSource = new CancellationTokenSource();

        private bool isBusy;
        private bool isCancelRequested;
        private SearchQuery searchQuery = new SearchQuery();

        public MainViewModel()
        {
            if (IsInDesignMode)
            {
                this.Assemblies.Add(new TestAssemblyViewModel(new AssemblyAndConfigFile(@"C:\Code\TestAssembly.dll", null)));
            }

            CommandBindings = CreateCommandBindings();
            this.MethodsCaption = "Methods (0)";

            TestCases = new FilteredCollectionView<TestCaseViewModel, SearchQuery>(
                allTestCases, TestCaseMatches, searchQuery, TestComparer.Instance);

            this.TestCases.CollectionChanged += TestCases_CollectionChanged;
            this.WindowLoadedCommand = new RelayCommand(OnExecuteWindowLoaded);
            this.RunCommand = new RelayCommand(OnExecuteRun, CanExecuteRun);
            this.CancelCommand = new RelayCommand(OnExecuteCancel, CanExecuteCancel);
        }

        private static bool TestCaseMatches(TestCaseViewModel testCase, SearchQuery searchQuery)
        {
            if (!testCase.DisplayName.Contains(searchQuery.SearchString))
            {
                return false;
            }

            switch (testCase.State)
            {
                case TestState.Passed:
                    return searchQuery.IncludePassedTests;

                case TestState.Skipped:
                    return searchQuery.IncludeSkippedTests;

                case TestState.Failed:
                    return searchQuery.IncludeFailedTests;

                case TestState.NotRun:
                    return true;

                default:
                    Debug.Assert(false, "What state is this test case in?");
                    return true;
            }
        }

        private void TestCases_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            MethodsCaption = $"Methods ({TestCases.Count})";
            MaximumProgress = TestCases.Count;
        }

        public ICommand ExitCommand { get; } = new RelayCommand(OnExecuteExit);
        public ICommand WindowLoadedCommand { get; }
        public RelayCommand RunCommand { get; }
        public RelayCommand CancelCommand { get; }

        public CommandBindingCollection CommandBindings { get; }

        private string methodsCaption;
        public string MethodsCaption
        {
            get { return methodsCaption; }
            private set { Set(ref methodsCaption, value); }
        }

        private int testsCompleted = 0;
        public int TestsCompleted
        {
            get { return testsCompleted; }
            set { Set(ref testsCompleted, value); }
        }

        private int testsPassed = 0;
        public int TestsPassed
        {
            get { return testsPassed; }
            set { Set(ref testsPassed, value); }
        }

        private int testsFailed = 0;
        public int TestsFailed
        {
            get { return testsFailed; }
            set { Set(ref testsFailed, value); }
        }

        private int testsSkipped = 0;
        public int TestsSkipped
        {
            get { return testsSkipped; }
            set { Set(ref testsSkipped, value); }
        }

        private int maximumProgress = int.MaxValue;
        public int MaximumProgress
        {
            get { return maximumProgress; }
            set { Set(ref maximumProgress, value); }
        }

        private TestState currentRunState;
        public TestState CurrentRunState
        {
            get { return currentRunState; }
            set { Set(ref currentRunState, value); }
        }

        private string output = string.Empty;
        public string Output
        {
            get { return output; }
            set { Set(ref output, value); }
        }

        public string FilterString
        {
            get { return searchQuery.SearchString; }
            set
            {
                if (Set(ref searchQuery.SearchString, value))
                {
                    FilterAfterDelay();
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
                        TestCases.FilterArgument = searchQuery;
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
        public FilteredCollectionView<TestCaseViewModel, SearchQuery> TestCases { get; }

        private async void OnExecuteOpen(object sender, ExecutedRoutedEventArgs e)
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
            await AddAssemblies(new[] { new AssemblyAndConfigFile(fileName, configFileName: null) });
        }

        private async Task AddAssemblies(IEnumerable<AssemblyAndConfigFile> assemblies)
        {
            var loadingDialog = new LoadingDialog { Owner = MainWindow.Instance };
            try
            {
                using (AssemblyHelper.SubscribeResolve())
                {
                    loadingDialog.Show();
                    foreach (var assembly in assemblies)
                    {
                        loadingDialog.AssemblyFileName = assembly.AssemblyFileName;

                        using (var xunit = new XunitFrontController(
                            useAppDomain: true,
                            assemblyFileName: assembly.AssemblyFileName,
                            configFileName: assembly.ConfigFileName,
                            diagnosticMessageSink: new DiagnosticMessageVisitor(),
                            shadowCopy: false))
                        using (var testDiscoveryVisitor = new TestDiscoveryVisitor(xunit))
                        {
                            await Task.Run(() =>
                            {
                                xunit.Find(includeSourceInformation: false, messageSink: testDiscoveryVisitor, discoveryOptions: TestFrameworkOptions.ForDiscovery());
                                testDiscoveryVisitor.Finished.WaitOne();
                            });

                            allTestCases.AddRange(testDiscoveryVisitor.TestCases);
                            Assemblies.Add(new TestAssemblyViewModel(assembly));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(Application.Current.MainWindow, ex.ToString());
            }
            finally
            {
                loadingDialog.Close();
            }
        }

        private class DiagnosticMessageVisitor : TestMessageVisitor
        {
            public override bool OnMessage(IMessageSinkMessage message)
            {
                return base.OnMessage(message);
            }
        }

        private class TestDiscoveryVisitor : TestMessageVisitor<IDiscoveryCompleteMessage>
        {
            ITestFrameworkDiscoverer discoverer;

            public List<TestCaseViewModel> TestCases { get; } = new List<TestCaseViewModel>();

            public TestDiscoveryVisitor(ITestFrameworkDiscoverer discoverer)
            {
                this.discoverer = discoverer;
            }

            public IDictionary<string, IList<string>> Traits { get; } = new Dictionary<string, IList<string>>();

            protected override bool Visit(ITestCaseDiscoveryMessage testCaseDiscovered)
            {
                var testCase = testCaseDiscovered.TestCase;
                TestCases.Add(new TestCaseViewModel(discoverer.Serialize(testCase), testCase.DisplayName, testCaseDiscovered.TestAssembly.Assembly.AssemblyPath));

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

        private class TestRunVisitor : TestMessageVisitor<ITestAssemblyFinished>
        {
            private readonly Func<bool> isCancelRequested;
            private readonly IEnumerable<TestCaseViewModel> testCases;

            public event EventHandler<TestStateEventArgs> TestFinished;

            public TestRunVisitor(IEnumerable<TestCaseViewModel> testCases, Func<bool> isCancelRequested)
            {
                this.testCases = testCases;
                this.isCancelRequested = isCancelRequested;
            }

            protected override bool Visit(ITestFailed testFailed)
            {
                var testCase = testCases.Single(tc => tc.DisplayName == testFailed.TestCase.DisplayName);
                testCase.State = TestState.Failed;
                var resultString = new StringBuilder(testFailed.TestCase.DisplayName);
                resultString.AppendLine(" FAILED:");
                for (int i = 0; i < testFailed.ExceptionTypes.Length; i++)
                {
                    resultString.AppendLine($"\tException type: '{testFailed.ExceptionTypes[i]}', number: '{i}', parent: '{testFailed.ExceptionParentIndices[i]}'");
                    resultString.AppendLine($"\tException message:");
                    resultString.AppendLine(testFailed.Messages[i]);
                    resultString.AppendLine($"\tException stacktrace");
                    resultString.AppendLine(testFailed.StackTraces[i]);
                }
                resultString.AppendLine();

                TestFinished?.Invoke(this, TestStateEventArgs.Failed(resultString.ToString()));
                return !isCancelRequested();
            }

            protected override bool Visit(ITestPassed testPassed)
            {
                var testCase = testCases.Single(tc => tc.DisplayName == testPassed.TestCase.DisplayName);
                testCase.State = TestState.Passed;
                TestFinished?.Invoke(this, TestStateEventArgs.Passed);
                return !isCancelRequested();
            }

            protected override bool Visit(ITestSkipped testSkipped)
            {
                var testCase = testCases.Single(tc => tc.DisplayName == testSkipped.TestCase.DisplayName);
                testCase.State = TestState.Skipped;
                TestFinished?.Invoke(this, TestStateEventArgs.Skipped);
                return !isCancelRequested();
            }
        }

        private bool IsBusy
        {
            get { return isBusy; }
            set
            {
                isBusy = value;
                RunCommand.RaiseCanExecuteChanged();
                CancelCommand.RaiseCanExecuteChanged();
            }
        }

        private bool IsCancelRequested
        {
            get { return isCancelRequested; }
            set
            {
                isCancelRequested = value;
                CancelCommand.RaiseCanExecuteChanged();
            }
        }

        private static void OnExecuteExit()
        {
            Application.Current.Shutdown();
        }

        private async void OnExecuteWindowLoaded()
        {
            try
            {
                IsBusy = true;
                await AddAssemblies(ParseCommandLine(Environment.GetCommandLineArgs().Skip(1)));
            }
            finally
            {
                IsBusy = false;
            }
        }

        private IEnumerable<AssemblyAndConfigFile> ParseCommandLine(IEnumerable<string> enumerable)
        {
            while (enumerable.Any())
            {
                var assemblyFileName = enumerable.First();
                enumerable = enumerable.Skip(1);

                var configFileName = (string)null;
                if (IsConfigFile(enumerable.FirstOrDefault()))
                {
                    configFileName = enumerable.First();
                    enumerable = enumerable.Skip(1);
                }

                yield return new AssemblyAndConfigFile(assemblyFileName, configFileName);
            }
        }

        private bool IsConfigFile(string fileName)
            => (fileName?.EndsWith(".config", StringComparison.OrdinalIgnoreCase) ?? false) ||
               (fileName?.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ?? false);

        private bool CanExecuteRun()
            => !IsBusy && TestCases.Any();

        private async void OnExecuteRun()
        {
            try
            {
                IsBusy = true;
                TestsCompleted = 0;
                TestsPassed = 0;
                TestsFailed = 0;
                TestsSkipped = 0;
                CurrentRunState = TestState.NotRun;
                Output = string.Empty;
                await Task.Run(() => RunTestsInBackground());
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            finally
            {
                IsBusy = false;
                IsCancelRequested = false;
            }
        }

        private void RunTestsInBackground()
        {
            foreach (var tc in TestCases)
            {
                tc.State = TestState.NotRun;
            }

            var selectedAssemblies = TestCases.ToLookup(tc => tc.AssemblyFileName);
            using (AssemblyHelper.SubscribeResolve())
            {
                foreach (var assembly in selectedAssemblies)
                {
                    using (var xunit = new XunitFrontController(
                        assemblyFileName: assembly.Key,
                        useAppDomain: true,
                        shadowCopy: false,
                        diagnosticMessageSink: new DiagnosticMessageVisitor()))
                    using (var testRunVisitor = new TestRunVisitor(allTestCases, () => IsCancelRequested))
                    {
                        testRunVisitor.TestFinished += TestRunVisitor_TestFinished;
                        xunit.RunTests(assembly.Select(tcvm => xunit.Deserialize(tcvm.TestCase)).ToArray(), testRunVisitor, TestFrameworkOptions.ForExecution());
                        testRunVisitor.Finished.WaitOne();
                    }
                }
            }
        }

        private void TestRunVisitor_TestFinished(object sender, TestStateEventArgs e)
        {
            TestsCompleted++;
            switch (e.State)
            {
                case TestState.Passed:
                    TestsPassed++;
                    break;
                case TestState.Failed:
                    TestsFailed++;
                    Output = Output + e.Results;
                    break;
                case TestState.Skipped:
                    TestsSkipped++;
                    break;
            }

            if (e.State > CurrentRunState)
            {
                CurrentRunState = e.State;
            }
        }

        private bool CanExecuteCancel() => IsBusy && !IsCancelRequested;

        private void OnExecuteCancel()
        {
            this.IsCancelRequested = true;
        }

        public bool IncludePassedTests
        {
            get { return searchQuery.IncludePassedTests; }
            set
            {
                if (Set(ref searchQuery.IncludePassedTests, value))
                {
                    FilterAfterDelay();
                }
            }
        }

        public bool IncludeFailedTests
        {
            get { return searchQuery.IncludeFailedTests; }
            set
            {
                if (Set(ref searchQuery.IncludeFailedTests, value))
                {
                    FilterAfterDelay();
                }
            }
        }

        public bool IncludeSkippedTests
        {
            get { return searchQuery.IncludeSkippedTests; }
            set
            {
                if (Set(ref searchQuery.IncludeSkippedTests, value))
                {
                    FilterAfterDelay();
                }
            }
        }
    }

    /// <summary>
    /// Note: More severe states are higher numbers.
    /// <see cref="MainViewModel.TestRunVisitor_TestFinished(object, TestStateEventArgs)"/>
    /// </summary>
    public enum TestState
    {
        NotRun = 1,
        Passed,
        Skipped,
        Failed,
    }

    public class TestComparer : IComparer<TestCaseViewModel>
    {
        public static TestComparer Instance { get; } = new TestComparer();

        public int Compare(TestCaseViewModel x, TestCaseViewModel y)
            => StringComparer.Ordinal.Compare(x.DisplayName, y.DisplayName);

        private TestComparer() { }
    }
}