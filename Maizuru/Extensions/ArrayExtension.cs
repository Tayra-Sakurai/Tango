// SPDX-FileCopyrighText: 2026 Tayra Sakurai
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Maizuru.Extensions
{
    public static class ArrayExtension
    {
        extension<TValue>(TValue[])
            where TValue : IFloatingPoint<TValue>
        {
            /// <summary>
            /// Gets the inner product of the two n-dimentional vectors.
            /// </summary>
            /// <param name="array1">The first vector.</param>
            /// <param name="array2">The second vector.</param>
            /// <returns>The inner product.</returns>
            public static TValue operator *(TValue[] array1, TValue[] array2)
            {
                TValue product = TValue.Zero;

                foreach ((TValue first, TValue second) in array1.Zip(array2))
                    product += first * second;

                return product;
            }
        }
    }
}
