namespace Mada.Simulator;

internal static class Program
{
    /// <summary>
    /// EN: Entry point. Opens the simulator window.
    /// AR: نقطة الدخول. تفتح نافذة المحاكي.
    /// </summary>
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new SimulatorForm());
    }
}
