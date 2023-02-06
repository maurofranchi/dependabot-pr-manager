using Octokit;

namespace dependabot_pr_manager;

public static class GithubPrManager
{
    public enum ReviewAction
    {
        Recreate,
        Rebase,
        Merge,
    }

    public static async Task ManagePr(
        IGitHubClient client,
        int prNumber,
        string repository,
        ICollection<int> recreatedPrs,
        ICollection<int> rebasedPrs,
        ICollection<int> dependabotPrs,
        GitHubConfig config)
    {
        var detailedPr = await client.PullRequest.Get(config.Owner, repository, prNumber);
            var mergeableState = detailedPr.MergeableState.HasValue ? detailedPr.MergeableState.Value.StringValue : "no value";
            var prTitleAndNumber = $"{Convert.ToString(detailedPr.Number)} - \"{detailedPr.Title}\"";
            Console.WriteLine($"{prTitleAndNumber} - {mergeableState}");
            try
            {
                switch (mergeableState)
                {
                    case "dirty":
                        await ReviewPr(
                            client,
                            config.Owner!,
                            repository,
                            detailedPr.Number,
                            ReviewAction.Recreate,
                            recreatedPrs);

                        break;
                    case "behind":
                        await ReviewPr(
                            client,
                            config.Owner!,
                            repository,
                            detailedPr.Number,
                            ReviewAction.Rebase,
                            rebasedPrs);

                        break;
                    case "blocked":
                    case "clean":
                        rebasedPrs.Remove(detailedPr.Number);
                        recreatedPrs.Remove(detailedPr.Number);
                        if (detailedPr.Mergeable == true)
                        {
                            await RemoveDoNotAutoTagLabel(
                                client,
                                dependabotPrs,
                                detailedPr,
                                repository,
                                config);

                            await ReviewPr(
                                client,
                                config.Owner!,
                                repository,
                                detailedPr.Number,
                                ReviewAction.Merge,
                                new List<int>());

                            dependabotPrs.Remove(prNumber);
                        }

                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to manage pr {prTitleAndNumber} - Error: {e.Message}");
            }
    }
    
    public static async Task<List<int>> GetDependabotPrs(IGitHubClient client, string owner, string repository)
    {
        var pullRequests = await client.PullRequest.GetAllForRepository(owner, repository);

        return pullRequests?.Where(x => x.State.Value == ItemState.Open && x.User.Login.Contains("dependabot"))
            .Select(x => x.Number)
            .ToList() ?? new List<int>();
    }

    private static async Task ReviewPr(
        IGitHubClient client,
        string owner,
        string repository,
        int prNumber,
        ReviewAction action,
        ICollection<int> prsToSkip)
    {
        if (!prsToSkip.Contains(prNumber))
        {
            // Recreate
            await client.PullRequest.Review.Create(owner, repository, prNumber,
                new PullRequestReviewCreate
                {
                    Body = $"@dependabot {action.ToString().ToLowerInvariant()}",
                    Event = action == ReviewAction.Merge 
                        ? PullRequestReviewEvent.Approve 
                        : PullRequestReviewEvent.Comment,
                });

            prsToSkip.Add(prNumber);
            if (action == ReviewAction.Merge)
            {
                Console.Write($"\tApproving and merging pr {prNumber} - waiting...");
                
                await WaitUntilPrIsMerged(
                    client,
                    owner,
                    repository,
                    prNumber);
            }
            else
            {
                Console.WriteLine($"\tReview pr {prNumber} with: {action.ToString()}");
            }
        }
    }

    private static async Task WaitUntilPrIsMerged(
        IGitHubClient client,
        string owner,
        string repository,
        int prNumber)
    {
        var attemptsCount = 0;

        PullRequest? pr;
        do
        {
            Console.Write("..");
            pr = await client.PullRequest.Get(owner, repository, prNumber);
            await Task.Delay(TimeSpan.FromSeconds(10));
            attemptsCount++;
        } while (!pr.Merged && attemptsCount < 20);

        Console.WriteLine(pr.Merged ? "Merged 🎉" : "Not merged yet 🤷");
    }

    private static async Task RemoveDoNotAutoTagLabel(
        IGitHubClient client,
        ICollection<int> prsList,
        PullRequest pr,
        string repository,
        GitHubConfig config)
    {
        var shouldRemoveLabel = config.RemoveDotNotAutoTagLabel
                                && prsList.Count == 1
                                && pr.Labels.Any(x => string.Equals(
                                    x.Name,
                                    config.DoNotAutoTagLabel,
                                    StringComparison.OrdinalIgnoreCase));

        if (shouldRemoveLabel)
        {
            // Remove the "do-not-auto-tag" label
            var updatedLabels = pr.Labels
                .Where(x => !string.Equals(x.Name, config.DoNotAutoTagLabel, StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Name)
                .ToArray();

            await client.Issue.Labels.ReplaceAllForIssue(
                config.Owner,
                repository,
                pr.Number,
                updatedLabels);

            Console.WriteLine($"\tRemoved \"{config.DoNotAutoTagLabel}\" label from PR {pr.Number}");
        }
    }
}
