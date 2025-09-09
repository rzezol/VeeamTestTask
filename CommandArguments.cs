namespace VeeamTestTask
{
    public record CommandArguments(
        string SrcPath,
        string DstPath,
        string LogPath,
        TimeSpan SyncInterval)
    {
        const string SrcPathArgName = "--src_path=";
        const string DstPathArgName = "--dst_path=";
        const string LogPathArgName = "--log_path=";
        const string SyncIntervalArgName = "--sync_interval=";

        public static CommandArguments ReadCommandArguments(string[] args)
        {
            string? srcPathValue = null;
            string? dstPathValue = null;
            string? logPathValue = null;
            uint? syncIntervalValue = null;

            foreach (var arg in args)
            {
                if (arg.StartsWith(SrcPathArgName))
                {
                    srcPathValue = arg.Substring(SrcPathArgName.Length);
                }
                else if (arg.StartsWith(DstPathArgName))
                {
                    dstPathValue = arg.Substring(DstPathArgName.Length);
                }
                else if (arg.StartsWith(LogPathArgName))
                {
                    logPathValue = arg.Substring(LogPathArgName.Length);
                }
                else if (arg.StartsWith(SyncIntervalArgName))
                {
                    if (uint.TryParse(arg.AsSpan(SyncIntervalArgName.Length), out uint value))
                    {
                        syncIntervalValue = value;
                    }
                    else
                    {
                        throw new ArgumentException($"Can not parse '{SyncIntervalArgName}' argument with value '{arg.Substring(SyncIntervalArgName.Length)}' as integer");
                    }
                }
                else
                {
                    throw new ArgumentException($"Unknown argument '{arg}'");
                }
            }

            if (srcPathValue == null) throw new ArgumentException($"Missing argument '{SrcPathArgName}'");
            if (dstPathValue == null) throw new ArgumentException($"Missing argument '{DstPathArgName}'");
            if (logPathValue == null) throw new ArgumentException($"Missing argument '{LogPathArgName}'");
            if (!syncIntervalValue.HasValue) throw new ArgumentException($"Missing argument '{SyncIntervalArgName}'");

            return new CommandArguments(srcPathValue, dstPathValue, logPathValue, TimeSpan.FromSeconds(syncIntervalValue.Value));
        }
    }
}
