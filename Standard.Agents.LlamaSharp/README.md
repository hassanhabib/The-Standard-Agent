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
