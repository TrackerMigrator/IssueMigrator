
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

using CommandLine;

using Octokit;

namespace CodePlexIssueMigrator
{
	public class Program
	{
		/// <summary>Command line arguments.</summary>
		private readonly CommandLineOptions _options;

		/// <summary>GitHub API Client.</summary>
		private GitHubClient _gitHubClient;

		/// <summary>Http client to connect to CodePlex.</summary>
		private HttpClient _httpClient;

		static int Main(string[] args)
		{
			var returnCode = 0;
			var isValidOptions = true;

			var options = new CommandLineOptions();
			if (!Parser.Default.ParseArguments(args, options))
			{
				returnCode = 1;
				isValidOptions = false;
			}

			if (isValidOptions)
			{
				try
				{
					var program = new Program(options);
					program.Run();
				}
				catch (Exception ex)
				{
					returnCode = -1;
					Console.WriteLine("Exception occurred while running. Exception is: {0}", ex.Message);
				}
			}

			return returnCode;
		}

		/// <summary>
		/// Constructs a new program with the given options.
		/// </summary>
		protected Program(CommandLineOptions options)
		{
			_options = options;

			_httpClient = new HttpClient();

			var credentials = new Credentials(options.GitHubAccessToken);
			var connection = new Connection(new ProductHeaderValue("CodeplexIssueMigrator")) { Credentials = credentials };
			_gitHubClient = new GitHubClient(connection);
		}

		/// <summary>
		/// The main logic for migrate CodePlex project issues to GitHub.</summary>
		private void Run()
		{
			Console.WriteLine("Source: {0}.codeplex.com", _options.CodeplexProject);
			Console.WriteLine("Destination: github.com/{0}/{1}", _options.GitHubOwner, _options.GitHubRepository);

			Console.WriteLine("Migrating issues:");

			MigrateIssues().Wait();

			Console.WriteLine();
			Console.WriteLine("Completed successfully.");
		}

		private async Task MigrateIssues()
		{
			var issues = GetIssues();
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

		private IEnumerable<CodePlexIssue> GetIssues(int size = 100)
		{
			// Find the number of discussions
			var numberOfDiscussions = GetNumberOfItems();

			// Calculate number of pages
			var pages = (int)Math.Ceiling((double)numberOfDiscussions / size);

			for (int page = 0; page < pages; page++)
			{
				var url = string.Format("http://{0}.codeplex.com/workitem/list/advanced?keyword=&status=All&type=All&priority=All&release=All&assignedTo=All&component=All&sortField=Id&sortDirection=Ascending&size={1}&page={2}", _options.CodeplexProject, size, page);
				var html = _httpClient.GetStringAsync(url).Result;
				foreach (var issue in GetMatches(html, "<tr id=\"row_checkbox_\\d+\" class=\"CheckboxRow\">(.*?)</tr>"))
				{
					var id = int.Parse(GetMatch(issue, "<td class=\"ID\">(\\d+?)</td>"));
					var status = GetMatch(issue, "<td class=\"Status\">(.+?)</td>");
					var type = GetMatch(issue, "<td class=\"Type\">(.+?)</td>");
					var impact = GetMatch(issue, "<td class=\"Severity\">(.+?)</td>");
					var title = GetMatch(issue, "<a id=\"TitleLink.*>(.*?)</a>");
					Console.WriteLine("{0} ({1}) : {2}", id, status, title);
					var codeplexIssue = GetIssue(id).Result;
					codeplexIssue.Id = id;
					codeplexIssue.Title = HtmlToMarkdown(title);
					codeplexIssue.Status = status;
					codeplexIssue.Type = type;
					codeplexIssue.Impact = impact;
					yield return codeplexIssue;
				}
			}
		}

		private int GetNumberOfItems()
		{
			var url = string.Format("https://{0}.codeplex.com/workitem/list/advanced", _options.CodeplexProject);
			var html = _httpClient.GetStringAsync(url).Result;
			return int.Parse(GetMatch(html, "Selected\">(\\d+)</span> items"));
		}

		private async Task<CodePlexIssue> GetIssue(int number)
		{
			var url = string.Format("http://{0}.codeplex.com/workitem/{1}", _options.CodeplexProject, number);
			var html = await _httpClient.GetStringAsync(url);

			var description = GetMatch(html, "descriptionContent\">(.*?)</div>");
			var reportedBy = GetMatch(html, "ReportedByLink.*?>(.*?)</a>");

			var reportedTimeString = GetMatch(html, "ReportedOnDateTime.*?title=\"(.*?)\"");
			DateTime reportedTime;
			DateTime.TryParse(
				reportedTimeString,
				CultureInfo.InvariantCulture,
				DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal,
				out reportedTime);

			var issue = new CodePlexIssue { Description = HtmlToMarkdown(description), ReportedBy = reportedBy, Time = reportedTime };

			for (int i = 0; ; i++)
			{
				var commentMatch = Regex.Match(html, @"CommentContainer" + i + "\">.*", RegexOptions.Multiline | RegexOptions.Singleline);
				if (!commentMatch.Success)
				{
					break;
				}

				var commentHtml = commentMatch.Value;
				var author = GetMatch(commentHtml, "class=\"author\".*?>(.*?)</a>");

				var timeString = GetMatch(commentHtml, "class=\"smartDate\" title=\"(.*?)\"");
				DateTime time;
				DateTime.TryParse(
					timeString,
					CultureInfo.InvariantCulture,
					DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal,
					out time);

				var content = GetMatch(commentHtml, "markDownOutput \">(.*?)</div>");
				issue.Comments.Add(new CodeplexComment { Content = HtmlToMarkdown(content), Author = author, Time = time });
			}

			return issue;
		}

		private string HtmlToMarkdown(string html)
		{
			var text = HttpUtility.HtmlDecode(html);
			text = text.Replace("<br>", "\r\n");
			return text.Trim();
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

		/// <summary>
		/// Gets the value of the first group by matching the specified string with the specified regular expression.
		/// </summary>
		/// <param name="input">The input string.</param>
		/// <param name="expression">Regular expression with one group.</param>
		/// <returns>The value of the first group.</returns>
		private string GetMatch(string input, string expression)
		{
			var titleMatch = Regex.Match(input, expression, RegexOptions.Multiline | RegexOptions.Singleline);
			return titleMatch.Groups[1].Value;
		}

		/// <summary>
		/// Gets the value of the first group of the matches of the specified regular expression.
		/// </summary>
		/// <param name="input">The input string.</param>
		/// <param name="expression">Regular expression with a group that should be captured.</param>
		/// <returns>A sequence of values from the first group of the matches.</returns>
		private IEnumerable<string> GetMatches(string input, string expression)
		{
			foreach (Match match in Regex.Matches(input, expression, RegexOptions.Multiline | RegexOptions.Singleline))
			{
				yield return match.Groups[1].Value;
			}
		}
	}
}
