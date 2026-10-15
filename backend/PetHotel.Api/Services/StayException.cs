namespace PetHotel.Api.Services;

public enum StayError
{
    InvalidInput,
    NotFound,
    Conflict
}

public class StayException(
    StayError error,
    string message) : Exception(message)
{
    public StayError Error { get; } = error;
}