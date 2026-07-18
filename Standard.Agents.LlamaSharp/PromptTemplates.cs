// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace Standard.Agents.LlamaSharp;

public static class PromptTemplates
{
    // ChatML — Qwen and many modern instruct GGUFs. A good first guess.
    public static string ChatML(string systemPrompt, string userPrompt)
    {
        string system = string.IsNullOrWhiteSpace(systemPrompt)
            ? string.Empty
            : $"<|im_start|>system\n{systemPrompt}<|im_end|>\n";

        return $"{system}<|im_start|>user\n{userPrompt}<|im_end|>\n<|im_start|>assistant\n";
    }

    // Llama 3 instruct.
    public static string Llama3(string systemPrompt, string userPrompt)
    {
        string system = string.IsNullOrWhiteSpace(systemPrompt)
            ? string.Empty
            : $"<|start_header_id|>system<|end_header_id|>\n\n{systemPrompt}<|eot_id|>";

        return "<|begin_of_text|>" + system
            + $"<|start_header_id|>user<|end_header_id|>\n\n{userPrompt}<|eot_id|>"
            + "<|start_header_id|>assistant<|end_header_id|>\n\n";
    }

    // NVIDIA Nemotron — the <extra_id_0>/<extra_id_1> sentinel format.
    public static string Nemotron(string systemPrompt, string userPrompt) =>
        $"<extra_id_0>System\n{systemPrompt}\n"
            + $"<extra_id_1>User\n{userPrompt}\n<extra_id_1>Assistant\n";

    // Plain — base / completion models with no chat format.
    public static string Plain(string systemPrompt, string userPrompt)
    {
        string system = string.IsNullOrWhiteSpace(systemPrompt)
            ? string.Empty
            : systemPrompt + "\n\n";

        return $"{system}User: {userPrompt}\nAssistant: ";
    }
}
