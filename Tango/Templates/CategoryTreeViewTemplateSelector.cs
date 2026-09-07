// SPDX-FileCopyrighText: 2026 Tayra Sakurai
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tango.Templates
{
    public class CategoryTreeViewTemplateSelector : DataTemplateSelector
    {
        public DataTemplate BaseTemplate { get; set; }
        public DataTemplate NullTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            if (item is null)
                return NullTemplate;

            return BaseTemplate;
        }
    }
}
