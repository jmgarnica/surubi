using System.Collections.Generic;
using TigerCs.Generation.Semantic.Scopes;

namespace TigerCs.Generation.ByteCode
{
	public interface ITigerEmitter
	{
		TypeInfo VoidType { get; }
		TypeInfo Int { get; }
		TypeInfo String { get; }
		IDictionary<string, FunctionInfo> STD { get; }

		#region [Bound]

		IHolder AddConstant(int value);
		IHolder AddConstant(string value);
		IHolder BoundVar(IType tigertype, string name = null);


		IFunction AheadFunctionDeclaration(string name, IType returntype, params IType[] args);

		[ScopeChanger(Reason = "Creates and enters in a function scope", ScopeName = "BoundedFuncion")]
		IFunction BoundFunction(string name, IType returntype, params IType[] args);

		[ScopeChanger(Reason = "Creates and enters in a function scope", ScopeName = "BoundedPreviousDeclaredFuncion")]
		void BoundFunction(IFunction aheadedfunction);


		IType AheadTypeDeclaration(string name);

		#endregion



		#region [Call]
		/// <summary>
		/// Push a parameter in a stack for latter use whith call. 
		/// [IMPLEMENTATION_TIP] Ending a program with a non-empty stack will result in error.
		/// </summary>
		/// <param name="holder"></param>
		void Param(IHolder holder);

		/// <summary>
		/// Enters in a function with the <paramref name="paramcount"/> parameters on the top of the stack as it's parameters.
		/// [IMPLEMENTATION_TIP] The parameters are deleted from the stack.
		/// </summary>
		/// <param name="function"></param>
		/// <param name="paramcount"></param>
		void Call(IFunction function, int paramcount);

		#endregion

		#region [Scope Handling]

		/// <summary>
		/// This is the common prefix of all lavels in the currentscope.
		/// </summary>
		string ScopeCommonLavel { get; }

		TigerScope RootScope { get; }
		TigerScope CurrentScope { get; }

		/// <summary>
		/// [IMPLEMENTATION_TIP] Add <paramref name="newscopelabel"/> to ScopeCommonLavel to make the common label of the new scope
		/// </summary>
		/// <param name="newscopelabel"></param>
		[ScopeChanger(Reason = "Creates and enters in a nested scope", ScopeName = "InnerScope")]
		void EnterNestedScope(string newscopelabel);

		/// <summary>
		/// [IMPLEMENTATION_TIP] This should free any memory reserved in the scope that is about to leave.
		/// [IMPLEMENTATION_TIP] Remove the last part of ScopeCommonLavel, separators are plataform specific.
		/// </summary>
		[ScopeChanger(Reason = "Closes the current scope and returns to it's parent")]
		void LeaveScope();
		#endregion

	}
}
