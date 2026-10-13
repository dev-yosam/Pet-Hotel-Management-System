using Microsoft.EntityFrameworkCore;
using PetHotel.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<PetHotelDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PetHotel")
        ?? throw new InvalidOperationException("ConnectionStrings:PetHotel is not configured.")));

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
