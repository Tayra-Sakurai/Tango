// SPDX-LicenseCopyrightText: 2026 Tayra Sakurai
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Linq;
using System.Text.Json;
using Maizuru.Converters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Maizuru.Test.Converters
{
    [TestClass]
    public class VectorConverterTests
    {
        private VectorConverter converter = null!;

        [TestInitialize]
        public void Initialize()
        {
            converter = new VectorConverter();
        }

        [TestMethod]
        public void ConvertToProvider_SerializesFloatArrayToJson()
        {
            float[] vector = [1.5f, -2.5f, 3.0f];
            Func<float[], string> toProvider = converter.ConvertToProviderExpression.Compile();

            string json = toProvider(vector);

            CollectionAssert.AreEqual(vector, JsonSerializer.Deserialize<float[]>(json));
        }

        [TestMethod]
        public void ConvertFromProvider_DeserializesJsonToFloatArray()
        {
            string json = "[0.1,0.2,0.3]";
            Func<string, float[]> fromProvider = converter.ConvertFromProviderExpression.Compile();

            float[] result = fromProvider(json);

            Assert.AreEqual(3, result.Length);
            Assert.AreEqual(0.1f, result[0], 1e-6f);
            Assert.AreEqual(0.2f, result[1], 1e-6f);
            Assert.AreEqual(0.3f, result[2], 1e-6f);
        }

        [TestMethod]
        public void ConvertFromProvider_NullJson_ReturnsDimensionsZeroArray()
        {
            Func<string, float[]> fromProvider = converter.ConvertFromProviderExpression.Compile();

            string json = "null";
            float[] result = fromProvider(json);

            Assert.IsNotNull(result);
            Assert.AreEqual(Constants.DIMENSIONS, result.Length);
            Assert.IsTrue(result.All(v => v == 0.0f));
        }

        [TestMethod]
        public void Roundtrip_FullDimensionVector_PreservesValues()
        {
            float[] original = new float[Constants.DIMENSIONS];
            for (int i = 0; i < Constants.DIMENSIONS; i++)
            {
                original[i] = (float)Math.Sin(i);
            }

            Func<float[], string> toProvider = converter.ConvertToProviderExpression.Compile();
            Func<string, float[]> fromProvider = converter.ConvertFromProviderExpression.Compile();

            string json = toProvider(original);
            float[] restored = fromProvider(json);

            Assert.AreEqual(Constants.DIMENSIONS, restored.Length);
            for (int i = 0; i < Constants.DIMENSIONS; i++)
            {
                Assert.AreEqual(original[i], restored[i], 1e-6f);
            }
        }
    }
}
