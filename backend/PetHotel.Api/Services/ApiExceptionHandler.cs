using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace PetHotel.Api.Services;

public class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    // Turn an exception into an Http status and an error message for the frontend.
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception,
        CancellationToken cancellationToken)
    {
        // Use a general message for unexpected errors.
        var status = StatusCodes.Status500InternalServerError;
        var message = "The request could not be completed. Please try again.";

        // Return 400 for invalid input, 404 for missing records and 409 for conflicts.
        if (exception is ValidationException validationException)
        {
            status = StatusCodes.Status400BadRequest;
            message = validationException.Message;
        }
        else if (exception is CareException careException)
        {
            status = careException.Error switch
            {
                CareError.InvalidInput => StatusCodes.Status400BadRequest,
                CareError.NotFound => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status409Conflict
            };
            message = careException.Message;
        }
        else if (exception is BookingException bookingException)
        {
            status = bookingException.Error switch
            {
                BookingError.InvalidInput => StatusCodes.Status400BadRequest,
                BookingError.NotFound => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status409Conflict
            };
            message = bookingException.Message;

        }
        else if (exception is StayException stayException)
        {
            status = stayException.Error switch
            {
                StayError.InvalidInput => StatusCodes.Status400BadRequest,
                StayError.NotFound => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status409Conflict
            };
            message = stayException.Message;
        }
        else if (exception is DbUpdateConcurrencyException)
        {
            status = StatusCodes.Status409Conflict;
            message = "The record has changed. Refresh the page and try again.";
        }
        else
        {
            // Keep database details out of API responses.
            logger.LogError(exception, "The API request failed.");
        }

        // Use the same JSON error format across the API.
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = status == 500 ? "Server error" : "Request could not be completed",
            Detail = message
        }, options: null, contentType: "application/problem+json", cancellationToken: cancellationToken);
        return true;
    }
}
