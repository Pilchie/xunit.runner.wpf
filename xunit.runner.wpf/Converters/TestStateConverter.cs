using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using xunit.runner.data;

namespace xunit.runner.wpf.Converters
{
    public class TestStateConverter : IValueConverter
    {
        private static ImageSource failedSource;
        private static ImageSource passedSource;
        private static ImageSource skippedSource;

        static TestStateConverter()
        {
            failedSource = LoadResourceImage("Failed_small.png");
            passedSource = LoadResourceImage("Passed_small.png");
            skippedSource = LoadResourceImage("Skipped_small.png");
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
                    case TestState.Passed:
                        return Brushes.Green;
                    case TestState.Skipped:
                        return Brushes.Yellow;
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
                    case TestState.Passed:
                        return passedSource;
                    case TestState.Skipped:
                        return skippedSource;
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
