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
                        Premium = (Global.OAuth.User.Nitro)doc.RootElement.GetProperty("premium_type").GetSByte(),
                        GlobalName = doc.RootElement.GetProperty("global_name").GetString() ?? throw new NullReferenceException("The avatar is null"),
                        Mfa = doc.RootElement.GetProperty("mfa_enabled").GetBoolean(),
                    };

                    return user;
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    Global.Log.Logger.Debug("The user provided a wrong code");
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
            Global.Log.Logger.Error(ex, $"Request failed: {ex.Message}");
            throw;
        }
        catch (JsonException ex)
        {
            Global.Log.Logger.Error(ex, $"JSON parsing failed: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Global.Log.Logger.Error(ex, $"Unexpected error: {ex.Message}");
            return null;
        }


        Global.Log.Logger.Error("Max retry attempts reached.");
        return null;
    }

    /// <summary>
    /// Fetches the Access and Refresh Token from the Discord API. 
    /// </summary>
    /// <param name="clientID">The clientID of the Discord Application.</param>
    /// <param name="clientSecret">The clientSecret of the Discord Application.</param>
    /// <param name="code">The code from the user that authorized to Application.</param>
    /// <param name="redirectUri">The uri to redirect to. Does not actually redirect.
    /// <b>HAS to be the same redirecting URI as the one that was used the get the code.</b></param>
    /// <returns>
    /// A Object representing the Token with fields such as AccessToken, RefreshToken, TokenType,... etc.
    /// May return <c>null</c> if one of the parameter (or multiple) are incorrect or an unexpected exception occurs.
    /// </returns>
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

                    using JsonDocument doc = JsonDocument.Parse(content);
                    Token token = new Token
                    {
                        TokenType = doc.RootElement.GetProperty("token_type").GetString() ?? throw new NullReferenceException("The returned token_type is null"),
                        AccessToken = doc.RootElement.GetProperty("access_token").GetString() ?? throw new NullReferenceException("The returned  is null"),
                        RefreshToken = doc.RootElement.GetProperty("refresh_token").GetString() ?? throw new NullReferenceException("The returned refresh_token is null"),
                        ExpiresIn = doc.RootElement.GetProperty("expires_in").GetInt64(),
                        CreatedAt = DateTime.UtcNow,
                        Scope = (doc.RootElement.GetProperty("scope").GetString() ?? "").Split(" ").ToHashSet<string>(),
                    };
                    return token;
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
                    Global.Log.Logger.Debug($"Error from Discord API while fetching user Token: {content.ToString()}");
                    return null;
                }
            }
        }
        catch (HttpRequestException ex)
        {
            Global.Log.Logger.Error(ex, $"Request failed: {ex.Message}");
            throw;
        }
        catch (JsonException ex)
        {
            Global.Log.Logger.Error(ex, $"JSON parsing failed: {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            Global.Log.Logger.Error(ex, $"Unexpected error: {ex.Message}");
            return null;
        }
        return null;
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
