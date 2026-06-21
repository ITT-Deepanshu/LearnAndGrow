namespace PRM.Domain.Entities;

public class ActivityTag
{
    private ActivityTag() { }

    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsCustom { get; private set; }

    public static ActivityTag Create(int id, string name, bool isCustom = false) =>
        new() { Id = id, Name = name, IsCustom = isCustom };
}
