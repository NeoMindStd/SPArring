using Sparring.Client;
using Sparring.Core;

namespace Sparring.Tests;

public sealed class HotkeyCommandLayoutTests
{
    [Fact]
    public void TerranWorkerBuildCommandsAreGroupedUnderScvPages()
    {
        var barracks = Entry("terran_build_barracks", "Build Barracks");
        var factory = Entry("terran_build_factory", "Build Factory");

        var barracksObject = HotkeyCommandLayout.ObjectFor(barracks);
        var factoryObject = HotkeyCommandLayout.ObjectFor(factory);

        Assert.Equal("SCV", barracksObject.DisplayName);
        Assert.Equal("유닛", barracksObject.Category);
        Assert.Equal(barracksObject.Key, factoryObject.Key);
        Assert.Equal(HotkeyCommandLayout.PageBasicStructures, HotkeyCommandLayout.PageName(barracks));
        Assert.Equal(3, HotkeyCommandLayout.Slot(barracks));
        Assert.Equal(HotkeyCommandLayout.PageAdvancedStructures, HotkeyCommandLayout.PageName(factory));
        Assert.Equal(0, HotkeyCommandLayout.Slot(factory));
    }

    [Fact]
    public void WorkerCommandCardKeepsGeneralActionsInGameOrder()
    {
        var move = Entry("general_cmd_move", "Move");
        var attack = Entry("general_cmd_attack", "Attack");
        var repair = Entry("terran_cmd_repair", "Repair");
        var build = Entry("terran_cmd_buildstruc", "Build Structure");

        Assert.Equal(HotkeyCommandLayout.PageGeneral, HotkeyCommandLayout.PageName(move));
        Assert.Equal(0, HotkeyCommandLayout.Slot(move));
        Assert.Equal(2, HotkeyCommandLayout.Slot(attack));
        Assert.Equal(3, HotkeyCommandLayout.Slot(repair));
        Assert.Equal(7, HotkeyCommandLayout.Slot(build));
    }

    [Fact]
    public void ProtossAndZergBuildCommandsUseWorkerObjects()
    {
        var gateway = Entry("protoss_build_gateway", "Warp in Gateway");
        var spawningPool = Entry("zerg_build_spawningpool", "Mutate into Spawning Pool");

        Assert.Equal("Probe", HotkeyCommandLayout.ObjectFor(gateway).DisplayName);
        Assert.Equal("Drone", HotkeyCommandLayout.ObjectFor(spawningPool).DisplayName);
        Assert.Equal(HotkeyCommandLayout.PageBasicStructures, HotkeyCommandLayout.PageName(gateway));
        Assert.Equal(3, HotkeyCommandLayout.Slot(gateway));
        Assert.Equal(HotkeyCommandLayout.PageBasicStructures, HotkeyCommandLayout.PageName(spawningPool));
        Assert.Equal(2, HotkeyCommandLayout.Slot(spawningPool));
    }

    private static HotkeyEntry Entry(string commandId, string description)
    {
        return new HotkeyEntry
        {
            StringId = 1,
            CommandId = commandId,
            Description = description,
            Hotkey = commandId[..1],
            DefaultText = description,
            CurrentText = description
        };
    }
}
