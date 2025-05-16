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

        var accessToken = new Global.OAuth.Token(accessToken: "YOUR TOKEN HERE", tokenType: "Bearer", refreshToken: "null", expiresIn: 1000, createdAt: DateTime.UtcNow);
        await Global.OAuth.API.fetch(accessToken);





    }
}
