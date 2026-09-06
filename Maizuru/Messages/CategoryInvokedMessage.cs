// SPDX-LicenseCopyrightText: 2026 Tayra Sakurai
// SPDX-License-Identifier: GPL-3.0-or-later
using CommunityToolkit.Mvvm.Messaging.Messages;
using Maizuru.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Maizuru.Messages
{
    public class CategoryInvokedMessage : ValueChangedMessage<Category>
    {
        public CategoryInvokedMessage(Category value)
            : base(value)
        {
        }
    }
}
