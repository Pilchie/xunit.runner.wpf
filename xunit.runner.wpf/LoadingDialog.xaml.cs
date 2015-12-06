using System.Windows;

namespace Xunit.Runner.Wpf
{
    public partial class LoadingDialog : Window
    {
        public LoadingDialog()
        {
            InitializeComponent();
        }

        public string AssemblyFileName
        {
            get { return (string)GetValue(AssemblyFileNameProperty); }
            set { SetValue(AssemblyFileNameProperty, value); }
        }

        public static readonly DependencyProperty AssemblyFileNameProperty =
            DependencyProperty.Register(nameof(AssemblyFileName), typeof(string), typeof(LoadingDialog));
    }
}
