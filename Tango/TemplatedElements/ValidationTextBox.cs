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
    [TemplatePart(Name = "Box", Type = typeof(TextBox))]
    [TemplatePart(Name = "Message", Type = typeof(TextBlock))]
    public sealed partial class ValidationTextBox : Control
    {
        private TextBlock? messageBlock;
        private TextBox? box;

        public ValidationTextBox()
        {
            DefaultStyleKey = typeof(ValidationTextBox);
            DataContextChanged += ValidationTextBox_DataContextChanged;
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            messageBlock = (TextBlock)GetTemplateChild("Message");
            box = (TextBox)GetTemplateChild("Box");
            box.TextChanged += Box_TextChanged;
        }

        private void Box_TextChanged(object sender, TextChangedEventArgs e)
        {
            Text = ((TextBox)sender).Text;
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
                notifyDataErrorInfo.ErrorsChanged -= ((ValidationTextBox)d).NotifyDataErrorInfo_ErrorsChanged;
            if (e.NewValue is INotifyDataErrorInfo notifyDataErrorInfo1)
            {
                notifyDataErrorInfo1.ErrorsChanged += ((ValidationTextBox)d).NotifyDataErrorInfo_ErrorsChanged;
                ((ValidationTextBox)d).ShowValidationResult();
                ((ValidationTextBox)d).DataContextChanged -= ((ValidationTextBox)d).ValidationTextBox_DataContextChanged;
            }
        }

        private void NotifyDataErrorInfo_ErrorsChanged(object? sender, DataErrorsChangedEventArgs args)
        {
            ShowValidationResult();
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public static DependencyProperty TextProperty { get; } = DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(ValidationTextBox),
            null);

        public string Header
        {
            get => (string)GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        public static DependencyProperty HeaderProperty { get; } = DependencyProperty.Register(
            nameof(Header),
            typeof(string),
            typeof(ValidationTextBox),
            null);

        public string PlaceholderText
        {
            get => (string)GetValue(PlaceholderTextProperty);
            set => SetValue(PlaceholderTextProperty, value);
        }

        public static DependencyProperty PlaceholderTextProperty { get; } = DependencyProperty.Register(
            nameof(PlaceholderText),
            typeof(string),
            typeof(ValidationTextBox),
            null);

        public string Property
        {
            get => (string)GetValue(PropertyProperty);
            set => SetValue(PropertyProperty, value);
        }

        private static readonly DependencyProperty PropertyProperty = DependencyProperty.Register(
            nameof(Property),
            typeof(string),
            typeof(ValidationTextBox),
            new(string.Empty));

        public INotifyDataErrorInfo NotifyDataErrorInfo
        {
            get => (INotifyDataErrorInfo)GetValue(NotifyDataErrorInfoProperty);
            set => SetValue(NotifyDataErrorInfoProperty, value);
        }

        public static DependencyProperty NotifyDataErrorInfoProperty { get; } = DependencyProperty.Register(
            nameof(NotifyDataErrorInfo),
            typeof(INotifyDataErrorInfo),
            typeof(ValidationTextBox),
            new(default, OnNotifyDataErrorInfoPropertyChanged));

        public bool AcceptsReturn
        {
            get => (bool)GetValue(AcceptsReturnProperty);
            set => SetValue(AcceptsReturnProperty, value);
        }

        private readonly static DependencyProperty AcceptsReturnProperty = DependencyProperty.Register(
            nameof(AcceptsReturn),
            typeof(bool),
            typeof(ValidationTextBox),
            new(false));

        private void ShowValidationResult()
        {
            if (NotifyDataErrorInfo is not INotifyDataErrorInfo ||
                box is not TextBox textBox ||
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
