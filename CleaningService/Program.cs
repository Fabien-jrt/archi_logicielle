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

app.MapGet("/get_rooms_to_clean_today", async () =>
{
    const string roomUrl = baseDBAPIUrl + "rooms/records";
    const string cleaningsUrl = baseDBAPIUrl + "cleanings/records";
    const string bookingUrl = baseDBAPIUrl + "bookings/records";

    using (HttpClient client = new HttpClient())
    {
        try
        {
            // Fetch data from APIs
            HttpResponseMessage roomResponse = await client.GetAsync(roomUrl);
            HttpResponseMessage cleaningsResponse = await client.GetAsync(cleaningsUrl);
            HttpResponseMessage bookingsResponse = await client.GetAsync(bookingUrl);

            if (roomResponse.IsSuccessStatusCode && cleaningsResponse.IsSuccessStatusCode && bookingsResponse.IsSuccessStatusCode)
            {
                var roomContent = await roomResponse.Content.ReadAsStringAsync();
                var cleaningsContent = await cleaningsResponse.Content.ReadAsStringAsync();
                var bookingsContent = await bookingsResponse.Content.ReadAsStringAsync();

                var rooms = JsonSerializer.Deserialize<JsonElement>(roomContent);
                var cleanings = JsonSerializer.Deserialize<JsonElement>(cleaningsContent);
                var bookings = JsonSerializer.Deserialize<JsonElement>(bookingsContent);

                // Get rooms booked and client checked out
                var checkedOutRooms = bookings.GetProperty("items")
                    .EnumerateArray()
                    .Where(b =>
                        DateTime.TryParse(b.GetProperty("end_date").GetString(), out DateTime endDate) &&
                        endDate.Date == DateTime.UtcNow.Date)
                    .SelectMany(b => b.GetProperty("rooms").EnumerateArray().Select(r => r.GetString()))
                    .ToList();

                // Get rooms already cleaned today
                var cleanedRooms = cleanings.GetProperty("items")
                    .EnumerateArray()
                    .Where(c =>
                        DateTime.TryParse(c.GetProperty("cleaned_at").GetString(), out DateTime cleanedAt) &&
                        cleanedAt.Date == DateTime.UtcNow.Date)
                    .Select(c => c.GetProperty("room").GetString())
                    .ToList();

                // Filter rooms that need to be cleaned today
                var roomsToClean = rooms.GetProperty("items")
                    .EnumerateArray()
                    .Where(r => checkedOutRooms.Contains(r.GetProperty("id").GetString()) && !cleanedRooms.Contains(r.GetProperty("id").GetString()))
                    .ToList();

                return Results.Json(roomsToClean);
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
.WithName("GetRoomsToCleanToday")
.WithOpenApi();

app.Run();
