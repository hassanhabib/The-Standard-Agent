// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Foundations.Contracts.Exceptions;

namespace Standard.Agents.Services.Foundations.Contracts;

public partial class ContractService
{
    // Empty is a legitimate answer to check — a model that said nothing has not satisfied a
    // schema, and reporting that honestly is the point. Null is a caller that lost the draft it
    // meant to validate, and treating it as an empty answer would report a bug as a model failure.
    private static void ValidateAnswer(string answer)
    {
        if (answer is null)
        {
            throw new InvalidContractException(
                message: "Invalid contract, answer is required. "
                    + "Please correct the error and try again.");
        }
    }
}
