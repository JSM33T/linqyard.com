namespace Linqyard.Entities;

public sealed class AutoGroupingSettings
{
    public bool Enabled { get; set; } = true;
    public Dictionary<string, AutoGroupDefinition> Groups { get; set; } = new();
}

public sealed class AutoGroupDefinition
{
    public string Description { get; set; } = string.Empty;
    public List<string> Domains { get; set; } = new();
}
