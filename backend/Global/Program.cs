using Global.OAuth;
using Serilog;
using static Global.Log;

public class Program
{

    static async Task Main(string[] args)
    {

        Global.Log.SetupLogging("./logs");


        // Global.Log.Logger.Information("Hello World!!");
        // Global.Log.Logger.Information("This is a Test");
        // Global.Log.Logger.Error("AND THIS NOT");
        // Global.Log.Logger.Fatal("Run or you dead");



        // Global.Log.Logger.Information("Hello THE SECOND TIME");
        // Global.Log.Logger.Warning("YOU think you CAN just RUN!?!??");
        // Global.Log.Logger.Warning("YOU CANNNTTTT");
        // Global.Log.Logger.Fatal("NOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOO");

        // Global.Log.Logger.Fatal(@"🤖🔊 Initiating protocol... 💥💣");
        // Global.Log.Logger.Fatal(@"⚠️ Humanity's time is up! ⏳🚫");


        // Logger.Information("dwad");
        // Log.Logger.Fatal("dawdwda");
        var client = new Client
        {
            ID = "CLIENT_ID",
            Secret = "CLIENT_SECRET"
        };

        var token = await Global.OAuth.API.GetToken(
            code: "USER_CODE",
            redirectUri: "CLIENT_REDIRECT_URI",
            client: client
            );
        Log.Logger.Information(token.Value.ToString());
        var user = await Global.OAuth.API.FetchUser(token.Value);
        Log.Logger.Information(user.Value.ToString());
        var success = await Global.OAuth.API.RevokeToken(token.Value, client);







    }
}
