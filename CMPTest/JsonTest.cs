namespace CMPTest
{
	public class JsonBatchTest
	{
		public string lineEnd { get; set; } = "\n";
		public JsonPositiveTest[] correct { get; set; }
		public JsonNegativeTest[] fail { get; set; }
	}

	public class JsonPositiveTest
	{
		public string name { get; set; }
		public string code { get; set; }
		public string[] args { get; set; } = new string[0];
		public string input { get; set; } = "";
		public string correctOutput { get; set; } = null;
	}

	public enum Phase
	{
		Parse,
		SemanticCheck,
		CodeGeneration,
		Execution
	}

	public class JsonNegativeTest
	{
		public string name { get; set; }
		public string code { get; set; }
		public Phase failOn { get; set; }
		public JsonError[] errors { get; set; }
	}

	public class JsonError
	{
		public string error { get; set; }
		public int line { get; set; } = -1;
		public int column { get; set; } = -1;
		public int errno { get; set; } = -1;
	}
}
