
using System;
using System.Threading.Tasks;

using CommandLine;

using CodePlexIssueMigrator.CodePlex;
using CodePlexIssueMigrator.GitHub;

namespace CodePlexIssueMigrator
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
			Console.WriteLine("Source: {0}.codeplex.com", _options.CodeplexProject);
			Console.WriteLine("Destination: github.com/{0}/{1}", _options.GitHubOwner, _options.GitHubRepository);

			Console.WriteLine("Migrating issues:");

			MigrateIssues().Wait();

			Console.WriteLine();
			Console.WriteLine("Completed successfully.");
		}

		private async Task MigrateIssues()
		{
			var codePlexParser = new CodePlexParser(_options.CodeplexProject);
			var issues = codePlexParser.GetIssues();
			var gitHubWorker = new GitHubWorker(_options);

			await gitHubWorker.Run(issues);
		}
	}
}
