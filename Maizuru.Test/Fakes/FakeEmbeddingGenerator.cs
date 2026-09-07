// SPDX-LicenseCopyrightText: 2026 Tayra Sakurai
// SPDX-License-Identifier: GPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;

namespace Maizuru.Test.Fakes
{
    public class FakeEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
    {
        public EmbeddingGeneratorMetadata Metadata { get; } = new("FakeEmbeddingGenerator");

        public Func<string, EmbeddingGenerationOptions?, float[]>? GeneratorFunc { get; set; }

        public List<(string Prompt, EmbeddingGenerationOptions? Options)> Invocations { get; } = [];

        public float[] DefaultVector { get; set; } = new float[Constants.DIMENSIONS];

        public FakeEmbeddingGenerator()
        {
        }

        public FakeEmbeddingGenerator(Func<string, EmbeddingGenerationOptions?, float[]> generatorFunc)
        {
            GeneratorFunc = generatorFunc;
        }

        public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
            IEnumerable<string> values,
            EmbeddingGenerationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            List<Embedding<float>> embeddings = [];
            foreach (string value in values)
            {
                Invocations.Add((value, options));
                float[] vector = GeneratorFunc != null ? GeneratorFunc(value, options) : DefaultVector;
                embeddings.Add(new Embedding<float>(vector));
            }

            GeneratedEmbeddings<Embedding<float>> result = new(embeddings);
            return Task.FromResult(result);
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose()
        {
        }
    }
}
