using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

const string baseDBAPIUrl = "http://localhost:8090/api/collections/";

app.MapGet("/get_availability", async (DateTime startDate, DateTime endDate) =>
{
    const string roomUrl = baseDBAPIUrl + "rooms/records";
    const string bookingUrl = baseDBAPIUrl + "bookings/records";

    using (HttpClient client = new HttpClient())
    {
        try
        {
            // Fetch rooms
            HttpResponseMessage roomResponse = await client.GetAsync(roomUrl);
            HttpResponseMessage bookingResponse = await client.GetAsync(bookingUrl);

            if (roomResponse.IsSuccessStatusCode && bookingResponse.IsSuccessStatusCode)
            {
                var roomContent = await roomResponse.Content.ReadAsStringAsync();
                var bookingContent = await bookingResponse.Content.ReadAsStringAsync();

                var rooms = JsonSerializer.Deserialize<JsonElement>(roomContent);
                var bookings = JsonSerializer.Deserialize<JsonElement>(bookingContent);

                // Extract booked rooms within the date range
                var bookedRooms = bookings.GetProperty("items")
                    .EnumerateArray()
                    .Where(b =>
                        DateTime.TryParse(b.GetProperty("start_date").GetString(), out DateTime bookingStartDate) &&
                        DateTime.TryParse(b.GetProperty("end_date").GetString(), out DateTime bookingEndDate) &&
                        !(bookingEndDate <= startDate || bookingStartDate >= endDate))
                    .SelectMany(b => b.GetProperty("rooms").EnumerateArray().Select(r => r.GetString()))
                    .ToList();

                // Filter available rooms
                var availableRooms = rooms.GetProperty("items")
                    .EnumerateArray()
                    .Where(r => !bookedRooms.Contains(r.GetProperty("id").GetString()))
                    .ToList();

                return Results.Json(availableRooms);
            }
            else
            {
                throw new HttpRequestException($"Failed to fetch data.");
            }
        }
        catch (Exception ex)
        {
            throw new HttpRequestException($"An error occurred: {ex.Message}");
        }
    }
})
.WithName("GetAvailability")
.WithOpenApi();

app.Run();
