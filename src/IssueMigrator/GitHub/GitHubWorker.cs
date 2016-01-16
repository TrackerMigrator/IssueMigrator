
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

using Octokit;

using CodePlexIssueMigrator.CodePlex;

namespace CodePlexIssueMigrator.GitHub
{
	public class GitHubWorker
	{
		/// <summary>GitHub API Client.</summary>
		private GitHubClient _gitHubClient;

		/// <summary>Command line arguments.</summary>
		private readonly CommandLineOptions _options;

		public GitHubWorker(CommandLineOptions options)
		{
			_options = options;

			var credentials = new Credentials(_options.GitHubAccessToken);
			var connection = new Connection(new ProductHeaderValue("CodeplexIssueMigrator")) { Credentials = credentials };
			_gitHubClient = new GitHubClient(connection);
		}

		public async Task Run(IEnumerable<CodePlexIssue> issues)
		{
			foreach (var issue in issues)
			{
				if (issue.IsClosed())
				{
					continue;
				}

				var codePlexIssueUrl = string.Format("http://{0}.codeplex.com/workitem/{1}", _options.CodeplexProject, issue.Id);
				var description = new StringBuilder();
				description.AppendFormat("**This issue was imported from [CodePlex]({0})**", codePlexIssueUrl);
				description.AppendLine();
				description.AppendLine();
				description.AppendFormat(CultureInfo.InvariantCulture, "**[{0}](http://www.codeplex.com/site/users/view/{0})** wrote {1:yyyy-MM-dd} at {1:HH:mm}\r\n", issue.ReportedBy, issue.Time);
				description.Append(issue.Description);
				foreach (var comment in issue.Comments)
				{
					description.AppendLine();
					description.AppendLine();
					description.AppendFormat(CultureInfo.InvariantCulture, "**[{0}](http://www.codeplex.com/site/users/view/{0})** wrote {1:yyyy-MM-dd} at {1:HH:mm}\r\n", comment.Author, comment.Time);
					description.Append(comment.Content);
					// await CreateComment(gitHubIssue.Number, comment.Content);
				}

				var labels = new List<string>();

				if (issue.Type == "Feature")
				{
					labels.Add("enhancement");
				}

				// if (issue.Type == "Issue")
				//    labels.Add("bug");
				// if (issue.Impact == "Low" || issue.Impact == "Medium" || issue.Impact == "High")
				//    labels.Add(issue.Impact);
				var gitHubIssue = await CreateIssue(issue.Title, description.ToString().Trim(), labels);

				if (issue.IsClosed())
				{
					await CloseIssue(gitHubIssue);
				}
			}
		}

		private async Task<Issue> CreateIssue(string title, string body, List<string> labels)
		{
			var issue = new NewIssue(title) { Body = body };
			issue.Labels.Add("CodePlex");
			foreach (var label in labels)
			{
				if (!string.IsNullOrEmpty(label))
				{
					issue.Labels.Add(label);
				}
			}

			return await _gitHubClient.Issue.Create(_options.GitHubOwner, _options.GitHubRepository, issue);
		}

		private async Task CreateComment(int number, string comment)
		{
			await _gitHubClient.Issue.Comment.Create(_options.GitHubOwner, _options.GitHubRepository, number, comment);
		}

		private async Task CloseIssue(Issue issue)
		{
			var issueUpdate = new IssueUpdate { State = ItemState.Closed };
			foreach (var label in issue.Labels)
			{
				issueUpdate.Labels.Add(label.Name);
			}

			await _gitHubClient.Issue.Update(_options.GitHubOwner, _options.GitHubRepository, issue.Number, issueUpdate);
		}
	}
}
