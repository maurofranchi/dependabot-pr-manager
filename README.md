# Dependabot PRs Manager

This small console app manages the dependabot PRs of list of repositories or a given repository.

The tool goes through all the configured repositories and for a each of them fetches the PRs raised by dependabot and executes one of the following actions:
- if the PR is `green` (meaning it can be merged), the tool approves it and asks `dependabot` to merge it. After sending the command, the tool waits until the PR is merged. 
- if the PR is `behind` it comments on it asking `dependabot` to rebase it
- if the PR is marked as `dirty`, the tool asks `dependabot` to recreate it.

# Dependencies

- Download and install dotnet sdk v7.0.201+
- Create a [github access_token](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/creating-a-personal-access-token) 
  - Allow all permissions except delete repo
  - Classic token is fine
- Authorise your github personal token
  - There is a `configure sso` button on your github access token page that needs to be used

# How to install it

You can either install it from `nuget` with the command:

```
dotnet install dependabot-pr-manager --global
```

or from a local build with the command:

```
dotnet tool install dependabot-pr-manager --add-source .\nupkg --global
```

# How to use it

From the solution run the command:

```
dotnet run
```

In case you have installed the global tool you can run the following command:

```
manage-dbot-prs
```

At the very first run you will be prompted to configure the tool with:
- Github access token with `repo` permissions
- the github owner/org that owns the repositories
- the list of repositories (comma separated)

(the following options were added just for a personal use-case 😅)
- whether you want the tool to remove the `do-not-auto-tag` label on the last PR of a repo
- the label `do-not-auto-tag` label's name in case it is different from the default one

# Known Issues

Sometimes dependabot gets stuck and doesn't apply the `rebase` or `recreate` operation. In this case we suggest to manually merge one PR to trigger the checks on the open PRs.
