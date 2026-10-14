namespace PetHotel.Api.Services;

public enum BookingError
{
    InvalidInput,
    NotFound,
    Conflict
}

public class BookingException(
    BookingError error,
    string message) : Exception(message)
{
    public BookingError Error { get; } = error;
}