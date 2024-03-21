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
    var dependabotPrs = await GithubPrManager.GetDependabotPrs(client, config.Owner!, repository);

    var recreatedPrs = new List<int>();
    var approvedPrs = new List<int>();
    do
    {
        for (var i = 0; i < dependabotPrs.Count; i++)
        {
            await GithubPrManager.ManagePr(
                client,
                dependabotPrs[i],
                repository,
                recreatedPrs,
                approvedPrs,
                dependabotPrs,
                config);
        }

        if (dependabotPrs.Count > 0)
        {
            Console.WriteLine("Sleep for 60s");
            await Task.Delay(TimeSpan.FromSeconds(60));
        }

    } while (dependabotPrs.Any());

    Console.WriteLine($"All done for {repository} 🎉");
}

