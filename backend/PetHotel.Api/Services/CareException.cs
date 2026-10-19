namespace PetHotel.Api.Services;

// The error type tells the Api which http status to return
public enum CareError
{
    InvalidInput,
    NotFound,
    Conflict
}

// Keep the care error type and message together for the API error handler.
public class CareException(CareError error, string message) : Exception(message)
{
    public CareError Error { get; } = error;
}
