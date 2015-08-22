using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using xunit.runner.data;
using xunit.runner.wpf.ViewModel;

namespace xunit.runner.wpf.Converters
{
    public class TestStateConverter : IValueConverter
    {
        private static ImageSource passedSource;
        private static ImageSource failedSource;
        private static ImageSource skippedSource;
        static TestStateConverter()
        {
            passedSource = LoadResourceImage("Passed.ico");
            failedSource = LoadResourceImage("Failed.ico");
            skippedSource = LoadResourceImage("Skipped.ico");
        }

        private static BitmapImage LoadResourceImage(string resourceName)
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri("pack://application:,,,/xunit.runner.wpf;component/Artwork/" + resourceName);
            image.EndInit();
            return image;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var state = (TestState)value;
            if (targetType == typeof(Brush))
            {
                switch (state)
                {
                    case TestState.Failed:
                        return Brushes.Red;
                    case TestState.Skipped:
                        return Brushes.Yellow;
                    case TestState.Passed:
                        return Brushes.Green;
                    default:
                        return Brushes.Gray;
                }
            }
            else if (targetType == typeof(ImageSource))
            {
                switch (state)
                {
                    case TestState.Failed:
                        return failedSource;
                    case TestState.Skipped:
                        return skippedSource;
                    case TestState.Passed:
                        return passedSource;
                    default:
                        return null;
                }
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
