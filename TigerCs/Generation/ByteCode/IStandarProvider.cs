using System.Collections.Generic;
using TigerCs.Generation.Semantic.Scopes;

namespace TigerCs.Generation.ByteCode
{
	public interface ISemanticStandar
	{
		void InitializeSemanticCheck();

		#region [STD Types]
		IType VoidType { get; }
		IType Int { get; }
		IType String { get; }
		IHolder Nill { get; }
		#endregion

		#region [STD Functions]
		IDictionary<string, FunctionInfo> STD { get; }
		void IncludeStandarFunction(string standar);
		#endregion

		#region [Scope Handling]
		TigerScope RootScope { get; }
		TigerScope CurrentScope { get; }

		/// <summary>
		/// [IMPLEMENTATION_TIP] Add <paramref name="newscopelabel"/> to ScopeCommonLavel to make the common label of the new scope
		/// </summary>
		/// <param name="newscopelabel"></param>
		/// <param name="definetype">true if the new scope will define a new type</param>
		[ScopeChanger(Reason = "Creates and enters in a nested scope", ScopeName = "InnerScope")]
		void EnterNestedScope(string newscopelabel, bool definetype, string scopelabel = null);

		/// <summary>
		/// [IMPLEMENTATION_TIP] This should free any memory reserved in the scope that is about to leave.
		/// [IMPLEMENTATION_TIP] Remove the last part of ScopeCommonLavel, separators are TigerScope.ScopeNameSeparator
		/// [IMPLEMENTATION_TIP] Unused labels at the end of the scope will generate warnings
		/// [IMPLEMENTATION_TIP] Unseted label at the end of the scope generate critical errors
		/// </summary>
		[ScopeChanger(Reason = "Closes the current scope and returns to it's parent")]
		void LeaveScope();

		#endregion
	}
}
