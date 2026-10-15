namespace PetHotel.Api.Interfaces;

public interface IStayChargeCalculator
{
    decimal CalculateCharge(
        string roomType,
        DateTimeOffset scheduledCheckIn,
        DateTimeOffset scheduledCheckOut);
}