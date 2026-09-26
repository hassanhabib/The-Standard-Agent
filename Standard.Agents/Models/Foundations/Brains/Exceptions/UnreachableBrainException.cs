// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Xeptions;

namespace Standard.Agents.Models.Foundations.Brains.Exceptions;

/// <summary>
/// Nothing answered at all: a refused connection, a name that resolves to nothing, a request that
/// never left the machine (SPEC.md §4.10, v1.14). The person's next step is the address and
/// whatever should be listening at it, not support, so this is the sentence they are shown. The
/// native fault is kept as the inner exception for whoever investigates.
/// </summary>
public class UnreachableBrainException : Xeption
{
    public UnreachableBrainException(string message, Exception innerException)
        : base(message, innerException)
    { }
}
