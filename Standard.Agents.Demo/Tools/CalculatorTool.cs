// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Text.RegularExpressions;
using Standard.Agents.Tools;

namespace Standard.Agents.Demo.Tools;

// A leaf tool the consumer supplies. The library owns the ITool contract and the
// InternalTool/ToolBroker that runs it; what the tool DOES is never the library's
// business.
public sealed partial class CalculatorTool : ITool
{
    public string Name => "calculator";

    public ValueTask<string> ExecuteAsync(string input)
    {
        try
        {
            return ValueTask.FromResult(
                new NCalc.Expression(PromoteNumbers(input)).Evaluate()!.ToString()!);
        }
        catch (Exception exception)
        {
            // Returns the error rather than throwing it. A tool that reports trouble
            // gives the Brain something to read and recover from; a tool that throws
            // is a dependency failure and ends the turn (#29). "2 +" is a bad
            // expression, not a broken calculator.
            return ValueTask.FromResult($"error: {exception.Message}");
        }
    }

    // NCalc does integer division on integer literals, so "7/2" would answer 3.
    // Promoting whole numbers to decimals makes it answer 3.5, which is what anyone
    // asking a calculator expects.
    private static string PromoteNumbers(string expression) =>
        WholeNumberRegex().Replace(expression, "${0}.0");

    [GeneratedRegex(@"(?<![\d.])\d+(?![\d.])")]
    private static partial Regex WholeNumberRegex();
}
