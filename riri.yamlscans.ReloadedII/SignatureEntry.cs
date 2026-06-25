using RyoTune.Reloaded;

namespace riri.yamlscans.ReloadedII;

/// <summary>
/// Represents a function to signature mapping, where there are one or more signature candidates.
/// </summary>
/// <param name="name">Name of the function</param>
/// <param name="candidates">List of candidates</param>
public class SignatureEntry(string name, List<Candidate> candidates)
{
    /// <summary>
    /// Name of the function
    /// </summary>
    public string Name { get; internal set; } = name;
    private List<Candidate> Candidates { get; set; } = candidates;
    private readonly object Lock = new();
    private int ScansCompleted { get; set; } = 0;
    private bool Initialized { get; set; } = false;
    internal bool IsLatest { get; set; } = true;
    

    internal void CreateCandidateScan(Candidate candidate)
    {
        YamlScans._startupScanner!.AddMainModuleScan(candidate.Signature, result =>
        {
            // Don't do anything if this SignatureEntry has been replaced
            if (!IsLatest)
            {
                return;
            }
            lock (Lock) { ScansCompleted++; }

            if (!result.Found)
            {
                if (Initialized)
                {
                    Log.Debug($"\"{Name}\" was already found in a candidate pattern");
                } 
                else if (ScansCompleted == Candidates.Count)
                {
                    Log.Error($"Failed to find a pattern for {Name}.");   
                }
                else
                {
                    Log.Debug($"Couldn't find location for {Name} using pattern {candidate.Signature}, trying with another pattern...");
                }
                return;
            }

            var Address = nint.Zero;
            lock (Lock)
            {
                if (!Initialized)
                {
                    Address = candidate.Transformer.Transform(YamlScans.TransformProvider!, result.Offset);
                    Initialized = true;
                }
            }
            if (Address != nint.Zero)
            {
                Log.Debug($"\"{Name}\" found at 0x{Address:x}");
                YamlScans._sharedScans!.Broadcast(Name, Address);
            }
            else
            {
                Log.Debug($"\"{Name}\" was already found in a candidate pattern");
            }
        });
    }
}