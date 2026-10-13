using Microsoft.EntityFrameworkCore;
using PetHotel.Api.Data;
using PetHotel.Api.Interfaces;
using PetHotel.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<PetHotelDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PetHotel")
        ?? throw new InvalidOperationException("ConnectionStrings:PetHotel is not configured.")));

builder.Services.AddScoped<IOwnerService, OwnerService>();
builder.Services.AddScoped<IPetService, PetService>();
builder.Services.AddControllers();
builder.Services.AddScoped<ICareService, CareService>();
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
