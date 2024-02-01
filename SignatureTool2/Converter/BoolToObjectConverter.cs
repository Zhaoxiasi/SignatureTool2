using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SignatureTool2.Converter
{    public class BoolToObjectConverter : DependencyObject, IValueConverter
    {
        public static readonly DependencyProperty TrueValueProperty = DependencyProperty.RegisterAttached(nameof(TrueValue), typeof(object), typeof(BoolToObjectConverter));
        public static readonly DependencyProperty FalseValueProperty = DependencyProperty.RegisterAttached(nameof(FalseValue), typeof(object), typeof(BoolToObjectConverter));
        public static readonly DependencyProperty NullValueProperty = DependencyProperty.RegisterAttached(nameof(NullValue), typeof(object), typeof(BoolToObjectConverter), new PropertyMetadata(null));

        public object NullValue
        {
            get => GetValue(NullValueProperty);
            set => SetValue(NullValueProperty, value);
        }
        public object TrueValue
        {
            get => GetValue(TrueValueProperty);
            set => SetValue(TrueValueProperty, value);
        }
        public object FalseValue
        {
            get => GetValue(FalseValueProperty);
            set => SetValue(FalseValueProperty, value);
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is bool))
            {
                return NullValue;
            }
            return ((bool)value) ? TrueValue : FalseValue;

        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
