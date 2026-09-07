// SPDX-FileCopyrighText: 2026 Tayra Sakurai
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tango
{
    internal class PageLinkItem
    {
        internal required string ResourceName { private get; init; }
        internal required ICollection<Type> PageTypes { get; init; }
        internal required IconElement Icon { get; init; }
        internal string Header
        {
            get
            {
                ResourceLoader resourceLoader = new();
                return resourceLoader.GetString($"{ResourceName}/Header");
            }
        }
        internal string LinkText
        {
            get
            {
                ResourceLoader resourceLoader = new();
                return resourceLoader.GetString($"{ResourceName}/Content");
            }
        }
    }
}
