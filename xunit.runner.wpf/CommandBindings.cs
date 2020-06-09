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

        public static CommandBindingCollection? GetRegistration(UIElement element)
            => (element != null ? (CommandBindingCollection)element.GetValue(Registration) : null);

        private static void OnRegistrationChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is UIElement element)
            {
                if (e.NewValue is CommandBindingCollection bindings)
                {
                    element.CommandBindings.AddRange(bindings);
                }
            }
        }
    }
}
