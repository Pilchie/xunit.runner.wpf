using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace xunit.runner.wpf
{
    /// <summary>
    /// Interaction logic for LoadingDialog.xaml
    /// </summary>
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
