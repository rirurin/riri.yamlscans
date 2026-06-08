namespace riri.yamlscans.Tests;

[TestClass]
public sealed class YamlScanTests
{
    private static string INI_SCAN_SIMPLE =
        """
        - ULevelStreamingDynamic_LoadLevelInstance: "E8 ?? ?? ?? ?? 48 8B 4D ?? 49 89 06 48 85 C9 74 ?? E8 ?? ?? ?? ?? 48 8B 4D ?? 48 85 C9 74 ?? E8 ?? ?? ?? ?? 48 8B 9C 24 ?? ?? ?? ??"
        - ULevelStreaming_GetStreamingLevel: "48 89 54 24 ?? 55 53 56 57 41 55 41 56 41 57 48 8B EC 48 83 EC 40"
        """;

    [TestMethod]
    public void IniScanSimple()
    {
        var Model = ScanModel.FromString(INI_SCAN_SIMPLE);
        Assert.AreEqual("ULevelStreamingDynamic_LoadLevelInstance", Model.Entries[0].Key);
        Assert.AreEqual("E8 ?? ?? ?? ?? 48 8B 4D ?? 49 89 06 48 85 C9 74 ?? E8 ?? ?? ?? ?? 48 8B 4D ?? 48 85 C9 74 ?? E8 ?? ?? ?? ?? 48 8B 9C 24 ?? ?? ?? ??", Model.Entries[0].Candidates[0].Signature);
        Assert.AreEqual(new GetDirectAddress(), Model.Entries[0].Candidates[0].Transformer);
        
        Assert.AreEqual("ULevelStreaming_GetStreamingLevel", Model.Entries[1].Key);
        Assert.AreEqual("48 89 54 24 ?? 55 53 56 57 41 55 41 56 41 57 48 8B EC 48 83 EC 40", Model.Entries[1].Candidates[0].Signature);
        Assert.AreEqual(new GetDirectAddress(), Model.Entries[1].Candidates[0].Transformer);
    }

    private static string INI_SCAN_PRESET_TRANSFORM =
        """
        - ULevelStreamingDynamic_LoadLevelInstance: "E8 ?? ?? ?? ?? 48 8B 4D ?? 49 89 06 48 85 C9 74 ?? E8 ?? ?? ?? ?? 48 8B 4D ?? 48 85 C9 74 ?? E8 ?? ?? ?? ?? 48 8B 9C 24 ?? ?? ?? ??"
        - ULevelStreamingDynamic_LoadLevelInstance_RESULT: "GetIndirectAddressShort"
        """;
    
    [TestMethod]
    public void IniScanPresetTransform()
    {
        var Model = ScanModel.FromString(INI_SCAN_PRESET_TRANSFORM);
        Assert.AreEqual("ULevelStreamingDynamic_LoadLevelInstance", Model.Entries[0].Key);
        Assert.AreEqual("E8 ?? ?? ?? ?? 48 8B 4D ?? 49 89 06 48 85 C9 74 ?? E8 ?? ?? ?? ?? 48 8B 4D ?? 48 85 C9 74 ?? E8 ?? ?? ?? ?? 48 8B 9C 24 ?? ?? ?? ??", Model.Entries[0].Candidates[0].Signature);
        Assert.AreEqual(new GetIndirectAddressShort(), Model.Entries[0].Candidates[0].Transformer);
    }

    private static string INI_SCAN_PRESET_CUSTOM =
        """
        - ULevelStreamingDynamic_LoadLevelInstance: "E8 ?? ?? ?? ?? 48 8B 4D ?? 49 89 06 48 85 C9 74 ?? E8 ?? ?? ?? ?? 48 8B 4D ?? 48 85 C9 74 ?? E8 ?? ?? ?? ?? 48 8B 9C 24 ?? ?? ?? ??"
        - ULevelStreamingDynamic_LoadLevelInstance_RESULT: "GetGlobalAddress(result + 1)"
        """;
    
    [TestMethod]
    public void IniScanPresetCustom()
    {
        var Model = ScanModel.FromString(INI_SCAN_PRESET_CUSTOM);
        Assert.AreEqual("ULevelStreamingDynamic_LoadLevelInstance", Model.Entries[0].Key);
        Assert.AreEqual("E8 ?? ?? ?? ?? 48 8B 4D ?? 49 89 06 48 85 C9 74 ?? E8 ?? ?? ?? ?? 48 8B 4D ?? 48 85 C9 74 ?? E8 ?? ?? ?? ?? 48 8B 9C 24 ?? ?? ?? ??", Model.Entries[0].Candidates[0].Signature);
        Assert.AreEqual(new CustomExpression("GetGlobalAddress(result + 1)"), Model.Entries[0].Candidates[0].Transformer);
    }
    
    private static string INI_SCAN_DISABLED =
        """
        - ULevelStreamingDynamic_LoadLevelInstance: "DISABLED"
        """;

    [TestMethod]
    public void IniScanDisabled()
    {
        var Model = ScanModel.FromString(INI_SCAN_DISABLED);
        Assert.IsEmpty(Model.Entries);
    }
    
