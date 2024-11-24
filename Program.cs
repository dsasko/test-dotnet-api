using System.Net.Http;
using System.Text.Json;
using dotenv.net;

DotEnv.Load();
var envVars = DotEnv.Read();
var clientId = envVars["CLIENT_ID"];
var clientSecret = envVars["CLIENT_SECRET"];

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
HttpClient httpClient = new();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowOrigin",
        builder =>
        {
            builder.WithOrigins("http://localhost:5173")
                .AllowAnyMethod()
                .AllowAnyHeader()
                .WithExposedHeaders("Content-Length", "Content-Type", "Access-Control-Allow-Origin");
    });
});

var app = builder.Build();

// Apply CORS before routes
app.UseCors("AllowOrigin");

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

string[] whitelistedQueryParameters = { "originLocationCode", "destinationLocationCode", "departureDate", "adults" };

app.MapGet("/flights", async (HttpContext httpContext) =>
{
    // Create an object for storing whitelisted query parameters
    var queryParams = new Dictionary<string, string>();

    // Add only whitelisted query parameters to the queryParams dictionary
    foreach (var param in httpContext.Request.Query)
    {
        if (whitelistedQueryParameters.Contains(param.Key) && !string.IsNullOrEmpty(param.Value))
        {
            queryParams[param.Key] = param.Value!;
        }
    }

    var accessToken = await ProcessRepositoriesAsync(httpClient, clientId, clientSecret);

    var requestUri = "https://test.api.amadeus.com/v2/shopping/flight-offers";

    var uriBuilder = new UriBuilder(requestUri);
    var query = await new FormUrlEncodedContent(queryParams).ReadAsStringAsync();
    uriBuilder.Query = query;

    // Create an HTTP request message
    var request = new HttpRequestMessage(HttpMethod.Get, uriBuilder.ToString());

    // Add the Authorization header
    if (accessToken != null)
    {
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
    }

    // Send the request and obtain the response
    var response = await httpClient.SendAsync(request);

    // Read the response content as a string (JSON)
    var jsonResponse = await response.Content.ReadAsStringAsync();

    // Explicitly set the response content type to JSON
    httpContext.Response.ContentType = "application/json";

    // Directly return the JSON string; let ASP.NET handle the content length
    return Results.Content(jsonResponse, "application/json");
})
.WithName("GetFlights");

app.Run();

static async Task<string?> ProcessRepositoriesAsync(HttpClient client, string id, string secret)
{
    var requestUri = "https://test.api.amadeus.com/v1/security/oauth2/token";

    // Create form data to send with the POST request.
    var formData = new Dictionary<string, string>
    {
        { "grant_type", "client_credentials" },
        { "client_id", id },
        { "client_secret", secret }
    };

    // Encode the form data as application/x-www-form-urlencoded content.
    using var content = new FormUrlEncodedContent(formData);

    // Send the HTTP POST request with content.
    var response = await client.PostAsync(requestUri, content);

    // Parse the response content.
    var responseContent = await response.Content.ReadAsStringAsync();
    var responseJson = JsonDocument.Parse(responseContent);
    string? accessToken = responseJson.RootElement.GetProperty("access_token").GetString();

    return accessToken;
}
