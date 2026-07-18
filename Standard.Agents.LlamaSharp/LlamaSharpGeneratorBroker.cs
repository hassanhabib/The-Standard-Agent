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
    private const string AntiPrompt = "User:";

    private readonly LLamaWeights weights;
    private readonly StatelessExecutor executor;
    private readonly int maxTokens;

    public LlamaSharpGeneratorBroker(
        string modelPath,
        int contextSize = 4096,
        int gpuLayerCount = 0,
        int maxTokens = 1024)
    {
        var parameters = new ModelParams(modelPath)
        {
            ContextSize = (uint)contextSize,
            GpuLayerCount = gpuLayerCount
        };

        this.weights = LLamaWeights.LoadFromFile(parameters);
        this.executor = new StatelessExecutor(this.weights, parameters);
        this.maxTokens = maxTokens;
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
            AntiPrompts = [AntiPrompt],
            SamplingPipeline = new DefaultSamplingPipeline()
        };

        IAsyncEnumerable<string> tokens = this.executor.InferAsync(
            BuildPrompt(systemPrompt, userPrompt),
            inferenceParams,
            cancellationToken);

        await foreach (string token in tokens.WithCancellation(cancellationToken))
        {
            yield return token;
        }
    }

    private static string BuildPrompt(string systemPrompt, string userPrompt)
    {
        var prompt = new StringBuilder();

        if (string.IsNullOrWhiteSpace(systemPrompt) is false)
        {
            prompt.AppendLine(systemPrompt).AppendLine();
        }

        prompt.Append("User: ").AppendLine(userPrompt).Append("Assistant: ");

        return prompt.ToString();
    }

    public void Dispose() =>
        this.weights.Dispose();
}
