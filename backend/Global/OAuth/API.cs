namespace Global.OAuth;

using System.Text.Json;
using static Global.Util;
using static Global.Log;

//TODO: Remove the enormous amount of code duplicaiton. (I am way to lazy right now)

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
                        ID = doc.RootElement.GetProperty("id").GetString() ?? throw new NullReferenceException("The returned id is null"),
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
    /// <param name="code">The code from the user that authorized to Application.</param>
    /// <param name="redirectUri">The uri to redirect to. Does not actually redirect.
    /// <b>HAS to be the same redirecting URI as the one that was used the get the code.</b></param>
    /// <param name="client">A client object representing a Discord Client/Application that contains the credentials to make such API request</param>
    /// <returns>
    /// A Object representing the Token with fields such as AccessToken, RefreshToken, TokenType,... etc.
    /// May return <c>null</c> if one of the parameter (or multiple) are incorrect or an unexpected exception occurs.
    /// </returns>
    /// <exception cref="HttpRequestException">
    /// Thrown when the HTTP request fails due to network issues or an invalid response from the server.
    /// </exception>
    public static async Task<Token?> GetToken(string code, string redirectUri, Client client)
    {
        var form = new FormUrlEncodedContent(new[] {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("redirect_uri", redirectUri)
        });

        return await FetchToken(form, client);

    }

    /// <summary>
    /// Refreshes the Access and Refresh Token from the Discord API. 
    /// </summary>
    /// <param name="token">A Object representing the Token with fields such as AccessToken, RefreshToken, TokenType,... etc.</param>
    /// <param name="client">A client object representing a Discord Client/Application that contains the credentials to make such API request</param>
    /// <returns>
    /// The refreshed <c>Token</c> 
    /// </returns>
    /// <exception cref="HttpRequestException">
    /// Thrown when the HTTP request fails due to network issues or an invalid response from the server.
    /// </exception>
    public static async Task<Token?> RefreshToken(Token token, Client client)
    {
        var form = new FormUrlEncodedContent(new[] {
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("refresh_token", token.RefreshToken),
        });

        return await FetchToken(form, client);
    }


    /// <summary>
    /// Sends a request to the Discord API endpoint at (<c>/oauth2/token</c>) and returns the content parsed to an Token object
    /// </summary>
    /// <param name="code">The code from the user that authorized to Application.</param>
    /// <param name="client">A client object representing a Discord Client/Application that contains the credentials to make such API request</param>
    /// <returns>
    /// A Object representing the Token with fields such as AccessToken, RefreshToken, TokenType,... etc.
    /// May return <c>null</c> if one of the parameter (or multiple) are incorrect or an unexpected exception occurs.
    /// </returns>
    /// <exception cref="HttpRequestException">
    /// Thrown when the HTTP request fails due to network issues or an invalid response from the server.
    /// </exception>
    private static async Task<Token?> FetchToken(FormUrlEncodedContent form, Client client)
    {


        using HttpClient httpClient = new();
        var authToken = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{client.ID}:{client.Secret}"));
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authToken);

        try
        {

            for (int attempt = 0; attempt < 3; attempt++)
            {
                var response = await httpClient.PostAsync($"{client.ApiEndpoint}/oauth2/token", form);
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
                    Global.Log.Logger.Debug($"Error from Discord API while fetching Token: {content.ToString()}");
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
    /// Revokes the Access and Refresh Token. 
    /// </summary>
    /// <param name="token">A Object representing the Token with fields such as AccessToken, RefreshToken, TokenType,... etc.</param>
    /// <param name="client">A Object representing a Discord Client/Application that contains the credentials to make such API request</param>
    /// <returns>
    /// Whether or not the revocation was a success
    /// </returns>
    /// <exception cref="HttpRequestException">
    /// Thrown when the HTTP request fails due to network issues or an invalid response from the server.
    /// </exception>
    public static async Task<bool> RevokeToken(Token token, Client client)
    {

        using HttpClient httpClient = new();
        var authToken = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{client.ID}:{client.Secret}"));
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authToken);

        var form = new FormUrlEncodedContent(new[] {
                new KeyValuePair<string, string>("token", token.AccessToken),
                new KeyValuePair<string, string>("token_type_hint", "access_token")
        });

        try
        {

            for (int attempt = 0; attempt < 3; attempt++)
            {
                var response = await httpClient.PostAsync($"{client.ApiEndpoint}/oauth2/token/revoke", form);
                string content = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    return true;
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
                    Global.Log.Logger.Debug($"Error from Discord API while revoking user Token: {content.ToString()}");
                    return false;
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
            return false;
        }
        catch (Exception ex)
        {
            Global.Log.Logger.Error(ex, $"Unexpected error: {ex.Message}");
            return false;
        }
        return false;
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
            return user.Value.ID;
        }
        else
        {
            return null;
        }
    }
}
