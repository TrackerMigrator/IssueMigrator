
using System;
using System.Threading.Tasks;

using CommandLine;

using IssueMigrator.CodePlex;
using IssueMigrator.GitHub;

namespace IssueMigrator
{
	public class Program
	{
		/// <summary>Command line arguments.</summary>
		private readonly CommandLineOptions _options;

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
		}

		/// <summary>
		/// The main logic for migrate CodePlex project issues to GitHub.</summary>
		private void Run()
		{
			Console.WriteLine("{0} -- Source: {1}.codeplex.com", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), _options.CodeplexProject);
			Console.WriteLine("{0} -- Destination: github.com/{1}/{2}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), _options.GitHubOwner, _options.GitHubRepository);

			MigrateIssues().Wait();

			Console.WriteLine();
			Console.WriteLine("{0} -- Completed successfully", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
		}

		private async Task MigrateIssues()
		{
			var codePlexParser = new CodePlexParser(_options.CodeplexProject);
			var gitHubWorker = new GitHubWorker(_options);

			if (_options.IssueNumber.HasValue)
			{
				Console.WriteLine("{0} -- Exporting CodePlex issue:", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
				var issue = await codePlexParser.GetIssue(_options.IssueNumber.Value);
				Console.WriteLine("{0} -- Importing to GitHub:", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
				await gitHubWorker.RunFor(issue);
			}
			else
			{
				Console.WriteLine("{0} -- Exporting CodePlex issues:", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
				var issues = codePlexParser.GetIssues();
				Console.WriteLine("{0} -- Importing to GitHub:", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
				await gitHubWorker.Run(issues);
			}
		}
	}
}
