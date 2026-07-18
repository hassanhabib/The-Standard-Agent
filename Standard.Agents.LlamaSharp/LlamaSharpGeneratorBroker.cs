// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Runtime.CompilerServices;
using System.Text;
using LLama;
using LLama.Common;
using LLama.Sampling;
using Standard.Agents.Brokers.Generators;

namespace Standard.Agents.LlamaSharp;

public sealed class LlamaSharpGeneratorBroker : IGeneratorBroker, IDisposable
{
    private readonly LLamaWeights weights;
    private readonly StatelessExecutor executor;
    private readonly int maxTokens;
    private readonly Func<string, string, string> formatPrompt;

    // The prompt template is the thing to get right per model: an instruct model only
    // generates when its own chat format is used. Defaults to ChatML (the most common
    // modern format); pass PromptTemplates.Plain, or your own (system, user) => prompt,
    // to match a different model. There is no hard-coded anti-prompt — generation stops
    // at the model's own end-of-turn / EOS token.
    public LlamaSharpGeneratorBroker(
        string modelPath,
        int contextSize = 4096,
        int gpuLayerCount = 0,
        int maxTokens = 1024,
        Func<string, string, string>? promptTemplate = null)
    {
        var parameters = new ModelParams(modelPath)
        {
            ContextSize = (uint)contextSize,
            GpuLayerCount = gpuLayerCount
        };

        this.weights = LLamaWeights.LoadFromFile(parameters);
        this.executor = new StatelessExecutor(this.weights, parameters);
        this.maxTokens = maxTokens;
        this.formatPrompt = promptTemplate ?? PromptTemplates.ChatML;
    }

    public async ValueTask<string> GenerateAsync(string systemPrompt, string userPrompt)
    {
        var reply = new StringBuilder();

        await foreach (string token in GenerateStreamAsync(systemPrompt, userPrompt))
        {
            reply.Append(token);
        }

        return reply.ToString();
    }

    public async IAsyncEnumerable<string> GenerateStreamAsync(
        string systemPrompt,
        string userPrompt,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var inferenceParams = new InferenceParams
        {
            MaxTokens = this.maxTokens,
            SamplingPipeline = new DefaultSamplingPipeline()
        };

        IAsyncEnumerable<string> tokens = this.executor.InferAsync(
            this.formatPrompt(systemPrompt, userPrompt),
            inferenceParams,
            cancellationToken);

        await foreach (string token in tokens.WithCancellation(cancellationToken))
        {
            yield return token;
        }
    }

    public void Dispose() =>
        this.weights.Dispose();
}
