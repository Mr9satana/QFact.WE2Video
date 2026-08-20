using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Text;

namespace QFact.WE2Video;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        if (!OperatingSystem.IsWindows()) return 3;

        if (args.Length > 0)
        {
            EnsureConsole();
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            return CliRunner.RunAsync(args).GetAwaiter().GetResult();
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
        return 0;
    }

    private static void EnsureConsole()
    {
        if (GetConsoleWindow() == IntPtr.Zero)
        {
            AttachConsole(AttachParentProcess);
            if (GetConsoleWindow() == IntPtr.Zero) AllocConsole();
        }

        try
        {
            var stdout = new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)) { AutoFlush = true };
            var stderr = new StreamWriter(Console.OpenStandardError(), new UTF8Encoding(false)) { AutoFlush = true };
            Console.SetOut(stdout);
            Console.SetError(stderr);
        }
        catch { }
    }

    private const uint AttachParentProcess = 0xFFFFFFFF;

    [DllImport("kernel32.dll")]
    private static extern bool AttachConsole(uint dwProcessId);

    [DllImport("kernel32.dll")]
    private static extern bool AllocConsole();

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();
}
