using Microsoft.EntityFrameworkCore;
using PetHotel.Api.Data;
using PetHotel.Api.Interfaces;
using PetHotel.Api.Services;
using PetHotel.Api.Services.Pricing;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<PetHotelDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PetHotel")
        ?? throw new InvalidOperationException("ConnectionStrings:PetHotel is not configured.")));

builder.Services.AddScoped<IOwnerService, OwnerService>();
builder.Services.AddScoped<IPetService, PetService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddSingleton<RoomPricing, StandardRoomPricing>();
builder.Services.AddSingleton<RoomPricing, DeluxeRoomPricing>();
builder.Services.AddSingleton<IStayChargeCalculator, StayChargeCalculator>();
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddScoped<IStayService, StayService>();
builder.Services.AddControllers();
builder.Services.AddScoped<ICareService, CareService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IStayHistoryService, StayHistoryService>();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
