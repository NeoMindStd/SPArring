namespace StarAI.PracticeClient.App;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        using var mutex = new Mutex(initiallyOwned: true, "Global\\AIStarClient-PracticeClient", out var createdNew);
        if (!createdNew)
        {
            MessageBox.Show(
                "StarAI 연습 클라이언트가 이미 실행 중입니다. 기존 창을 닫고 다시 실행해 주세요.",
                "StarAI 연습 클라이언트",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }    
}
