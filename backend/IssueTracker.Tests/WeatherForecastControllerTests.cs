using System.Net;
using System.Net.Http.Json;
using IssueTracker.Api;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using Xunit.Abstractions;
namespace IssueTracker.Tests;

public class WeatherForecastControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;
    public WeatherForecastControllerTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        _client = factory.CreateClient();
        _output = output;
    }

    [Fact]
    public async Task GetWeatherForecast()
    {
        var response = await _client.GetAsync("/WeatherForecast");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var forecasts = await response.Content.ReadFromJsonAsync<List<WeatherForecastResponse>>();

        Assert.NotNull(forecasts);
        Assert.Equal(5, forecasts.Count);

        foreach(var forecast in forecasts)
        {
            _output.WriteLine($"Date {forecast.Date:yyyy-MM-dd} | Temp: {forecast.TemperatureC}°C | Summary: {forecast.Summary}");
            Assert.NotEqual(default, forecast.Date);

            Assert.True(forecast.TemperatureC >= -20 && forecast.TemperatureC <= 55, $"Temperature {forecast.TemperatureC}°C is out expected -20 to 55 range.");

            Assert.False(string.IsNullOrWhiteSpace(forecast.Summary), "Weather summmary shouldn't be empty.");
        }
    }
}

public record WeatherForecastResponse(DateOnly Date, int TemperatureC, string? Summary);
