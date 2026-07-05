namespace ZenGTPX.Gtp;

public sealed record GtpResponse(bool IsSuccess, string? Id, string Body)
{
    public static GtpResponse Success(string? id, string body = "") => new(true, id, body);

    public static GtpResponse Error(string? id, string body) => new(false, id, body);

    public string Format()
    {
        var marker = IsSuccess ? "=" : "?";
        var prefix = Id is null ? marker : marker + Id;
        var firstLine = Body switch
        {
            "" => prefix,
            ['\n', ..] => prefix + Body,
            _ => prefix + " " + Body,
        };
        return firstLine + "\n\n";
    }
}
