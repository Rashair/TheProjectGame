namespace Shared.Payloads.GMPayloads;

public class NotWaitedErrorPayload : Payload
{
    public int WaitFor { get; set; }

    public override string ToString()
    {
        return $"WaitFor:{WaitFor}";
    }
}
