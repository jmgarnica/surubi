﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TigerCs.CompilationServices;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Generation.AST.Declarations
{
	public class TypeDeclarationList : DeclarationList<TypeDeclaration>
	{
		protected List<TypeDeclaration> orderedList;
		protected List<TypeDeclaration> nonrecursive;
		protected List<TypeDeclaration> ceroex;

		protected virtual bool TopSort(List<TypeDeclaration> toorder, ErrorReport report)
		{
			var id = toorder.ToDictionary(t => t.TypeName);
			var indeg = toorder.ToDictionary(t => t.TypeName, t => 0);
			Stack <TypeDeclaration> ceroindeg = new Stack<TypeDeclaration>();
			int n = 0;

			for (int i = 0; i < toorder.Count; i++)
			{
				var cur = this[i];

				foreach (var s in cur.Dependencies)
				{
					if (!id.ContainsKey(s))
					{
						report.Add(new StaticError(line, column, $"Type {cur.Type} deppends on an unaccessible type {s}", ErrorLevel.Error));
						return false;
					}
					indeg[s]++;
				}
			}

			foreach (var pair in indeg)
				if (pair.Value == 0) ceroindeg.Push(id[pair.Key]);

			while (ceroindeg.Count > 0)
			{
				n++;
				var cur = ceroindeg.Pop();
				orderedList.Add(cur);

				foreach (var s in cur.Dependencies)
				{
					indeg[s]--;
					if(indeg[s] == 0) ceroindeg.Push(id[s]);
				}
			}

			if (n >= Count) return true;

			report.Add(new StaticError(line, column,
				                        $"Recursive type loop detected in types {string.Join(",", this.Except(orderedList).Select(t => t.ToString()))}",
				                        ErrorLevel.Error));
			return false;
		}

		public override bool BindName(ISemanticChecker sc, ErrorReport report)
		{
			orderedList = new List<TypeDeclaration>();
			nonrecursive = new List<TypeDeclaration>();
			ceroex = new List<TypeDeclaration>();

			var toorder = new List<TypeDeclaration>();
			foreach (var dex in this)
			{
				if (!dex.BindName(sc, report)) return false;
				if (dex.Type.Complete)
					nonrecursive.Add(dex);
				else toorder.Add(dex);

				if(dex.Dependencies.Length == 0)
					ceroex.Add(dex);
			}


			return TopSort(toorder, report);
		}

		public override bool CheckSemantics(ISemanticChecker sc, ErrorReport report, TypeInfo expected = null)
		{
			foreach (var dex in nonrecursive)
				if (!dex.CheckSemantics(sc, report)) return false;


			for (int i = orderedList.Count - 1; i >= 0; i--)
			{
				var curr = orderedList[i];
				if(curr.Dependencies.Length == 0)continue;
				if (!curr.CheckSemantics(sc, report))
					return false;
			}

			foreach (var dex in ceroex)
				if (!dex.CheckSemantics(sc, report)) return false;

			return true;
		}

		public override void GenerateCode<T, F, H>(IByteCodeMachine<T, F, H> cg, ErrorReport report)
		{
			foreach (var dex in nonrecursive)
			{
				dex.DeclareType(cg, report);
				dex.GenerateCode(cg,report);
			}

			for (int i = orderedList.Count - 1; i >= 0; i--)
				orderedList[i].DeclareType(cg, report);

			for (int i = orderedList.Count - 1; i >= 0; i--)
				orderedList[i].GenerateCode(cg, report);
		}
	}
}