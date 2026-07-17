// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Text.RegularExpressions;
using Standard.Agents.Tools;

namespace Standard.Agents.Demo.Tools;

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
                                                            return ValueTask.FromResult($"error: {exception.Message}");
        }
    }

                private static string PromoteNumbers(string expression) =>
        WholeNumberRegex().Replace(expression, "${0}.0");

    [GeneratedRegex(@"(?<![\d.])\d+(?![\d.])")]
    private static partial Regex WholeNumberRegex();
}
