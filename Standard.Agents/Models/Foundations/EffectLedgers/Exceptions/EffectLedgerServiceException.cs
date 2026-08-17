// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Xeptions;

namespace Standard.Agents.Models.Foundations.EffectLedgers.Exceptions;

public class EffectLedgerServiceException : Xeption
{
    public EffectLedgerServiceException(string message, Xeption? innerException)
        : base(message, innerException)
    { }
}
