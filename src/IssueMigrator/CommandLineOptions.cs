
using CommandLine;
using CommandLine.Text;

namespace CodePlexIssueMigrator
{
	/// <summary>
	/// The options class which contains the different options to CodePlex to GitHub migration utility.
	/// </summary>
	class CommandLineOptions
	{
		#region HelpText

		const string CodeplexProjectHelpText = "The name of the CodePlex project to migrate.";
		const string GitHubRepositoryHelpText = "The name of the GitHub repository.";
		const string GitHubOwnerHelpText = "The name of the dump GitHub repository owner/organization.";
		const string GitHubAccessTokenHelpText = "The name of the dump GitHub Access Token.";

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
	}
}
