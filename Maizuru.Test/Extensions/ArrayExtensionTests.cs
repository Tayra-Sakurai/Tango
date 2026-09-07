// SPDX-LicenseCopyrightText: 2026 Tayra Sakurai
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using Maizuru.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Maizuru.Test.Extensions
{
    [TestClass]
    public class ArrayExtensionTests
    {
        [TestMethod]
        public void OperatorMultiply_OrthogonalVectors_ReturnsZero()
        {
            float[] a = [1.0f, 0.0f, 0.0f];
            float[] b = [0.0f, 1.0f, 0.0f];

            float result = a * b;

            Assert.AreEqual(0.0f, result, 1e-6f);
        }

        [TestMethod]
        public void OperatorMultiply_IdenticalUnitVectors_ReturnsOne()
        {
            float[] a = [1.0f, 0.0f];
            float[] b = [1.0f, 0.0f];

            float result = a * b;

            Assert.AreEqual(1.0f, result, 1e-6f);
        }

        [TestMethod]
        public void OperatorMultiply_ArbitraryVectors_ReturnsExpectedProduct()
        {
            float[] a = [1.0f, 2.0f, 3.0f];
            float[] b = [4.0f, -5.0f, 6.0f];
            // 1*4 + 2*(-5) + 3*6 = 4 - 10 + 18 = 12

            float result = a * b;

            Assert.AreEqual(12.0f, result, 1e-6f);
        }

        [TestMethod]
        public void OperatorMultiply_EmptyArrays_ReturnsZero()
        {
            float[] a = [];
            float[] b = [];

            float result = a * b;

            Assert.AreEqual(0.0f, result);
        }

        [TestMethod]
        public void OperatorMultiply_DifferentLengths_CalculatesZipLength()
        {
            float[] a = [2.0f, 3.0f, 5.0f];
            float[] b = [4.0f, 1.0f];
            // 2*4 + 3*1 = 11

            float result = a * b;

            Assert.AreEqual(11.0f, result, 1e-6f);
        }

        [TestMethod]
        public void OperatorMultiply_DoublePrecision_CalculatesCorrectly()
        {
            double[] a = [0.5, 1.5];
            double[] b = [2.0, 4.0];
            // 0.5*2.0 + 1.5*4.0 = 1.0 + 6.0 = 7.0

            double result = a * b;

            Assert.AreEqual(7.0, result, 1e-9);
        }

        [TestMethod]
        public void OperatorMultiply_Dimension768_CalculatesCorrectly()
        {
            float[] a = new float[Constants.DIMENSIONS];
            float[] b = new float[Constants.DIMENSIONS];

            for (int i = 0; i < Constants.DIMENSIONS; i++)
            {
                a[i] = 1.0f / (float)Math.Sqrt(Constants.DIMENSIONS);
                b[i] = 1.0f / (float)Math.Sqrt(Constants.DIMENSIONS);
            }

            float result = a * b;

            // Unit vector dot itself should equal 1.0
            Assert.AreEqual(1.0f, result, 1e-4f);
        }
    }
}
