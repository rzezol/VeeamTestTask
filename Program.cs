using VeeamTestTask;

internal class Program
{
    private static void Main(string[] args)
    {
        CommandArguments commandArguments = CommandArguments.ReadCommandArguments(args);

        Log.Create(commandArguments);

        Synchronizer synchronizer = new(commandArguments);
        synchronizer.Run();
    }
}