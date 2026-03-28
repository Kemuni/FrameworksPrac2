namespace FrameworksPrac2.Services;

public interface IStorage
{
    void Add(string value);

    IReadOnlyList<string> GetAll();
}
