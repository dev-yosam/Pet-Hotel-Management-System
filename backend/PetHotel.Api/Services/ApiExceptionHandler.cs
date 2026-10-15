using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PetHotel.Api.Services;

public class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception,
        CancellationToken cancellationToken)
    {
        var status = StatusCodes.Status500InternalServerError;
        var message = "The request could not be completed. Please try again.";

        if (exception is CareException careException)
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
