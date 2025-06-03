using System.Threading.Tasks;
using Common.OAuth;

namespace Main;

public class Program
{
    public static async Task Main(string[] args)
    {


        Common.Log.SetupLogging("./logs");
        // Start the API in a background task
        var apiTask = Task.Run(() => API.Program.StartAsync());

        // You can start other services (e.g., Bot) here in the future
        // var botTask = Task.Run(() => Bot.Program.StartAsync());

        Console.WriteLine("System started. Press Ctrl+C to exit.");
        await apiTask;
    }
}

