
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
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

		private int _apiCalls = 0;

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
				Console.WriteLine("{0} - Adding {1} : {2}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), issue.Id, issue.Title);
				await CreateIssue(issue);
				VerifyRateLimit();
			}
		}

		public async Task RunFor(CodePlexIssue issue)
		{
			await CreateIssue(issue);
		}

		private async Task CreateIssue(CodePlexIssue codePlexIssue)
		{
			var codePlexIssueUrl = string.Format("http://{0}.codeplex.com/workitem/{1}", _options.CodeplexProject, codePlexIssue.Id);
			var description = new StringBuilder();
			description.AppendFormat("<sub>This issue was imported from [CodePlex]({0})</sub>", codePlexIssueUrl);
			description.AppendLine();
			description.AppendLine();
			description.AppendFormat(CultureInfo.InvariantCulture, "**[{0}](https://github.com/{0})** <sup>wrote {1:yyyy-MM-dd} at {1:HH:mm}</sup>", codePlexIssue.ReportedBy, codePlexIssue.Time);
			description.AppendLine();
			description.Append(codePlexIssue.Description);

			var labels = new List<string>();

			if (codePlexIssue.Type == "Feature")
			{
				labels.Add("enhancement");
			}

			// if (codePlexIssue.Type == "Issue")
			//    labels.Add("bug");
			// if (codePlexIssue.Impact == "Low" || codePlexIssue.Impact == "Medium" || codePlexIssue.Impact == "High")
			//    labels.Add(issue.Impact);

			var issue = new NewIssue(codePlexIssue.Title) { Body = description.ToString().Trim() };
			issue.Labels.Add("CodePlex");
			foreach (var label in labels)
			{
				if (!string.IsNullOrEmpty(label))
				{
					issue.Labels.Add(label);
				}
			}

			var gitHubIssue = await _gitHubClient.Issue.Create(_options.GitHubOwner, _options.GitHubRepository, issue);

			var commentsCount = codePlexIssue.Comments.Count;
			var n = 0;
			foreach (var comment in codePlexIssue.Comments)
			{
				Console.WriteLine("{0} - > Adding Comment {1}/{2} by {3}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), ++n, commentsCount, comment.Author);
				await CreateComment(gitHubIssue.Number, comment);
			}

			if (codePlexIssue.IsClosed())
			{
				await CloseIssue(gitHubIssue);
			}
		}

		private async Task CreateComment(int number, CodeplexComment comment)
		{
			var message = new StringBuilder();
			message.AppendFormat(CultureInfo.InvariantCulture, "**[{0}](https://github.com/{0})** <sup>wrote {1:yyyy-MM-dd} at {1:HH:mm}</sup>", comment.Author, comment.Time);
			message.AppendLine();
			message.Append(comment.Content);
			await _gitHubClient.Issue.Comment.Create(_options.GitHubOwner, _options.GitHubRepository, number, message.ToString());
			VerifyRateLimit();
		}

		private async Task CloseIssue(Issue issue)
		{
			var issueUpdate = new IssueUpdate { State = ItemState.Closed };
			Console.WriteLine("{0} - > Close issue", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
			await _gitHubClient.Issue.Update(_options.GitHubOwner, _options.GitHubRepository, issue.Number, issueUpdate);
		}

		private void VerifyRateLimit()
		{
			if (_options.RateLimit > 0)
			{
				_apiCalls++;
				if (_apiCalls >= _options.RateLimit)
				{
					Console.WriteLine("{0} -- GitHub API Limit reached: {1} calls. Waiting {2} seconds", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), _apiCalls, _options.RatePause);
					Thread.Sleep(_options.RatePause * 1000);
					_apiCalls = 0;
				}
			}
		}
	}
}
