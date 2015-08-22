using GalaSoft.MvvmLight;
using System.Windows.Input;
using System;
using System.Windows;
using GalaSoft.MvvmLight.CommandWpf;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO.Pipes;
using System.IO;
using System.Text;
using xunit.runner.data;
using System.Windows.Threading;

namespace xunit.runner.wpf.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private readonly ITestUtil testUtil;
        private readonly ObservableCollection<TestCaseViewModel> allTestCases = new ObservableCollection<TestCaseViewModel>();
        private CancellationTokenSource filterCancellationTokenSource = new CancellationTokenSource();

        private CancellationTokenSource cancellationTokenSource;
        private bool isBusy;
        private SearchQuery searchQuery = new SearchQuery();

        public MainViewModel()
        {
            if (IsInDesignMode)
            {
                this.Assemblies.Add(new TestAssemblyViewModel(new AssemblyAndConfigFile(@"C:\Code\TestAssembly.dll", null)));
            }

            CommandBindings = CreateCommandBindings();
            this.testUtil = new xunit.runner.wpf.Impl.RemoteTestUtil(Dispatcher.CurrentDispatcher);
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
            if (!assemblies.Any())
            {
                return;
            }

            var loadingDialog = new LoadingDialog { Owner = MainWindow.Instance };
            try
            {
                await ExecuteTestSessionOperation(() =>
                {
                    var testSessionList = new List<ITestSession>();
                    foreach (var assembly in assemblies)
                    {
                        var assemblyPath = assembly.AssemblyFileName;
                        var session = this.testUtil.Discover(assemblyPath, cancellationTokenSource.Token);
                        session.TestDiscovered += OnTestDiscovered;

                        testSessionList.Add(session);
                        Assemblies.Add(new TestAssemblyViewModel(assembly));
                    }

                    return testSessionList;
                });
            }
            finally
            {
                loadingDialog.Close();
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

        private static void OnExecuteExit()
        {
            Application.Current.Shutdown();
        }

        private async void OnExecuteWindowLoaded()
        {
            await AddAssemblies(ParseCommandLine(Environment.GetCommandLineArgs().Skip(1)));
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
            await ExecuteTestSessionOperation(RunTests);
        }

        private List<ITestSession> RunTests()
        {
            Debug.Assert(this.isBusy);
            Debug.Assert(this.cancellationTokenSource != null);

            TestsCompleted = 0;
            TestsPassed = 0;
            TestsFailed = 0;
            TestsSkipped = 0;
            CurrentRunState = TestState.NotRun;
            Output = string.Empty;

            foreach (var tc in TestCases)
            {
                tc.State = TestState.NotRun;
            }

            // TODO: Need a way to filter based on traits

            var runAll = TestCases.Count == this.allTestCases.Count;
            var testSessionList = new List<ITestSession>();

            foreach (var assemblyPath in TestCases.Select(x => x.AssemblyFileName).Distinct())
            {
                ITestRunSession session;
                if (runAll)
                {
                    session = this.testUtil.RunAll(assemblyPath, this.cancellationTokenSource.Token);
                }
                else
                {
                    var testCaseDisplayNames = TestCases
                        .Where(x => x.AssemblyFileName == assemblyPath)
                        .Select(x => x.DisplayName)
                        .ToImmutableArray();
                    session = this.testUtil.RunSpecific(assemblyPath, testCaseDisplayNames, this.cancellationTokenSource.Token);
                }

                session.TestFinished += OnTestFinished;
                testSessionList.Add(session);
            }

            return testSessionList;
        }

        private async Task ExecuteTestSessionOperation(Func<List<ITestSession>> operation)
        {
            Debug.Assert(!this.IsBusy);
            Debug.Assert(this.cancellationTokenSource == null);

            try
            {
                this.IsBusy = true;
                this.cancellationTokenSource = new CancellationTokenSource();

                var testSessionList = operation();
                await Task.WhenAll(testSessionList.Select(x => x.Task));
            }
            catch (Exception ex)
            {
                this.cancellationTokenSource?.Cancel();
                MessageBox.Show(Application.Current.MainWindow, ex.ToString());
            }
            finally
            {
                this.cancellationTokenSource = null;
                this.IsBusy = false;
            }
        }

        private void OnTestDiscovered(object sender, TestCaseDataEventArgs e)
        {
            var t = e.TestCaseData;
            allTestCases.Add(new TestCaseViewModel(t.SerializedForm, t.DisplayName, t.AssemblyPath));
        }

        private void OnTestFinished(object sender, TestResultDataEventArgs e)
        {
            var testCase = TestCases.Single(x => x.DisplayName == e.TestCaseDisplayName);
            testCase.State = e.TestState;

            TestsCompleted++;
            switch (e.TestState)
            {
                case TestState.Passed:
                    TestsPassed++;
                    break;
                case TestState.Failed:
                    TestsFailed++;
                    Output = Output + e.Output;
                    break;
                case TestState.Skipped:
                    TestsSkipped++;
                    break;
            }

            if (e.TestState > CurrentRunState)
            {
                CurrentRunState = e.TestState;
            }
        }

        private bool CanExecuteCancel()
        {
            return this.cancellationTokenSource != null && !this.cancellationTokenSource.IsCancellationRequested;
        }

        private void OnExecuteCancel()
        {
            Debug.Assert(CanExecuteCancel());
            this.cancellationTokenSource.Cancel();
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

    public class TestComparer : IComparer<TestCaseViewModel>
    {
        public static TestComparer Instance { get; } = new TestComparer();

        public int Compare(TestCaseViewModel x, TestCaseViewModel y)
            => StringComparer.Ordinal.Compare(x.DisplayName, y.DisplayName);

        private TestComparer() { }
    }
}
