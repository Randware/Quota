namespace Store;

public readonly struct Quote
{
    public string Quotee { get; init; }

    public string Text { get; init; }

    public Quote(string quotee, string text)
    {
        Quotee = quotee;
        Text = text;
    }

}
