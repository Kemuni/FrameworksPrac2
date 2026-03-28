namespace FrameworksPrac2.Services;

public interface IClock
{
    DateTimeOffset Now { get; }
}
