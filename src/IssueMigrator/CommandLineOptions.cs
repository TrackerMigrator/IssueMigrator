
using CommandLine;
using CommandLine.Text;

namespace IssueMigrator
{
	/// <summary>
	/// The options class which contains the different options to CodePlex to GitHub migration utility.
	/// </summary>
	public class CommandLineOptions
	{
		#region HelpText

		const string CodeplexProjectHelpText = "CodePlex project to migrate.";
		const string GitHubRepositoryHelpText = "GitHub repository.";
		const string GitHubOwnerHelpText = "GitHub repository owner/organization.";
		const string GitHubAccessTokenHelpText = "GitHub Access Token.";
		const string IssueNumberTokenHelpText = "Only migrate selected issue.";
		const string RateLimitHelpText = "Number of calls to GitHubApi to make pause.";
		const string RatePauseHelpText = "Pause in seconds.";
		const string AddCodePlexLabelHelpText = @"Indicates if we should add ""CodePlex"" label.";

		[HelpOption]
		public string GetHelp()
		{
			return HelpText.AutoBuild(this, c => HelpText.DefaultParsingErrorsHandler(this, c));
		}

		#endregion

		[Option("from", Required = true, HelpText = CodeplexProjectHelpText)]
		public string CodeplexProject { get; set; }

		[Option("to", Required = true, HelpText = GitHubRepositoryHelpText)]
		public string GitHubRepository { get; set; }

		[Option("owner", Required = true, HelpText = GitHubOwnerHelpText)]
		public string GitHubOwner { get; set; }

		[Option("token", Required = true, HelpText = GitHubAccessTokenHelpText)]
		public string GitHubAccessToken { get; set; }

		[Option("issue", HelpText = IssueNumberTokenHelpText)]
		public int? IssueNumber { get; set; }

		[Option("rate-limit", DefaultValue = 0, HelpText = RateLimitHelpText)]
		public int RateLimit { get; set; }

		[Option("rate-pause", DefaultValue = 60, HelpText = RatePauseHelpText)]
		public int RatePause { get; set; }

		[Option("add-codeplex", DefaultValue = false, HelpText = AddCodePlexLabelHelpText)]
		public bool AddCodePlexLabel { get; set; }
	}
}
