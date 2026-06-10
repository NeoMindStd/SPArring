using StarAI.PracticeClient.App;

namespace StarAI.PracticeClient.Tests;

public sealed class GlobalInputActionHookTests
{
    private const int WmKeyDown = 0x0100;
    private const int WmSysKeyDown = 0x0104;
    private const int VirtualKeyF4 = 0x73;
    private const int VirtualKeyQ = 0x51;

    [Fact]
    public void InterceptsAltF4OnlyForTheCapturedPlayerStarCraftProcess()
    {
        Assert.True(GlobalInputActionHook.ShouldInterceptPlayerExitShortcut(
            WmSysKeyDown,
            VirtualKeyF4,
            altDown: true,
            foregroundProcessId: 111,
            playerStarCraftProcessId: 111));
    }

    [Theory]
    [InlineData(WmKeyDown, VirtualKeyF4, false, 111, 111)]
    [InlineData(WmSysKeyDown, VirtualKeyQ, true, 111, 111)]
    [InlineData(WmSysKeyDown, VirtualKeyF4, true, 222, 111)]
    [InlineData(WmSysKeyDown, VirtualKeyF4, true, 111, null)]
    public void DoesNotInterceptOtherKeyboardInput(
        int message,
        int virtualKeyCode,
        bool altDown,
        int? foregroundProcessId,
        int? playerStarCraftProcessId)
    {
        Assert.False(GlobalInputActionHook.ShouldInterceptPlayerExitShortcut(
            message,
            virtualKeyCode,
            altDown,
            foregroundProcessId,
            playerStarCraftProcessId));
    }
}
