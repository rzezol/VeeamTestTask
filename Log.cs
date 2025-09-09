namespace VeeamTestTask
{
    public class Log
    {
        private static Log? _instance;

        public string LogPath { get; }

        private Log(string logPath)
        {
            LogPath = logPath;
        }

        public static Log Create(CommandArguments commandArguments)
        {
            if (_instance != null)
            {
                throw new InvalidOperationException("Logger already created.");
            }

            _instance = new Log(commandArguments.LogPath);
            return _instance;
        }

        public static Log Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;
                else
                    throw new InvalidOperationException("Logger not created yet.");
            }
        }

        public void AddMessage(string message)
        {
            string logLine = $"[{DateTime.Now}] {message}{Environment.NewLine}";
            Console.Write(logLine);
            File.AppendAllText(LogPath, logLine);
        }
    }
}
