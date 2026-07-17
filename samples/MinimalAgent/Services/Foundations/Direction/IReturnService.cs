// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

namespace MinimalAgent.Services.Foundations.Direction;

public interface IReturnService
{
    ValueTask<string> ReturnAsync(string payload);
}
