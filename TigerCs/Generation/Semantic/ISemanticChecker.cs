using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TigerCs.Generation.ByteCode;
using TigerCs.Generation.Semantic;

namespace TigerCs.Generation.Semantic
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
		void EnterNestedScope(bool autoclosure = false, params IScopeDescriptor[] descriptors);

		[ScopeChanger(Reason = "Closes the current scope and returns to it's parent")]
		void LeaveScope();

		[ScopeChanger(Reason = "Closes the current scope and returns to it's parent, returns all references to upper scopes")]
		Dictionary<string, MemberInfo> LeaveScopeWithAutoClosure();

		/// <summary>
		/// Declare a new member, declaration of a already declared member or,
		/// when trapping STD members, a missing member, will generate an error report
		/// </summary>
		/// <param name="name"></param>
		/// <param name="member"></param>
		/// <returns></returns>
		void DeclareMember(string name, MemberInfo member);

		bool Reachable(string name, out MemberInfo member, MemberInfo desired = null);

		T SeekDescriptor<T>(params Type[] stopontype)
			where T : IScopeDescriptor;

	}
}
