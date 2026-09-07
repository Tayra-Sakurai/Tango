// SPDX-LicenseCopyrightText: 2026 Tayra Sakurai
// SPDX-License-Identifier: GPL-3.0-or-later
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Tango
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private readonly PageLinkItem[] pageLinkItems;

        public MainWindow()
        {
            InitializeComponent();
            pageLinkItems = GetPageLinkItems();
            Activated += MainWindow_Activated;
            BaseNavigation.ItemInvoked += BaseNavigation_ItemInvoked;
            MainFrame.Navigated += MainFrame_Navigated;
            BaseNavigation.BackRequested += BaseNavigation_BackRequested;
        }

        private void BaseNavigation_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
        {
            if (MainFrame.CanGoBack)
                MainFrame.GoBack();
        }

        private void MainFrame_Navigated(object sender, NavigationEventArgs e)
        {
            Frame frame = (Frame)sender;

            foreach (
                PageLinkItem item in
                pageLinkItems)
            {
                if (item.PageTypes.Contains(frame.SourcePageType))
                {
                    BaseNavigation.Header = item.Header;
                    break;
                }
            }

            BaseNavigation.IsBackEnabled = frame.CanGoBack;
        }

        private void BaseNavigation_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            PageLinkItem invokedItem = pageLinkItems.Single(e => e.LinkText == (string)args.InvokedItem);
            MainFrame.Navigate(invokedItem.PageTypes.First());
        }

        private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (MainFrame.SourcePageType is null)
            {
                MainFrame.Navigate(pageLinkItems.First().PageTypes.First());
            }
        }

        private static PageLinkItem[] GetPageLinkItems()
        {
            return [
                new()
                {
                    ResourceName = "CategoryLink",
                    PageTypes = [
                        typeof(CategoriesViewPage),
                        typeof(CategoryViewPage),
                    ],
                    Icon = new FontIcon
                    {
                        Glyph = "\uED41",
                    },
                },
                new()
                {
                    ResourceName = "ItemLink",
                    PageTypes = [
                        typeof(ItemsViewPage),
                        typeof(ItemViewPage),
                    ],
                    Icon = new FontIcon
                    {
                        Glyph = "\uE8C3",
                    },
                },
                new()
                {
                    ResourceName = "PaymentMethodLink",
                    PageTypes = [
                        typeof(PaymentMethodsViewPage),
                        typeof(PaymentMethodViewPage),
                    ],
                    Icon = new FontIcon
                    {
                        Glyph = "\uEBC7",
                    },
                }];
        }
    }
}
