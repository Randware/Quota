namespace Global.OAuth;

using System.Text.Json;
using static Global.Util;
using static Global.Log;

public static class API
{

    /// <summary>
    /// Fetches the current user's info from the Discord API using the provided token.
    /// </summary>
    /// <param name="token">An access token containing the token type and access token string.</param>
    /// <returns>
    /// The Discord information of the authenticated user, or <c>null</c> if the request fails or is rate-limited.
    /// </returns>
    /// <exception cref="HttpRequestException">
    /// Thrown when the HTTP request fails due to network issues or an invalid response from the server.
    /// </exception>
    public static async Task<User?> FetchUser(Token token)
    {
        using HttpClient client = new();

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(token.TokenType, token.AccessToken);


        try
        {
            for (int attempt = 0; attempt < 3; attempt++)
            {
                var response = await client.GetAsync("https://discord.com/api/users/@me");

                string content = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {


                    using JsonDocument doc = JsonDocument.Parse(content);

                    var user = new User
                    {
                        Id = doc.RootElement.GetProperty("id").GetString() ?? throw new NullReferenceException("The returned id is null"),
                        Username = doc.RootElement.GetProperty("username").GetString() ?? throw new NullReferenceException("The returned username is null"),
                        Avatar = doc.RootElement.GetProperty("avatar").GetString() ?? throw new NullReferenceException("The avatar is null"),
                        Premium = doc.RootElement.GetProperty("premium").GetSByte(),
                        GlobalName = doc.RootElement.GetProperty("global_name").GetString() ?? throw new NullReferenceException("The avatar is null"),
                        Mfa = doc.RootElement.GetProperty("mfa_enabled").GetBoolean(),
                    };

                    return user;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    Global.Log.Logger.Verbose("Invalid token given");
                    return null;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    int retryAfterMs = JsonDocument.Parse(content).RootElement.GetProperty("retry_after").GetInt32();

                    Global.Log.Logger.Warning(
                        @$"Rate limited. Attempting {3 - attempt} more times.
                    Waiting {retryAfterMs}ms before retrying...");
                    await Task.Delay(retryAfterMs);
                    continue;

                }
                else
                {
                    Global.Log.Logger.Error($"unexpected HTTP Error: {response.StatusCode} - {content}");
                    return null;
                }
            }
        }
        catch (HttpRequestException ex)
        {
            Global.Log.Logger.Error($"Request failed: {ex.Message}");
            throw;
        }
        catch (JsonException ex)
        {
            Global.Log.Logger.Error($"JSON parsing failed: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Global.Log.Logger.Error($"Unexpected error: {ex.Message}");
            return null;
        }


        Global.Log.Logger.Error("Max retry attempts reached.");
        return null;
    }

    ///
    /// <exception cref="HttpRequestException">
    /// Thrown when the HTTP request fails due to network issues or an invalid response from the server.
    /// </exception>
    public static async Task<Token?> GetToken(string clientID, string clientSecret, string code, string redirectUri)
    {

        using HttpClient client = new();
        var authToken = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{clientID}:{clientSecret}"));
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authToken);

        var form = new FormUrlEncodedContent(new[] {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("redirect_uri", redirectUri)
        });

        try
        {

            for (int attempt = 0; attempt < 3; attempt++)
            {
                var response = await client.PostAsync("https://discord.com/api/v10/oauth2/token", form);
                string content = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    using JsonDocument doc = await JsonDocument.ParseAsync(content);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    int retryAfterMs = JsonDocument.Parse(content).RootElement.GetProperty("retry_after").GetInt32();

                    Global.Log.Logger.Warning(
                        @$"Rate limited. Attempting {3 - attempt} more times.
                    Waiting {retryAfterMs}ms before retrying...");
                    await Task.Delay(retryAfterMs);
                    continue;

                }
                else
                {
                    Global.Log.Logger.Verbose("Invalid code given");
                    return null;
                }
            }
        }
        catch (HttpRequestException ex)
        {
            Global.Log.Logger.Error($"Request failed: {ex.Message}");
            throw;
        }
        catch (JsonException ex)
        {
            Global.Log.Logger.Error($"JSON parsing failed: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Global.Log.Logger.Error($"Unexpected error: {ex.Message}");
            return null;
        }
    }


    /// <summary>
    /// Fetches the Username of the Token.
    /// Returns null if the token is invalid or a other error occured
    /// </summary>
    /// <exception cref="HttpRequestException">
    /// Thrown when the HTTP request fails due to network issues or an invalid response from the server.
    /// </exception>
    public static async Task<string?> GetName(this Token token)
    {
        User? user = await FetchUser(token);

        if (user.HasValue)
        {
            return user.Value.Username;
        }
        else
        {
            return null;
        }
    }


    /// <summary>
    /// Fetches the ID of the Token.
    /// Returns null if the token is invalid or a other error occured
    /// </summary>
    /// <exception cref="HttpRequestException">
    /// Thrown when the HTTP request fails due to network issues or an invalid response from the server.
    /// </exception>
    public static async Task<string?> GetID(this Token token)
    {
        User? user = await FetchUser(token);

        if (user.HasValue)
        {
            return user.Value.Id;
        }
        else
        {
            return null;
        }
    }
}
