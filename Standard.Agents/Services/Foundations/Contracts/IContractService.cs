// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Foundations.Contracts;

namespace Standard.Agents.Services.Foundations.Contracts;

public interface IContractService
{
    /// <summary>
    /// Whether an answer is the shape it was held to, and when it is not, what a model should do
    /// about it.
    /// </summary>
    ValueTask<ContractVerdict> CheckAsync(string answer, string schema);
}
