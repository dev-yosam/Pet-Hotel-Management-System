namespace PetHotel.Api.Services;

public enum CareError
{
    InvalidInput,
    NotFound,
    Conflict
}

public class CareException(CareError error, string message) : Exception(message)
{
    public CareError Error { get; } = error;
}
