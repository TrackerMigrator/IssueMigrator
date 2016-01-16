﻿
using CommandLine;
using CommandLine.Text;

namespace CodePlexIssueMigrator
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
		const string IssueNumberTokenHelpText = "Only migrate numbered issue.";

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

		[Option("key", Required = true, HelpText = GitHubAccessTokenHelpText)]
		public string GitHubAccessToken { get; set; }

		[Option("issue", HelpText = IssueNumberTokenHelpText)]
		public int? IssueNumber { get; set; }
	}
}
