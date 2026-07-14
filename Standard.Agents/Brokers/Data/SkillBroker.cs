namespace Standard.Agents.Brokers.Data;

// Liaison to ONE resource: the skill files on disk. Reads every *.md, orders by
// name, composes the instruction document. It only reads; a LINQ query, not a loop.
public sealed class SkillBroker : ISkillBroker
{
    private readonly string skillsPath;

    public SkillBroker(string skillsPath) =>
        this.skillsPath = Path.Combine(AppContext.BaseDirectory, skillsPath);

    public ValueTask<string> SelectSkillsAsync() =>
        ValueTask.FromResult(
            string.Join(
                "\n\n",
                Directory
                    .EnumerateFiles(this.skillsPath, "*.md")
                    .OrderBy(path => path)
                    .Select(File.ReadAllText)));
}
