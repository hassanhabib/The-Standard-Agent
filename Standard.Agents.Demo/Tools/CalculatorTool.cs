using System.Text.RegularExpressions;
using Standard.Agents.Tools;

namespace Standard.Agents.Demo.Tools;

// A demo-provided leaf tool. The framework supplies the ITool contract and the
// InternalTool/ToolBroker that runs it; the specific tool is the consumer's.
public sealed partial class CalculatorTool : ITool
{
    public string Name => "calculator";

    public ValueTask<string> ExecuteAsync(string input) =>
        ValueTask.FromResult(
            new NCalc.Expression(PromoteNumbers(input)).Evaluate()!.ToString()!);

    private static string PromoteNumbers(string expression) =>
        WholeNumberRegex().Replace(expression, "${0}.0");

    [GeneratedRegex(@"(?<![\d.])\d+(?![\d.])")]
    private static partial Regex WholeNumberRegex();
}
