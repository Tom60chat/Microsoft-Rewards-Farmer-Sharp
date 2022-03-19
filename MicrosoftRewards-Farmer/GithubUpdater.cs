using Octokit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

public class GithubUpdater
{
    #region Constructors
    public GithubUpdater(string repositoryOwner, string repositoryName)
    {
        RepositoryOwner = repositoryOwner;
        RepositoryName = repositoryName;
    }
    #endregion

    #region Variables
    private string RepositoryOwner;
    private string RepositoryName;
    private string LastTag;
    #endregion

    #region Methods
    /// <summary>
    /// Check if a new release is available on GitHub
    /// </summary>
    /// <returns>true a new release is available, else false</returns>
    public async Task<bool> CheckNewerVersion()
    {
        //Get all releases from GitHub
        //Source: https://octokitnet.readthedocs.io/en/latest/getting-started/
        try
        {
            GitHubClient client = new GitHubClient(new ProductHeaderValue(RepositoryName));
            IReadOnlyList<Release> releases = await client.Repository.Release.GetAll(RepositoryOwner, RepositoryName);

            foreach (var release in releases)
            {
                /*if (release.Prerelease)
                    continue;*/

                //Setup the versions
                Version latestGitHubVersion;
                if (!Version.TryParse(release.TagName, out latestGitHubVersion) &&
                    !Version.TryParse(release.Name, out latestGitHubVersion))
                    return false;
                Version localVersion = Assembly.GetExecutingAssembly().GetName().Version;

                //Compare the Versions
                //Source: https://stackoverflow.com/questions/7568147/compare-version-numbers-without-using-split-function
                int versionComparison = localVersion.CompareTo(latestGitHubVersion);

                if (versionComparison < 0)
                {
                    //The version on GitHub is more up to date than this local release.
                    LastTag = release.TagName;
                    return true;
                }
                else if (versionComparison > 0)
                {
                    //This local version is greater than the release version on GitHub.
                    return false;
                }
                else
                {
                    //This local Version and the Version on GitHub are equal.
                    return false;
                }
            }
        }
        catch (HttpRequestException) { }

        return false;
    }

    /// <summary>
    /// Open a link to the lastest release
    /// </summary>
    public void Update()
    {
        ProcessStartInfo psi = new ProcessStartInfo { UseShellExecute = true };
        psi.FileName = string.IsNullOrEmpty(LastTag) ?
            $"https://github.com/{RepositoryOwner}/{RepositoryName}/releases" :
            $"https://github.com/{RepositoryOwner}/{RepositoryName}/releases/tag/{LastTag}";
        Process.Start(psi);
    }
    #endregion
}