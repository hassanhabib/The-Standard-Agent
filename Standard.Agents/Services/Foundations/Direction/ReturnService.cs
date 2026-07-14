namespace Standard.Agents.Services.Foundations.Direction;

// The dead-end foundation: no broker, no external resource. Its "resource" is the
// caller. Today it hands the payload back unchanged; later it can format, stream, or
// deliver over a channel. This is where Return grows.
public sealed class ReturnService : IReturnService
{
    public ValueTask<string> ReturnAsync(string payload) =>
        ValueTask.FromResult(payload);
}
