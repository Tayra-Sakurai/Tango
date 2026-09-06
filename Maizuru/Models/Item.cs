// SPDX-LicenseCopyrightText: 2026 Tayra Sakurai
// SPDX-License-Identifier: GPL-3.0-or-later
using System;
using System.Collections.Generic;
using System.Text;

namespace Maizuru.Models
{
    public class Item
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTimeOffset DateTimeOffset { get; set; } = DateTimeOffset.Now;
        public double Expense { get; set; } = 0d;
        public double Income { get; set; } = 0d;
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
        public int PaymentMethodId { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }
        public float[] Vector { get; set; } = new float[Constants.DIMENSIONS];
    }
}
