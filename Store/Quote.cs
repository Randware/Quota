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

    public override string ToString()
    {
        return $"text: {Text}, quotee: {Quotee}";
    }
}
