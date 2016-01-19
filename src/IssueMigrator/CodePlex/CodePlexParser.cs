
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

using HtmlAgilityPack;

namespace CodePlexIssueMigrator.CodePlex
{
	public class CodePlexParser
	{
		/// <summary>Http client to connect to CodePlex.</summary>
		private HttpClient _httpClient;

		/// <summary>CodePlex project name.</summary>
		private string _codePlexProject;

		public CodePlexParser(string codePlexProject)
		{
			_codePlexProject = codePlexProject;
			_httpClient = new HttpClient();
		}

		public List<CodePlexIssue> GetIssues(int size = 100)
		{
			List<CodePlexIssue> issues = new List<CodePlexIssue>();

			// Find the number of discussions
			var numberOfDiscussions = GetNumberOfItems();

			// Calculate number of pages
			var pages = (int)Math.Ceiling((double)numberOfDiscussions / size);

			for (int page = 0; page < pages; page++)
			{
				var url = string.Format("http://{0}.codeplex.com/workitem/list/advanced?keyword=&status=All&type=All&priority=All&release=All&assignedTo=All&component=All&sortField=Id&sortDirection=Ascending&size={1}&page={2}", _codePlexProject, size, page);
				var html = _httpClient.GetStringAsync(url).Result;
				foreach (var issue in GetMatches(html, "<tr id=\"row_checkbox_\\d+\" class=\"CheckboxRow\">(.*?)</tr>"))
				{
					var id = int.Parse(GetMatch(issue, "<td class=\"ID\">(\\d+?)</td>"));

					var codeplexIssue = GetIssue(id).Result;
					issues.Add(codeplexIssue);
				}
			}

			return issues;
		}

		private int GetNumberOfItems()
		{
			var url = string.Format("https://{0}.codeplex.com/workitem/list/advanced", _codePlexProject);
			var html = _httpClient.GetStringAsync(url).Result;
			return int.Parse(GetMatch(html, "Selected\">(\\d+)</span> items"));
		}

		public async Task<CodePlexIssue> GetIssue(int id)
		{
			var url = string.Format("http://{0}.codeplex.com/workitem/{1}", _codePlexProject, id);
			var html = await _httpClient.GetStringAsync(url);

			HtmlDocument doc = new HtmlDocument();
			doc.LoadHtml(html);
			var root = doc.DocumentNode;

			var description = root.SelectSingleNode("//div[@id='descriptionContent']").InnerHtml;
			var reportedBy = root.SelectSingleNode("//a[@id='ReportedByLink']").InnerText;

			var title = root.SelectSingleNode("//h1[@id='workItemTitle']").InnerText;
			var status = root.SelectSingleNode("//a[@id='StatusLink']").InnerText;
			var type = root.SelectSingleNode("//a[@id='TypeLink']").InnerText;
			var impact = root.SelectSingleNode("//a[@id='ImpactLink']").InnerText;

			var reportedTimeString = root.SelectSingleNode("//span[@id='ReportedOnDateTime']").GetAttributeValue("title", "01/01/1990 00:00:00");
			DateTime reportedTime;
			DateTime.TryParse(
				reportedTimeString,
				CultureInfo.InvariantCulture,
				DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal,
				out reportedTime);

			var issue = new CodePlexIssue { Description = HtmlToMarkdown(description), ReportedBy = reportedBy, Time = reportedTime };

			issue.Id = id;
			issue.Title = HtmlToMarkdown(title);
			issue.Status = status;
			issue.Type = type;
			issue.Impact = impact;

			for (int i = 0; ; i++)
			{
				var commentNode = root.SelectSingleNode("//div[@id='CommentContainer" + i + "']");

				if (commentNode == null)
				{
					break;
				}

				HtmlDocument commentDoc = new HtmlDocument();
				commentDoc.LoadHtml(commentNode.InnerHtml);
				var commentRoot = commentDoc.DocumentNode;

				var author = commentRoot.SelectSingleNode("//a[@id='PostedByLink" + i + "']").InnerText;

				var timeString = commentRoot.SelectSingleNode("//span[@id='PostedOnDateTime" + i + "']"). GetAttributeValue("title", "01/01/1990 00:00:00");
				DateTime time;
				DateTime.TryParse(
					timeString,
					CultureInfo.InvariantCulture,
					DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal,
					out time);

				var content = commentRoot.SelectSingleNode("//div[@class='markDownOutput' or @class='markDownOutput ']").InnerHtml;
				issue.Comments.Add(new CodeplexComment { Content = HtmlToMarkdown(content), Author = author, Time = time });
			}

			if (issue.IsClosed())
			{
				var closedNode = root.SelectSingleNode("//div[@id='ClosedDiv']");

				HtmlDocument closedDoc = new HtmlDocument();
				closedDoc.LoadHtml(closedNode.InnerHtml);
				var closedRoot = closedDoc.DocumentNode;

				var timeString = closedRoot.SelectSingleNode("//span[@id='ClosedOnDateTimeComment']").GetAttributeValue("title", "01/01/1990 00:00:00");
				DateTime time;
				DateTime.TryParse(
					timeString,
					CultureInfo.InvariantCulture,
					DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal,
					out time);

				var author = closedRoot.SelectSingleNode("//a[@id='ClosedByLink']").InnerText;

				var content = closedRoot.SelectSingleNode("//div[@class='markDownOutput' or @class='markDownOutput ']").InnerHtml;
				if (string.IsNullOrEmpty(content))
				{
					content = "Closed.";
				}

				issue.Comments.Add(new CodeplexComment { Content = HtmlToMarkdown(content), Author = author, Time = time });
			}

			Console.WriteLine("{0} : {1} ({2} comments)", issue.Id, issue.Title, issue.Comments.Count);

			return issue;
		}

		private string HtmlToMarkdown(string html)
		{
			var config = new ReverseMarkdown.Config("pass_through", true);
			var converter = new ReverseMarkdown.Converter(config);

			return converter.Convert(html);
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
