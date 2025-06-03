using Serilog.Context;

namespace Common.OAuth;

using System.Text.Json;
using static Common.Util;
using static Common.Log;

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
        var response = await Fetch(endpoint: "https://discord.com/api/users/@me", transformers: httpClient =>
        {
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(token.TokenType, token.AccessToken);
        });

        if (response is null)
        {
            Log.Logger.Debug("The provided token is wrong");
            // This assumes that every fetch error unrelated to HttpRequestException 
            // is due to a wrongly provided code.
            return null;
        }

        try
        {
            string content = await response.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(content);
            var user = new User
            {
                ID = doc.RootElement.GetProperty("id").GetString() ?? throw new NullReferenceException("The returned id is null"),
                Username = doc.RootElement.GetProperty("username").GetString() ?? throw new NullReferenceException("The returned username is null"),
                Avatar = doc.RootElement.GetProperty("avatar").GetString() ?? throw new NullReferenceException("The avatar is null"),
                Premium = (OAuth.User.Nitro)doc.RootElement.GetProperty("premium_type").GetSByte(),
                GlobalName = doc.RootElement.GetProperty("global_name").GetString() ?? throw new NullReferenceException("The avatar is null"),
                Mfa = doc.RootElement.GetProperty("mfa_enabled").GetBoolean(),
            };

            return user;
        }
        catch (NullReferenceException ex)
        {
            Log.Logger.Error(ex, $"Could not parse API response to User: {ex.Message}");
            throw;
        }
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
        // Remove trailing slash from redirectUri if present. (Discord does not like it)
        if (!string.IsNullOrEmpty(redirectUri) && redirectUri.EndsWith("/"))
            redirectUri = redirectUri.TrimEnd('/');
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
        try
        {
            var response = await Fetch($"{client.ApiEndpoint}/oauth2/token", form, httpClient =>
            {
                var authToken = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{client.ID}:{client.Secret}"));
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authToken);
            });

            if (response is null)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();

            if (content is not null)
            {

                using JsonDocument doc = JsonDocument.Parse(content);
                Token token = new Token
                {
                    TokenType = doc.RootElement.GetProperty("token_type").GetString() ?? throw new NullReferenceException("The returned token_type is null"),
                    AccessToken = doc.RootElement.GetProperty("access_token").GetString() ?? throw new NullReferenceException("The returned access_token is null"),
                    RefreshToken = doc.RootElement.GetProperty("refresh_token").GetString() ?? throw new NullReferenceException("The returned refresh_token is null"),
                    ExpiresIn = doc.RootElement.GetProperty("expires_in").GetInt64(),
                    CreatedAt = DateTime.UtcNow,
                    Scope = (doc.RootElement.GetProperty("scope").GetString() ?? "").Split(" ").ToHashSet<string>(),
                };
                return token;
            }

        }
        catch (NullReferenceException ex)
        {
            Log.Logger.Error(ex, $"Could not parse API response to Token: {ex.Message}");
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

        var form = new FormUrlEncodedContent(new[] {
                new KeyValuePair<string, string>("token", token.AccessToken),
                new KeyValuePair<string, string>("token_type_hint", "access_token")
        });

        var response = await Fetch($"{client.ApiEndpoint}/oauth2/token/revoke", form, httpClient =>
        {
            var authToken = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{client.ID}:{client.Secret}"));
            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authToken);
        });

        if (response is null)
        {
            return false;
        }
        return true;

    }

    /// <summar>
    /// Fetches the given endpoint and returns the content.
    /// 
    /// </summary>
    /// <param name="endpoint">The endpoint which gets fetched</param>
    /// <param name="form">If this parameter is not null the request is a <c>POST</c> request. This is the body of the <c>POST</c> request</param>
    /// <returns>
    /// The content as string or null if an error occured
    /// </returns>
    /// <exception cref="HttpRequestException">
    /// Thrown when the HTTP request fails due to network issues or an invalid response from the server.
    /// </exception>
    private static async Task<HttpResponseMessage?> Fetch(string endpoint, FormUrlEncodedContent? form = null, params Action<HttpClient>[] transformers)
    {

        using HttpClient httpClient = new();
        try
        {
            foreach (var transformer in transformers)
            {
                transformer(httpClient);
            }
            
            using (LogContext.PushProperty("SourceContext", "Discord.OAuth"))
            {
                //TODO: Make retry attempts configurable
                for (int attempt = 0; attempt < 3; attempt++)
                {
                    HttpResponseMessage response = null!;
                    if (form is null)
                    {
                        response = await httpClient.GetAsync(endpoint);
                    }
                    else
                    {
                        response = await httpClient.PostAsync(endpoint, form);
                    }
                    string content = await response.Content.ReadAsStringAsync();
                    if (response.IsSuccessStatusCode)
                    {
                        return response;
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {

                        int retryAfterMs = JsonDocument.Parse(content).RootElement.GetProperty("retry_after").GetInt32();
                        Log.Logger.Warning(
                            "Rate limited while accessing {Endpoint}. Attempting {AttemptsLeft} more times. Waiting {RetryAfter}ms before retrying...",
                            endpoint, 3 - attempt, retryAfterMs);
                        await Task.Delay(retryAfterMs);
                        continue;

                    }
                    else
                    {
                        Log.Logger.Debug("Error while fetching {Endpoint}: {Content}", endpoint, content);
                        return null;
                    }
                }
            }
        }
        catch (HttpRequestException ex)
        {
            using (LogContext.PushProperty("SourceContext", "Discord.OAuth"))
            {
                Log.Logger.Error(ex, "Request to {Endpoint} failed: {Message}", endpoint, ex.Message);
            }
            throw;
        }
        catch (JsonException ex)
        {
            using (LogContext.PushProperty("SourceContext", "Discord.OAuth"))
            {
                Log.Logger.Error(ex, "JSON parsing failed for {Endpoint}: {Message}", endpoint, ex.Message);
            }
            return null;
        }
        catch (Exception ex)
        {
            using (LogContext.PushProperty("SourceContext", "Discord.OAuth"))
            {
                Log.Logger.Error(ex, "Unexpected error accessing {Endpoint}: {Message}", endpoint, ex.Message);
            }
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
            return user.Value.ID;
        }
        else
        {
            return null;
        }
    }
}
