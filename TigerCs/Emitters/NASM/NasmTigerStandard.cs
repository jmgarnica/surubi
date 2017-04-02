using System;

namespace TigerCs.Emitters.NASM
{
	public static class NasmTigerStandard
	{
		public static string PrintSFunctionLabel = "_cprintS";
		public static string PrintIFunctionLabel = "_cprintI";
		public static string GetCharFunctionLabel = "_cgetchar";
		public static string OrdFunctionLabel = "_cord";
		public static string ChrFunctionLabel = "_cchr";
		public static string SubstringFunctionLabel = "_csubstring";
		public static string ConcatFunctionLabel = "_cconcat";
		public static string EmitErrorFunctionLabel = "_emit_error";
		public static string StrCmp = "_strcmp";


		public static NasmFunction AddPrintS(NasmEmitter bound)
			=> new NasmCFunction(PrintSFunctionLabel, true, bound, true, "PrintS");

		public static NasmFunction AddPrintI(NasmEmitter bound)
			=> new NasmCFunction(PrintIFunctionLabel, true, bound, name: "PrintI");

		public static NasmFunction AddGetChar(NasmEmitter bound)
			=> new NasmCFunction(GetCharFunctionLabel, true, bound, name: "GetChar");

		public static NasmFunction AddOrd(NasmEmitter bound)
			=> new NasmCFunction(OrdFunctionLabel, true, bound, true, "Ordinal");

		public static NasmFunction AddChr(NasmEmitter bound)
			=> new NasmCFunction(ChrFunctionLabel, true, bound, true, "Char");

		public static NasmFunction AddSubstring(NasmEmitter bound)
			=> new NasmCFunction(SubstringFunctionLabel, true, bound, true, "Substring");

		public static NasmFunction AddConcat(NasmEmitter bound)
			=> new NasmCFunction(ConcatFunctionLabel, true, bound, true, "Concat");

		public static NasmFunction AddEmitError(NasmEmitter bound)
			=> new NasmCFunction(EmitErrorFunctionLabel, true, bound, true, "EmitError");

		public static NasmFunction AddStringCompare(NasmEmitter bound)
			=> new NasmCFunction(StrCmp, true, bound, true, "strcmp");
	}
}
