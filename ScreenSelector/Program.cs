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
            // Lets setup open OAuth in an explicitly selected browser (including Codex's
            // in-app browser). Only the public authorization URL is printed, never tokens.
            if (args.Contains("--spotify-connect", StringComparer.OrdinalIgnoreCase))
            {
                ConnectSpotifyForSetupAsync(args).GetAwaiter().GetResult();
                return;
            }
            using var singleInstance = new Mutex(true, "ScreenSelector.SingleInstance", out var isFirstInstance);
            if (!isFirstInstance)
            {
                return;
            }

            ApplicationConfiguration.Initialize();
            Application.Run(new Form1(args.Contains("--minimized", StringComparer.OrdinalIgnoreCase)));
        }

        private static async Task ConnectSpotifyForSetupAsync(string[] args)
        {
            try
            {
                var settings = AppSettings.Load();
                var index = Array.IndexOf(args, "--client-id");
                if (index >= 0 && index + 1 < args.Length)
                {
                    settings.SpotifyClientId = args[index + 1];
                    settings.Save();
                }
                var connection = new SpotifyConnection(() => settings.SpotifyClientId);
                await connection.ConnectAsync(uri => Console.WriteLine(uri.AbsoluteUri), CancellationToken.None);
                var library = new SpotifyLibraryService(connection, () => settings);
                var playlists = await library.GetOwnedPlaylistsAsync(CancellationToken.None);
                var local = SpotifyLibraryService.ChooseDefaultPlaylist(playlists, "", "Yerli");
                var foreign = SpotifyLibraryService.ChooseDefaultPlaylist(playlists, "", "Yabancı");
                settings.SpotifyLocalPlaylistId = local?.Id ?? "";
                settings.SpotifyForeignPlaylistId = foreign?.Id ?? "";
                settings.Save();
                Console.WriteLine($"Spotify bağlı. Yerli hedefi: {local?.Name ?? "Seçilmedi"}; Yabancı hedefi: {foreign?.Name ?? "Seçilmedi"}.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Spotify bağlantısı tamamlanamadı: " + ex.Message);
                Environment.ExitCode = 1;
            }
        }
    }
}
