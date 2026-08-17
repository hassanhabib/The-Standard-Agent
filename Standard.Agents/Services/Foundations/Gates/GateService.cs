// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Brokers.Classifiers;
using Standard.Agents.Brokers.Loggings;

namespace Standard.Agents.Services.Foundations.Gates;

public partial class GateService : IGateService
{
    private const string ConflictRubric =
        "You review an AI agent's active skill instructions for a DIRECT CONTRADICTION — two " +
        "instructions that cannot both be obeyed at once, for example 'answer in Arabic' versus " +
        "'answer in English'. If such a contradiction exists, reply with a single line in the " +
        "form: CONFLICT: <short label> | <instruction> || <short label> | <instruction>. If there " +
        "is no contradiction, reply with a single word: NONE.";

    private readonly IClassifierBroker classifierBroker;
    private readonly ILoggingBroker loggingBroker;

    public GateService(
        IClassifierBroker classifierBroker,
        ILoggingBroker loggingBroker)
    {
        this.classifierBroker = classifierBroker;
        this.loggingBroker = loggingBroker;
    }

    // The Gate screens the raw task, so it sees exactly what redaction exists to hide, and it may
    // run on a different host than the Brain (SPEC.md §4.6). That redaction is a decoration on
    // the classifier below, applied at composition, so this service holds one broker.
    public ValueTask<string> ScreenAsync(string input) =>
    TryCatch(async () =>
    {
        ValidateScreen(input);

        return await this.classifierBroker.ClassifyAsync(input);
    });

    public ValueTask<string> DetectConflictAsync(string instructions) =>
    TryCatch(async () =>
    {
        ValidateScreen(instructions);

        return await this.classifierBroker.AssessAsync(ConflictRubric, instructions);
    });
}
