using System;
using System.Collections.Generic;
using TigerCs.CompilationServices;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation
{
	public interface ISemanticChecker
	{
		/// <summary>
		/// Initializate a new semantic checker context
		/// </summary>
		/// <param name="trappedSTD">
		/// when not null all missing members from a call to Reachable whith 
		/// enough information on the desired field, desired field not null, of type(or derived from) 
		/// TypeInfo, FunctionInfo and HolderInfo,
		/// and with only null values in those members of a type difined in ByteCode namespace,
		/// will be added to the given dictionary, if the same name is missing more than once will not cause an error report
		/// unless the definitions differs, equality is componentwise, TypeInfo.TypeId is ignored, Alises are handled as its internal.
		/// </param>
		void InitializeSemanticCheck(ErrorReport report, Dictionary<string, MemberInfo> trappedSTD = null);
		void End();

		[ScopeChanger(Reason = "Creates and enters in a nested scope", ScopeName = "InnerScope")]
		void EnterNestedScope(Dictionary<string, MemberInfo> autoclosure = null, params object[] descriptors);

		[ScopeChanger(Reason = "Closes the current scope and returns to it's parent")]
		void LeaveScope();

		/// <summary>
		/// Declare a new member, declaration of a already declared member or,
		/// when trapping STD members, a missing member, will generate an error report
		/// </summary>
		/// <param name="name"></param>
		/// <param name="member"></param>
		/// <returns></returns>
		bool DeclareMember(string name, MemberInfo member);

		/// <summary>
		/// [IMPLEMENTATION_TIP] set to true MemberInfo.Use before outing it
		/// </summary>
		/// <param name="name"></param>
		/// <param name="member"></param>
		/// <param name="desired"></param>
		/// <returns></returns>
		bool Reachable(string name, out MemberInfo member, MemberDefinition desired = null);
		
		T SeekDescriptor<T>(Predicate<object> stop = null)
			where T : class;

	}
}
