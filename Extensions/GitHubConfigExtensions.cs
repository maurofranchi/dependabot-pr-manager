using System.Text.Json;
using dependabot_pr_manager.Exceptions;

namespace dependabot_pr_manager.Extensions;

public static class GitHubConfigExtensions
{
    public static void Validate(this GitHubConfig? config)
    {
        if (config == null)
        {
            throw new ConfigurationException("No configuration loaded for github");
        }
        
        if (string.IsNullOrWhiteSpace(config.AccessToken))
        {
            Console.Error.WriteLine("The GH access token is required");
            throw new ConfigurationException("Missing access token");
        }
        
        if (string.IsNullOrWhiteSpace(config.Owner))
        {
            Console.Error.WriteLine("The GH owner is required");
            throw new ConfigurationException("Missing owner name");
        }
        
        if (config.Repositories is not { Length: > 0 })
        {
            Console.Error.WriteLine("The list of GH repositories to check is required");
            throw new ConfigurationException("Missing list of repositories");
        }
    }

    public static GitHubConfig? Load(this GitHubConfig? config)
    {
        try 
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "githubConfig.json");

            if (File.Exists(filePath))
            {
                config = JsonSerializer.Deserialize<GitHubConfig>(File.ReadAllText(filePath));
            }
            
            var saveConfig = false;
            
            if (config == null)
            {
                saveConfig = true;
                config = new GitHubConfig();
            }
            
            if (string.IsNullOrWhiteSpace(config.AccessToken))
            {
                Console.Write("The GH access token is required. Please enter it: ");
                config.AccessToken = Console.ReadLine();
                saveConfig = true;
            }
        
            if (string.IsNullOrWhiteSpace(config.Owner))
            {
                Console.Write("The GH owner is required. Please enter it: ");
                config.Owner = Console.ReadLine();
                saveConfig = true;
            }
        
            if (config.Repositories is not { Length: > 0 })
            {
                Console.Write("The list of GH repositories to check is required. Please enter the list of repositories (separate them with a ','): ");
                config.Repositories = Console.ReadLine()?.Split(",").ToArray() ?? Array.Empty<string>();
                for (var i = 0; i < config.Repositories.Length; i++)
                {
                    config.Repositories[i] = config.Repositories[i].Trim();
                }
                
                saveConfig = true;
            }

            if (saveConfig)
            {
                Console.Write("Do you want to remove the do-not-auto-tag label from the last PR of the repo (y|n)? ");
                
                bool exitLoop = true;
                do
                {
                    var response = Console.ReadLine();

                    if (response?.Equals("y", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        config.RemoveDotNotAutoTagLabel = true;
                        exitLoop = true;
                    }
                    else if (response?.Equals("n", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        config.RemoveDotNotAutoTagLabel = false;
                        exitLoop = true;
                    }
                    else
                    {
                        Console.Write("Please enter either Y for yes or N for no: ");
                        exitLoop = false;
                    }
                } while (!exitLoop);

                if (config.RemoveDotNotAutoTagLabel)
                {
                    Console.Write("Enter the label to remove on the last PR of a repository (default: 'do-not-auto-tag', press 'enter'): ");

                    var label = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(label))
                    {
                        config.DoNotAutoTagLabel = label;
                    }
                }
            }

            if (saveConfig)
            {
                config.Save();
            }
        }
        catch (Exception ex) 
        {
            Console.Error.WriteLine($"Error reading the github settings: {ex.Message}");
        }

        return config;
    }

    private static void Save(this GitHubConfig config)
    {
        try 
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "githubConfig.json");
            
            File.WriteAllText(filePath, JsonSerializer.Serialize(config));
        }
        catch (Exception ex) 
        {
            Console.Error.WriteLine($"Error writing the github settings: {ex.Message}");
        }
    }
}
