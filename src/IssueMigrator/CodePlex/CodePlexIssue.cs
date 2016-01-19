
using System;
using System.Collections.Generic;

namespace IssueMigrator.CodePlex
{
	public class CodePlexIssue
	{
		public CodePlexIssue()
		{
			this.Comments = new List<CodeplexComment>();
		}

		public int Id { get; set; }
		public string Title { get; set; }
		public string Description { get; set; }
		public string Status { get; set; }
		public string Type { get; set; }
		public string Impact { get; set; }
		public DateTime Time { get; set; }
		public string ReportedBy { get; set; }
		public string ClosedReason { get; set; }
		public string Release { get; set; }
		public string Component { get; set; }
		public List<CodeplexComment> Comments { get; private set; }

		public bool IsClosed()
		{
			return this.Status == "Closed";
		}
	}
}
