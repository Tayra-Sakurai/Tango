// SPDX-FileCopyrighText: 2026 Tayra Sakurai
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Tango.TemplatedElements
{
    [TemplatePart(Name = "Box", Type = typeof(NumberBox))]
    [TemplatePart(Name = "Message", Type = typeof(TextBlock))]
    public sealed partial class ValidationNumberBox : Control
    {
        private TextBlock? messageBlock;
        private NumberBox? box;

        public ValidationNumberBox()
        {
            DefaultStyleKey = typeof(ValidationNumberBox);
            DataContextChanged += ValidationTextBox_DataContextChanged;
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            messageBlock = (TextBlock)GetTemplateChild("Message");
            box = (NumberBox)GetTemplateChild("Box");
            box.ValueChanged += Box_ValueChanged;
        }

        private void Box_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            Value = sender.Value;
        }

        private void ValidationTextBox_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (args.NewValue is INotifyDataErrorInfo notifyDataErrorInfo)
            {
                NotifyDataErrorInfo = notifyDataErrorInfo;
                ShowValidationResult();
            }
        }

        private static void OnNotifyDataErrorInfoPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is INotifyDataErrorInfo notifyDataErrorInfo)
                notifyDataErrorInfo.ErrorsChanged -= ((ValidationNumberBox)d).NotifyDataErrorInfo_ErrorsChanged;
            if (e.NewValue is INotifyDataErrorInfo notifyDataErrorInfo1)
            {
                notifyDataErrorInfo1.ErrorsChanged += ((ValidationNumberBox)d).NotifyDataErrorInfo_ErrorsChanged;
                ((ValidationNumberBox)d).ShowValidationResult();
                ((ValidationNumberBox)d).DataContextChanged -= ((ValidationNumberBox)d).ValidationTextBox_DataContextChanged;
            }
        }

        private void NotifyDataErrorInfo_ErrorsChanged(object? sender, DataErrorsChangedEventArgs args)
        {
            ShowValidationResult();
        }

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public static DependencyProperty ValueProperty { get; } = DependencyProperty.Register(
            nameof(Value),
            typeof(double),
            typeof(ValidationNumberBox),
            null);

        public string Header
        {
            get => (string)GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        public static DependencyProperty HeaderProperty { get; } = DependencyProperty.Register(
            nameof(Header),
            typeof(string),
            typeof(ValidationNumberBox),
            null);

        public string PlaceholderText
        {
            get => (string)GetValue(PlaceholderTextProperty);
            set => SetValue(PlaceholderTextProperty, value);
        }

        public static DependencyProperty PlaceholderTextProperty { get; } = DependencyProperty.Register(
            nameof(PlaceholderText),
            typeof(string),
            typeof(ValidationNumberBox),
            null);

        public string Property
        {
            get => (string)GetValue(PropertyProperty);
            set => SetValue(PropertyProperty, value);
        }

        private static readonly DependencyProperty PropertyProperty = DependencyProperty.Register(
            nameof(Property),
            typeof(string),
            typeof(ValidationNumberBox),
            new(string.Empty));

        public INotifyDataErrorInfo NotifyDataErrorInfo
        {
            get => (INotifyDataErrorInfo)GetValue(NotifyDataErrorInfoProperty);
            set => SetValue(NotifyDataErrorInfoProperty, value);
        }

        public static DependencyProperty NotifyDataErrorInfoProperty { get; } = DependencyProperty.Register(
            nameof(NotifyDataErrorInfo),
            typeof(INotifyDataErrorInfo),
            typeof(ValidationNumberBox),
            new(default, OnNotifyDataErrorInfoPropertyChanged));

        private void ShowValidationResult()
        {
            if (NotifyDataErrorInfo is not INotifyDataErrorInfo ||
                box is not NumberBox textBox ||
                messageBlock is not TextBlock textBlock ||
                Property is not string property)
                return;

            ValidationResult? validationResult = NotifyDataErrorInfo.GetErrors(property).OfType<ValidationResult>().FirstOrDefault();
            if (validationResult is not null)
            {
                messageBlock.Text = validationResult.ErrorMessage;

                if (App.Current.Resources.TryGetValue("SystemControlErrorTextForegroundBrush", out object obj) && obj is Brush brush)
                    textBox.BorderBrush = brush;
            }
            else
            {
                messageBlock.Text = string.Empty;

                if (App.Current.Resources.TryGetValue("TextBoxBorderThemeBrush", out object obj) && obj is Brush brush)
                    textBox.BorderBrush = brush;
            }
        }
    }
}
