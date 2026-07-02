using Sparring.Client;

namespace Sparring.Tests;

public sealed class WindowsApplicationErrorDialogCloserTests
{
    [Theory]
    [InlineData("Windows - 응용 프로그램 오류")]
    [InlineData("Windows - Application Error")]
    public void IsApplicationErrorTitleRecognizesWindowsErrorDialogs(string title)
    {
        Assert.True(WindowsApplicationErrorDialogCloser.IsApplicationErrorTitle(title));
    }

    [Fact]
    public void IsApplicationErrorTitleIgnoresOtherWindows()
    {
        Assert.False(WindowsApplicationErrorDialogCloser.IsApplicationErrorTitle("Brood War"));
    }
}
