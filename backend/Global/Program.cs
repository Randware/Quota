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

        var token = await Global.OAuth.API.GetToken(clientID: "YOUR-CLIENT-ID-HERE",
            clientSecret: "YOU-CLIENT-SECRET-HERE",
            code: "YOUR-CODE-HERE",
            redirectUri: "YOUR-REDIRECT-CODE-HERE");
        Log.Logger.Information(token.Value.ToString());
        var user = await Global.OAuth.API.FetchUser(token.Value);
        Log.Logger.Information(user.Value.ToString());







    }
}
