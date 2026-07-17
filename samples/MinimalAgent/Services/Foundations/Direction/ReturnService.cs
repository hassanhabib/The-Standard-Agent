// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace MinimalAgent.Services.Foundations.Direction;

public sealed class ReturnService : IReturnService
{
    public ValueTask<string> ReturnAsync(string payload) =>
        ValueTask.FromResult(payload);
}
