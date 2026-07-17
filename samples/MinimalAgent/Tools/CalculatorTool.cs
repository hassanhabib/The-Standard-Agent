// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using System.Data;
using System.Text.RegularExpressions;

namespace MinimalAgent.Tools;

public sealed partial class CalculatorTool : ITool
{
    public string Name => "calculator";

    public ValueTask<string> ExecuteAsync(string input) =>
        ValueTask.FromResult(
            new DataTable().Compute(PromoteNumbers(input), null).ToString()!);

    private static string PromoteNumbers(string expression) =>
        WholeNumberRegex().Replace(expression, "${0}.0");

    [GeneratedRegex(@"(?<![\d.])\d+(?![\d.])")]
    private static partial Regex WholeNumberRegex();
}
