namespace dependabot_pr_manager.Exceptions;

public class ConfigurationException : Exception
{
    public ConfigurationException(string? message) : base(message)
    {
    }
}
