namespace Store;

public readonly struct Quote
{
    public string Text { get; }
    public string Quotee { get; }

    public Quote(string text, string quotee)
    {
        Text = text;
        Quotee = quotee;
    }

}
