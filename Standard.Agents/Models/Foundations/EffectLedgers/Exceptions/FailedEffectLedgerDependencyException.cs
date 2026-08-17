// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Xeptions;

namespace Standard.Agents.Models.Foundations.EffectLedgers.Exceptions;

public class FailedEffectLedgerDependencyException : Xeption
{
    public FailedEffectLedgerDependencyException(string message, Exception innerException)
        : base(message, innerException)
    { }
}
