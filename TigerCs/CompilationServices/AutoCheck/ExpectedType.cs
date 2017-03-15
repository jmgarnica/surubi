namespace TigerCs.CompilationServices.AutoCheck
{
	public enum ExpectedType
	{
		Unknown,
		Int,
		String,
		Void,
		Null,
		Dependent,
		Expected,

		//array and member types are not allowed as expected return type for semantic checked attribute

		ArrayOfInt,
		ArrayOfString,
		ArrayOfDependent,
		ArrayOfExpected,

		MemberOfDependent,
		MemberOfExpected
	}
}