    private static string INI_SCAN_MULTIPLE =
        """
        - ULevelStreamingDynamic_LoadLevelInstance: ["E8 ?? ?? ?? ?? 48 8B 4D ?? 49 89 06 48 85 C9 74 ?? E8 ?? ?? ?? ?? 48 8B 4D ?? 48 85 C9 74 ?? E8 ?? ?? ?? ?? 48 8B 9C 24 ?? ?? ?? ??", "48 89 54 24 ?? 55 53 56 57 41 55 41 56 41 57 48 8B EC 48 83 EC 40"]
        """;
    
    [TestMethod]
    public void IniScanMultiple()
    {
        var Model = ScanModel.FromString(INI_SCAN_MULTIPLE);
        Assert.AreEqual("ULevelStreamingDynamic_LoadLevelInstance", Model.Entries[0].Key);
        Assert.AreEqual("E8 ?? ?? ?? ?? 48 8B 4D ?? 49 89 06 48 85 C9 74 ?? E8 ?? ?? ?? ?? 48 8B 4D ?? 48 85 C9 74 ?? E8 ?? ?? ?? ?? 48 8B 9C 24 ?? ?? ?? ??", Model.Entries[0].Candidates[0].Signature);
        Assert.AreEqual(new GetDirectAddress(), Model.Entries[0].Candidates[0].Transformer);
        Assert.AreEqual("48 89 54 24 ?? 55 53 56 57 41 55 41 56 41 57 48 8B EC 48 83 EC 40", Model.Entries[0].Candidates[1].Signature);
        Assert.AreEqual(new GetDirectAddress(), Model.Entries[0].Candidates[1].Transformer);
    }
    
    private static string INI_SCAN_MULTIPLE_TRANSFORMS =
        """
        - ULevelStreamingDynamic_LoadLevelInstance: ["E8 ?? ?? ?? ?? 48 8B 4D ?? 49 89 06 48 85 C9 74 ?? E8 ?? ?? ?? ?? 48 8B 4D ?? 48 85 C9 74 ?? E8 ?? ?? ?? ?? 48 8B 9C 24 ?? ?? ?? ??", "48 89 54 24 ?? 55 53 56 57 41 55 41 56 41 57 48 8B EC 48 83 EC 40"]
        - ULevelStreamingDynamic_LoadLevelInstance_RESULT: ["GetIndirectAddressShort2", "GetGlobalAddress(result + 2)"]
        """;
    
    [TestMethod]
    public void IniScanMultipleTransforms()
    {
        var Model = ScanModel.FromString(INI_SCAN_MULTIPLE_TRANSFORMS);
        Assert.AreEqual("ULevelStreamingDynamic_LoadLevelInstance", Model.Entries[0].Key);
        Assert.AreEqual("E8 ?? ?? ?? ?? 48 8B 4D ?? 49 89 06 48 85 C9 74 ?? E8 ?? ?? ?? ?? 48 8B 4D ?? 48 85 C9 74 ?? E8 ?? ?? ?? ?? 48 8B 9C 24 ?? ?? ?? ??", Model.Entries[0].Candidates[0].Signature);
        Assert.AreEqual(new GetIndirectAddressShort2(), Model.Entries[0].Candidates[0].Transformer);
        Assert.AreEqual("48 89 54 24 ?? 55 53 56 57 41 55 41 56 41 57 48 8B EC 48 83 EC 40", Model.Entries[0].Candidates[1].Signature);
        Assert.AreEqual(new CustomExpression("GetGlobalAddress(result + 2)"), Model.Entries[0].Candidates[1].Transformer);
    }

    private static string YAML_SCAN_SIMPLE_SINGLE =
        """
        - ULevelStreamingDynamic_LoadLevelInstance: 
            - signatures: "E8 ?? ?? ?? ?? 48 8B 4D ?? 49 89 06 48 85 C9 74 ?? E8 ?? ?? ?? ?? 48 8B 4D ?? 48 85 C9 74 ?? E8 ?? ?? ?? ?? 48 8B 9C 24 ?? ?? ?? ??"
        - ULevelStreaming_GetStreamingLevel: 
            - signatures: "48 89 54 24 ?? 55 53 56 57 41 55 41 56 41 57 48 8B EC 48 83 EC 40"
        """;

    [TestMethod]
    public void YamlScanSimple()
    {
        var Model = ScanModel.FromString(YAML_SCAN_SIMPLE_SINGLE);
        Assert.AreEqual("ULevelStreamingDynamic_LoadLevelInstance", Model.Entries[0].Key);
        Assert.AreEqual("E8 ?? ?? ?? ?? 48 8B 4D ?? 49 89 06 48 85 C9 74 ?? E8 ?? ?? ?? ?? 48 8B 4D ?? 48 85 C9 74 ?? E8 ?? ?? ?? ?? 48 8B 9C 24 ?? ?? ?? ??", Model.Entries[0].Candidates[0].Signature);
        Assert.AreEqual(new GetDirectAddress(), Model.Entries[0].Candidates[0].Transformer);
        Assert.AreEqual("ULevelStreaming_GetStreamingLevel", Model.Entries[1].Key);
        Assert.AreEqual("48 89 54 24 ?? 55 53 56 57 41 55 41 56 41 57 48 8B EC 48 83 EC 40", Model.Entries[1].Candidates[0].Signature);
        Assert.AreEqual(new GetDirectAddress(), Model.Entries[1].Candidates[0].Transformer);
    }
    
