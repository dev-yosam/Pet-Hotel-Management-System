namespace PetHotel.Api.DTOs;

public static class InputRules
{
    public const string PersonName = @"(?=.*\p{L})[\p{L}\p{M} .'’\-]+";
    public const string PetName = @"(?=.*\p{L})[\p{L}\p{M}0-9 .'’\-]+";
    public const string AnimalType = @"(?=.*\p{L})[\p{L}\p{M} .'’/\-]+";
    public const string Phone = @"(?=(?:[^0-9]*[0-9]){7,15}[^0-9]*$) *\+?(?:[0-9]+|\([0-9]+\))(?:[ -]?(?:[0-9]+|\([0-9]+\)))* *";
    public const string Email = @"(?=[^@]{1,64}@)[A-Za-z0-9_%+'\-]+(?:\.[A-Za-z0-9_%+'\-]+)*@(?:[A-Za-z0-9](?:[A-Za-z0-9\-]{0,61}[A-Za-z0-9])?\.)+[A-Za-z]{2,63}";
}
