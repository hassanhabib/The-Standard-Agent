// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Foundations.InternalTools.Exceptions;
using Xeptions;

namespace Standard.Agents.Services.Foundations.Direction;

public partial class InternalToolService
{
    private delegate ValueTask<bool> ReturningBooleanFunction();

    private async ValueTask<bool> TryCatch(ReturningBooleanFunction returningBooleanFunction)
    {
        try
        {
            return await returningBooleanFunction();
        }
        catch (InvalidInternalToolException invalidInternalToolException)
        {
            throw await CreateAndLogValidationExceptionAsync(invalidInternalToolException);
        }
        catch (Exception exception)
        {
            var failedInternalToolServiceException =
                new FailedInternalToolServiceException(
                    message: "Failed internal tool service error occurred, contact support.",
                    innerException: exception);

            throw await CreateAndLogServiceExceptionAsync(failedInternalToolServiceException);
        }
    }

    private async ValueTask<InternalToolValidationException> CreateAndLogValidationExceptionAsync(
        Xeption exception)
    {
        var internalToolValidationException =
            new InternalToolValidationException(
                message: "Internal tool validation error occurred, fix the error and try again.",
                innerException: exception);

        await this.loggingBroker.LogErrorAsync(internalToolValidationException);

        return internalToolValidationException;
    }

    private async ValueTask<InternalToolServiceException> CreateAndLogServiceExceptionAsync(
        Xeption exception)
    {
        var internalToolServiceException =
            new InternalToolServiceException(
                message: "Internal tool service error occurred, contact support.",
                innerException: exception);

        await this.loggingBroker.LogErrorAsync(internalToolServiceException);

        return internalToolServiceException;
    }
}
