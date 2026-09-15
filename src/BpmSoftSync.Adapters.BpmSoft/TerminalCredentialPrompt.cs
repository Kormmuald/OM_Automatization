namespace BpmSoftSync.Adapters.BpmSoft;

public interface ICredentialTerminal
{
    bool IsInteractive { get; }
    string? ReadLine();
    ConsoleKeyInfo ReadKey(bool intercept);
    void Write(string value);
    void WriteLine();
}

public sealed class ConsoleCredentialTerminal : ICredentialTerminal
{
    public bool IsInteractive => Environment.UserInteractive && !Console.IsInputRedirected;
    public string? ReadLine() => Console.ReadLine();
    public ConsoleKeyInfo ReadKey(bool intercept) => Console.ReadKey(intercept);
    public void Write(string value) => Console.Write(value);
    public void WriteLine() => Console.WriteLine();
}

public sealed class TerminalCredentialPrompt(ICredentialTerminal terminal)
{
    public TerminalCredentialPrompt() : this(new ConsoleCredentialTerminal()) { }

    public InteractiveCredentials Read()
    {
        if (!terminal.IsInteractive)
            throw new BpmSoftTransportException(BpmSoftTransportError.InteractiveTerminalRequired, "INTERACTIVE_TERMINAL_REQUIRED: credentials cannot be read from arguments, configuration, files, or redirected input.");

        terminal.Write("BPMSoft origin: ");
        var originInput = terminal.ReadLine();
        terminal.Write("Login: ");
        var loginInput = terminal.ReadLine();
        terminal.Write("Password: ");
        var password = ReadPassword();

        try
        {
            var origin = BpmSoftTargetOrigin.ParseInteractive(originInput ?? string.Empty);
            return InteractiveCredentials.Create(origin, (loginInput ?? string.Empty).AsSpan(), password);
        }
        finally
        {
            Array.Clear(password);
        }
    }

    private char[] ReadPassword()
    {
        var characters = new List<char>();
        while (true)
        {
            var key = terminal.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter) break;
            if (key.Key == ConsoleKey.Backspace)
            {
                if (characters.Count > 0) characters.RemoveAt(characters.Count - 1);
                continue;
            }
            if (!char.IsControl(key.KeyChar)) characters.Add(key.KeyChar);
        }
        terminal.WriteLine();
        return characters.ToArray();
    }
}
