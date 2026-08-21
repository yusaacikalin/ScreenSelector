namespace ScreenSelector
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            using var singleInstance = new Mutex(true, "ScreenSelector.SingleInstance", out var isFirstInstance);
            if (!isFirstInstance)
            {
                return;
            }

            ApplicationConfiguration.Initialize();
            Application.Run(new Form1(args.Contains("--minimized", StringComparer.OrdinalIgnoreCase)));
        }
    }
}
