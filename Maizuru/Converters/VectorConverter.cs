// SPDX-LicenseCopyrightText: 2026 Tayra Sakurai
// SPDX-License-Identifier: GPL-3.0-or-later
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Maizuru.Converters
{
    public class VectorConverter : ValueConverter<float[], string>
    {
        public VectorConverter()
            : base(
                  v => JsonSerializer.Serialize(v),
                  s => JsonSerializer.Deserialize<float[]>(s) ?? new float[Constants.DIMENSIONS])
        { }
    }
}
