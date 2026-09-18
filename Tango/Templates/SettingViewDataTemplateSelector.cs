// SPDX-FileCopyrighText: 2026 Tayra Sakurai
// SPDX-License-Identifier: GPL-3.0-or-later

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Text;

namespace Tango.Templates
{
    public class SettingViewDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate BoolTemplate { get; set; }
        public DataTemplate TimeSpanTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            if (item is SettingDataObjViewModel settingDataObjViewModel)
            {
                if (item is SettingDataBoolViewModel settingDataBoolViewModel)
                    return BoolTemplate;

                if (item is SettingDataTimeSpanViewModel settingDataTimeSpanViewModel)
                    return TimeSpanTemplate;
            }

            throw new NotImplementedException();
        }
    }
}
