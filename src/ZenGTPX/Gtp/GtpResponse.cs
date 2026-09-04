namespace ZenGTPX.Gtp;

public sealed record GtpResponse(bool IsSuccess, string? Id, string Body, bool IsEndResponse = true)
{
    public static GtpResponse Success(string? id, string body = "", bool IsEndResponse = true) => new(true, id, body, IsEndResponse);

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

        if (IsEndResponse is true)
            return firstLine + "\n\n";
        else
            return firstLine + "\n";
    }
}
