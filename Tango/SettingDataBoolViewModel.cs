// SPDX-FileCopyrighText: 2026 Tayra Sakurai
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Text;

namespace Tango
{
    public class SettingDataBoolViewModel : SettingDataObjViewModel
    {
        public new bool Value { get; set; }
        public string? OnLabel { get; set; }
        public string? OffLabel { get; set; }
    }
}
