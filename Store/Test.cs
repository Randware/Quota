namespace Store;

public static class Test
{
    public static void Main(string[] args)
    {
        Storage storage = new Storage(123);
        storage.Delete();
    }
}