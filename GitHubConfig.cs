namespace dependabot_pr_manager;

public class GitHubConfig
{
    public string? AccessToken { get; set; }
    public string? Owner { get; set; }
    public string[]? Repositories { get; set; }
    public string DoNotAutoTagLabel { get; set; } = "do-not-auto-tag";
    public bool RemoveDotNotAutoTagLabel { get; set; } = true;
}
