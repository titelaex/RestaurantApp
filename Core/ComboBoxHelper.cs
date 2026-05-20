using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RestaurantApp.Core
{
    public static class ComboBoxHelper
    {
        public static readonly DependencyProperty SelectionChangedCommandProperty =
            DependencyProperty.RegisterAttached("SelectionChangedCommand", typeof(ICommand), typeof(ComboBoxHelper),
                new PropertyMetadata(null, OnSelectionChangedCommandChanged));

        public static void SetSelectionChangedCommand(DependencyObject dp, ICommand value) => dp.SetValue(SelectionChangedCommandProperty, value);
        public static ICommand GetSelectionChangedCommand(DependencyObject dp) => (ICommand)dp.GetValue(SelectionChangedCommandProperty);

        private static void OnSelectionChangedCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ComboBox comboBox)
            {
                comboBox.SelectionChanged -= ComboBox_SelectionChanged;
                if (e.NewValue != null)
                {
                    comboBox.SelectionChanged += ComboBox_SelectionChanged;
                }
            }
        }

        private static void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.RemovedItems.Count > 0 && sender is ComboBox comboBox)
            {
                ICommand command = GetSelectionChangedCommand(comboBox);
                if (command != null)
                {
                    command.Execute(comboBox.DataContext);
                }
            }
        }
    }
}