using RyoTune.Reloaded;

namespace riri.yamlscans.ReloadedII;

public class SignatureEntry(string name, List<Candidate> candidates)
{
    public string Name { get; internal set; } = name;
    private List<Candidate> Candidates { get; set; } = candidates;
    private readonly object Lock = new();
    private int ScansCompleted { get; set; } = 0;
    private bool Initialized { get; set; } = false;
    internal bool IsLatest { get; set; } = true;
    

    internal void CreateCandidateScan(Candidate candidate)
    {
        YamlScans._startupScanner.AddMainModuleScan(candidate.Signature, result =>
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
                    Log.Debug($"\"{name}\" was already found in a candidate pattern");
                } 
                else if (ScansCompleted == Candidates.Count)
                {
                    Log.Error($"Failed to find a pattern for {name}.");   
                }
                else
                {
                    Log.Debug($"Couldn't find location for {name} using pattern {candidate.Signature}, trying with another pattern...");
                }
                return;
            }

            var Address = nint.Zero;
            lock (Lock)
            {
                if (!Initialized)
                {
                    Address = candidate.Transformer.Transform(YamlScans.TransformProvider, result.Offset);
                    Initialized = true;
                }
            }
            if (Address != nint.Zero)
            {
                Log.Debug($"\"{name}\" found at 0x{Address:x}");
                YamlScans._sharedScans.Broadcast(name, Address);
            }
            else
            {
                Log.Debug($"\"{name}\" was already found in a candidate pattern");
            }
        });
    }
}