using dependabot_pr_manager;
using dependabot_pr_manager.Extensions;
using Octokit;

var config = (new GitHubConfig()).Load();
config.Validate();

var tokenAuth = new Credentials(config!.AccessToken);
var client = new GitHubClient(new ProductHeaderValue("dependabot-pr-manager"))
{
    Credentials = tokenAuth,
};

var repositories = config.Repositories!.Distinct();
foreach (var repository in repositories)
{
    Console.WriteLine($"======== {repository.ToUpper()} ========");
    var dependabotPrs = new List<int>();
    try
    {
        dependabotPrs = await GithubPrManager.GetDependabotPrs(client, config.Owner!, repository);
    }
    catch (Exception e)
    {
        Console.Error.WriteLine($"Could not get the list of PRs from the repository {config.Owner}/{repository} - Error: {e.Message}");
        continue;
    }

    var recreatedPrs = new List<int>();
    var rebasedPrs = new List<int>();
    do
    {
        foreach (var pr in dependabotPrs)
        {
            await GithubPrManager.ManagePr(
                client,
                pr,
                repository,
                recreatedPrs,
                rebasedPrs,
                dependabotPrs, 
                config);
        }

        if (dependabotPrs.Count > 0)
        {
            Console.WriteLine("Sleep for 10s");
            await Task.Delay(TimeSpan.FromSeconds(10));
        }
    } while (dependabotPrs.Any());

    Console.WriteLine($"All done for {repository} 🎉");
}
