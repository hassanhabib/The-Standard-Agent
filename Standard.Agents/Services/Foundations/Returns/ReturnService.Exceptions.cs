// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Foundations.Returns.Exceptions;
using Xeptions;

namespace Standard.Agents.Services.Foundations.Returns;

public partial class ReturnService
{
    private delegate ValueTask<string> ReturningStringFunction();

            private async ValueTask<string> TryCatch(ReturningStringFunction returningStringFunction)
    {
        try
        {
            return await returningStringFunction();
        }
        catch (InvalidReturnException invalidReturnException)
        {
            throw await CreateAndLogValidationExceptionAsync(invalidReturnException);
        }
        catch (Exception exception)
        {
            var failedReturnServiceException =
                new FailedReturnServiceException(
                    message: "Failed return service error occurred, contact support.",
                    innerException: exception);

            throw await CreateAndLogServiceExceptionAsync(failedReturnServiceException);
        }
    }

    private async ValueTask<ReturnValidationException> CreateAndLogValidationExceptionAsync(
        Xeption? exception)
    {
        var returnValidationException =
            new ReturnValidationException(
                message: "Return validation error occurred, fix the error and try again.",
                innerException: exception);

        await this.loggingBroker.LogErrorAsync(returnValidationException);

        return returnValidationException;
    }

    private async ValueTask<ReturnServiceException> CreateAndLogServiceExceptionAsync(
        Xeption? exception)
    {
        var returnServiceException =
            new ReturnServiceException(
                message: "Return service error occurred, contact support.",
                innerException: exception);

        await this.loggingBroker.LogErrorAsync(returnServiceException);

        return returnServiceException;
    }
}
