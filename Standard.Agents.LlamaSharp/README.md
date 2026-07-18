# Standard.Agents.LlamaSharp

An optional **in-process local-inference brain** for [Standard.Agents](https://www.nuget.org/packages/Standard.Agents),
backed by [LLamaSharp](https://github.com/SciSharp/LLamaSharp) (llama.cpp). Run GGUF models locally
— no HTTP, no API key. The core `Standard.Agents` package stays dependency-free; the native weight
lives only here.

```bash
dotnet add package Standard.Agents.LlamaSharp
dotnet add package LLamaSharp.Backend.Cpu     # or .Cuda12 / .Vulkan for GPU
```

```csharp
using Standard.Agents;
using Standard.Agents.LlamaSharp;

var agent = new StandardAgent()
    .UseGenerator(new LlamaSharpGeneratorBroker("path/to/model.gguf"));

string answer = await agent.ProcessPromptAsync("What is 47 * 89?");
```

`LlamaSharpGeneratorBroker` implements the brain's `IGeneratorBroker` (both `GenerateAsync` and
streaming `GenerateStreamAsync`), so everything else — skills, tools, guardians, memory, knowledge,
streaming — works exactly as with a hosted brain. Constructor options: `contextSize`,
`gpuLayerCount` (0 = CPU), `maxTokens`.

You supply the `.gguf` model and a backend package; llama.cpp does the rest, entirely on your
machine.

## Choosing the prompt template (important)

An instruct model only generates when it's given **its own chat format**. If generation comes
back **empty**, the template is almost always the reason. The default is `ChatML` (Qwen and many
modern GGUFs); switch it to match your model:

```csharp
using Standard.Agents.LlamaSharp;

// ChatML (default) — Qwen, many modern instruct models
new LlamaSharpGeneratorBroker("model.gguf");

// Llama 3 instruct
new LlamaSharpGeneratorBroker("model.gguf", promptTemplate: PromptTemplates.Llama3);

// Base / completion model
new LlamaSharpGeneratorBroker("model.gguf", promptTemplate: PromptTemplates.Plain);

// Your own — any (systemPrompt, userPrompt) => prompt
new LlamaSharpGeneratorBroker("model.gguf",
    promptTemplate: (system, user) => $"<s>[INST] {system}\n{user} [/INST]");   // Mistral
```

## Backend setup & troubleshooting

`RuntimeError: The native library cannot be correctly loaded` is a **backend** problem, not a
model or adapter one:

- **Exactly one backend** — never install `LLamaSharp.Backend.Cpu` *and* `.Cuda12` together; they
  fight over the native `llama` library. CPU-only box → keep only `.Cpu`.
- **Put the backend in the app that runs**, not just a class library — a `Backend.*` reference on
  a library doesn't reliably copy its native DLLs into the executable's output. Verify `llama.dll`
  / `ggml*.dll` sit next to your `.exe`.
- **Match versions** — `LLamaSharp` and the `Backend` must be the same version.
- Force CPU and see the load attempts, at the top of `Main`:
  ```csharp
  LLama.Native.NativeLibraryConfig.All
      .WithCuda(false)
      .WithLogCallback((level, msg) => Console.Write(msg));
  ```
