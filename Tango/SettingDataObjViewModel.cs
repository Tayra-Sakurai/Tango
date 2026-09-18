// SPDX-FileCopyrighText: 2026 Tayra Sakurai
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tango
{
    public class SettingDataObjViewModel
    {
        public string Name { get; set; } = string.Empty;
        public object? Value { get; set; }
        public string Description { get; set; } = string.Empty;
        public IconElement? Icon { get; set; }
    }
}
