using System.Windows;
using System.Windows.Input;

namespace Xunit.Runner.Wpf
{
    class CommandBindings
    {
        public static DependencyProperty Registration = DependencyProperty.RegisterAttached(
            nameof(Registration), typeof(CommandBindingCollection), typeof(CommandBindings), new PropertyMetadata(null, OnRegistrationChanged));

        public static void SetRegistration(UIElement element, CommandBindingCollection value)
        {
            if (element != null)
            {
                element.SetValue(Registration, value);
            }
        }

        public static CommandBindingCollection GetRegistration(UIElement element)
            => (element != null ? (CommandBindingCollection)element.GetValue(Registration) : null);

        private static void OnRegistrationChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            UIElement element = sender as UIElement;
            if (element != null)
            {
                CommandBindingCollection bindings = e.NewValue as CommandBindingCollection;
                if (bindings != null)
                {
                    element.CommandBindings.AddRange(bindings);
                }
            }
        }
    }
}
