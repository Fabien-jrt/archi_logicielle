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

app.MapGet("/get_availability", async () =>
{
    const string roomUrl = baseDBAPIUrl + "rooms/records";

    using (HttpClient client = new HttpClient())
    {
        try
        {
            HttpResponseMessage response = await client.GetAsync(roomUrl);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<JsonElement>(content);
            }
            else
            {
                throw new HttpRequestException($"Failed to fetch data. Status code: {response.StatusCode}");
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