    private static string YAML_SCAN_TRANSFORMS =
        """
        - ULevelStreamingDynamic_LoadLevelInstance: 
            - signatures: "E8 ?? ?? ?? ?? 48 8B 4D ?? 49 89 06 48 85 C9 74 ?? E8 ?? ?? ?? ?? 48 8B 4D ?? 48 85 C9 74 ?? E8 ?? ?? ?? ?? 48 8B 9C 24 ?? ?? ?? ??"
            - transforms: "GetIndirectAddressShort"
        - ULevelStreaming_GetStreamingLevel: 
            - signatures: "48 89 54 24 ?? 55 53 56 57 41 55 41 56 41 57 48 8B EC 48 83 EC 40"
            - transforms: "GetGlobalAddress(result + 1)"
        """;

    [TestMethod]
    public void YamlScanTransforms()
    {
        var Model = ScanModel.FromString(YAML_SCAN_TRANSFORMS);
        Assert.AreEqual("ULevelStreamingDynamic_LoadLevelInstance", Model.Entries[0].Key);
        Assert.AreEqual("E8 ?? ?? ?? ?? 48 8B 4D ?? 49 89 06 48 85 C9 74 ?? E8 ?? ?? ?? ?? 48 8B 4D ?? 48 85 C9 74 ?? E8 ?? ?? ?? ?? 48 8B 9C 24 ?? ?? ?? ??", Model.Entries[0].Candidates[0].Signature);
        Assert.AreEqual(new GetIndirectAddressShort(), Model.Entries[0].Candidates[0].Transformer);
        Assert.AreEqual("ULevelStreaming_GetStreamingLevel", Model.Entries[1].Key);
        Assert.AreEqual("48 89 54 24 ?? 55 53 56 57 41 55 41 56 41 57 48 8B EC 48 83 EC 40", Model.Entries[1].Candidates[0].Signature);
        Assert.AreEqual(new CustomExpression("GetGlobalAddress(result + 1)"), Model.Entries[1].Candidates[0].Transformer);
    }

    private static string YAML_SCAN_MULTIPLE =
        """
        - UAtlEvtSubsystem_DoesLevelStreamingLevelExist:
            - signatures: ["48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 40 4C 89 C7", "48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 40 49 8B F8 48 85 D2"]
        - ULevelStreamingDynamic_LoadLevelInstance: 
            - signatures: ["E8 ?? ?? ?? ?? 48 8B 4D ?? 49 89 06 48 85 C9 74 ?? E8 ?? ?? ?? ?? 48 8B 4D ?? 48 85 C9 74 ?? E8 ?? ?? ?? ?? 48 8B 9C 24 ?? ?? ?? ??", "48 89 54 24 ?? 55 53 56 57 41 55 41 56 41 57 48 8B EC 48 83 EC 40"]
            - transforms: ["GetIndirectAddressShort2", "GetGlobalAddress(result + 2)"]
        """;

    [TestMethod]
    public void YamlMultipleSignatures()
    {
        var Model = ScanModel.FromString(YAML_SCAN_MULTIPLE);
        Assert.AreEqual("UAtlEvtSubsystem_DoesLevelStreamingLevelExist", Model.Entries[0].Key);
        Assert.AreEqual("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 40 4C 89 C7", Model.Entries[0].Candidates[0].Signature);
        Assert.AreEqual(new GetDirectAddress(), Model.Entries[0].Candidates[0].Transformer);
        Assert.AreEqual("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 40 49 8B F8 48 85 D2", Model.Entries[0].Candidates[1].Signature);
        Assert.AreEqual(new GetDirectAddress(), Model.Entries[0].Candidates[1].Transformer);
        
        Assert.AreEqual("ULevelStreamingDynamic_LoadLevelInstance", Model.Entries[1].Key);
        Assert.AreEqual("E8 ?? ?? ?? ?? 48 8B 4D ?? 49 89 06 48 85 C9 74 ?? E8 ?? ?? ?? ?? 48 8B 4D ?? 48 85 C9 74 ?? E8 ?? ?? ?? ?? 48 8B 9C 24 ?? ?? ?? ??", Model.Entries[1].Candidates[0].Signature);
        Assert.AreEqual(new GetIndirectAddressShort2(), Model.Entries[1].Candidates[0].Transformer);
        Assert.AreEqual("48 89 54 24 ?? 55 53 56 57 41 55 41 56 41 57 48 8B EC 48 83 EC 40", Model.Entries[1].Candidates[1].Signature);
        Assert.AreEqual(new CustomExpression("GetGlobalAddress(result + 2)"), Model.Entries[1].Candidates[1].Transformer);
    }
}