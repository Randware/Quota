namespace Store;

public static class Test
{
    public static void Main(string[] args)
    {
        Storage storage = new Storage(123);

        uint id = storage.SaveQuote(new Quote("Das ist ein Test", "Darius"));

        storage.SaveQuote(new Quote("Das ist ein Test", "Darius"));
    }
}
        
