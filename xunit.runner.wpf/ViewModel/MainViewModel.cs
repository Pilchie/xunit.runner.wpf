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

        private bool isBusy;
        private bool isCancelRequested;
        public MainViewModel()
        {
            if (IsInDesignMode)
            {
                this.Assemblies.Add(new TestAssemblyViewModel(@"C:\Code\TestAssembly.dll"));
            }

            CommandBindings = CreateCommandBindings();
            this.testUtil = new xunit.runner.wpf.Impl.RemoteTestUtil();
            this.MethodsCaption = "Methods (0)";

            TestCases = new FilteredCollectionView<TestCaseViewModel, Tuple<string, TestState>>(
                allTestCases, TestCaseMatches, Tuple.Create(string.Empty, TestState.All), TestComparer.Instance);

            this.TestCases.CollectionChanged += TestCases_CollectionChanged;
            this.RunCommand = new RelayCommand(OnExecuteRun, CanExecuteRun);
            this.CancelCommand = new RelayCommand(OnExecuteCancel, CanExecuteCancel);
        }

        private static bool TestCaseMatches(TestCaseViewModel testCase, Tuple<string, TestState> filterTextAndTestState)
            => testCase.DisplayName.Contains(filterTextAndTestState.Item1) && (testCase.State & filterTextAndTestState.Item2) == filterTextAndTestState.Item2;

        private void TestCases_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            MethodsCaption = $"Methods ({TestCases.Count})";
            MaximumProgress = TestCases.Count;
        }

        public ICommand ExitCommand { get; } = new RelayCommand(OnExecuteExit);
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
                var list = this.testUtil.Discover(fileName);
                allTestCases.AddRange(list);
                Assemblies.Add(new TestAssemblyViewModel(fileName));
            }
            catch (Exception ex)
            {
                MessageBox.Show(Application.Current.MainWindow, ex.ToString());
            }
        }

        private class DiagnosticMessageVisitor : TestMessageVisitor
        {
            public override bool OnMessage(IMessageSinkMessage message)
            {
                return base.OnMessage(message);
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

        private bool CanExecuteRun()
            => !IsBusy && TestCases.Any();

        private void OnExecuteRun()
        {
            try
            {
                IsBusy = true;
                TestsCompleted = 0;
                TestsPassed = 0;
                TestsFailed = 0;
                TestsSkipped = 0;
                CurrentRunState = TestState.NotRun;
                RunTests();
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

        private void RunTests()
        {
            foreach (var tc in TestCases)
            {
                tc.State = TestState.NotRun;
            }

            // Hacky way of using one assembly for now.  Will expand later. 
            var assemblyPath = TestCases.Select(x => x.AssemblyFileName).First();
            var session = this.testUtil.Run(Dispatcher.CurrentDispatcher, assemblyPath);
            session.TestFinished += OnTestFinished;
        }

        private void OnTestFinished(object sender, TestResultEventArgs e)
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

        private bool CanExecuteCancel() => IsBusy && !IsCancelRequested;

        private void OnExecuteCancel()
        {
            this.IsCancelRequested = true;
        }

        public bool IsPassedFilterChecked
        {
            get { return ResultFilter == TestState.Passed; }
            set { UpdateFilter(value, TestState.Passed); }
        }

        public bool IsFailedFilterChecked
        {
            get { return ResultFilter == TestState.Failed; }
            set { UpdateFilter(value, TestState.Failed); }
        }

        public bool IsSkippedFilterChecked
        {
            get { return ResultFilter == TestState.Skipped; }
            set { UpdateFilter(value, TestState.Skipped); }
        }

        private void UpdateFilter(bool value, TestState newState)
        {
            if (value && ResultFilter != newState)
            {
                ResultFilter = newState;
                RaisePropertyChanged(nameof(IsPassedFilterChecked));
                RaisePropertyChanged(nameof(IsFailedFilterChecked));
                RaisePropertyChanged(nameof(IsSkippedFilterChecked));
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