namespace Rat6.Device;

public sealed class Rat6Exception : Exception
{
    public Rat6Exception(string message) : base(message)
    {
    }

    public Rat6Exception(string message, Exception innerException) : base(message, innerException)
    {
    }
}
