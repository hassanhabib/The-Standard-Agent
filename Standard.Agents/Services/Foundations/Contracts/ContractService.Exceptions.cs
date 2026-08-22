// ---------------------------------------------------------------
// Copyright (c) Hassan Habib All rights reserved.
// Licensed under the The Standard Software License (TSSL)
// ---------------------------------------------------------------

using Standard.Agents.Models.Foundations.Contracts;
using Standard.Agents.Models.Foundations.Contracts.Exceptions;
using Xeptions;

namespace Standard.Agents.Services.Foundations.Contracts;

public partial class ContractService
{
    private delegate ValueTask<ContractVerdict> ReturningVerdictFunction();

    private async ValueTask<ContractVerdict> TryCatch(
        ReturningVerdictFunction returningVerdictFunction)
    {
        try
        {
            return await returningVerdictFunction();
        }
        catch (InvalidContractException invalidContractException)
        {
            throw await CreateAndLogValidationExceptionAsync(invalidContractException);
        }

        // A malformed SCHEMA is the host's error, not the model's, and the distinction is
        // load-bearing: reported as a model failure it would send every draft into a revision
        // chasing a fault no reply can fix, until the turn budget runs out.
        catch (ArgumentException)
        {
            throw await CreateAndLogValidationExceptionAsync(
                new InvalidContractException(
                    message: "Invalid contract, the schema is not valid JSON. "
                        + "Please correct the error and try again."));
        }
        catch (HttpRequestException httpRequestException)
        {
            throw await CreateAndLogDependencyExceptionAsync(
                new FailedContractDependencyException(
                    message: "Failed contract dependency error occurred, contact support.",
                    innerException: httpRequestException));
        }
        catch (TaskCanceledException taskCanceledException)
        {
            throw await CreateAndLogDependencyExceptionAsync(
                new FailedContractDependencyException(
                    message: "Failed contract dependency error occurred, contact support.",
                    innerException: taskCanceledException));
        }
        catch (Exception exception)
        {
            var failedContractServiceException =
                new FailedContractServiceException(
                    message: "Failed contract service error occurred, contact support.",
                    innerException: exception);

            throw await CreateAndLogServiceExceptionAsync(failedContractServiceException);
        }
    }

    private async ValueTask<ContractValidationException> CreateAndLogValidationExceptionAsync(
        Xeption exception)
    {
        var contractValidationException =
            new ContractValidationException(
                message: "Contract validation error occurred, fix the error and try again.",
                innerException: exception);

        await this.loggingBroker.LogErrorAsync(contractValidationException);

        return contractValidationException;
    }

    private async ValueTask<ContractDependencyException> CreateAndLogDependencyExceptionAsync(
        Xeption exception)
    {
        var contractDependencyException =
            new ContractDependencyException(
                message: "Contract dependency error occurred, contact support.",
                innerException: exception);

        await this.loggingBroker.LogErrorAsync(contractDependencyException);

        return contractDependencyException;
    }

    private async ValueTask<ContractServiceException> CreateAndLogServiceExceptionAsync(
        Xeption exception)
    {
        var contractServiceException =
            new ContractServiceException(
                message: "Contract service error occurred, contact support.",
                innerException: exception);

        await this.loggingBroker.LogErrorAsync(contractServiceException);

        return contractServiceException;
    }
}
