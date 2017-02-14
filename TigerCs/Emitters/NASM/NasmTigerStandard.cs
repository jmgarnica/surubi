namespace TigerCs.Emitters.NASM
{
	public static class NasmTigerStandard
	{

		public static NasmFunction AddPrintS(NasmEmitter bound)
			=> new NasmCFunction(NasmEmitter.PrintSFunctionLabel, false, bound, true, "PrintS");

		public static void WritePrintS(FormatWriter fw, NasmEmitter bound)
		{
			fw.WriteLine(NasmEmitter.PrintSFunctionLabel + ":");
			fw.IncrementIndentation();
			fw.WriteLine("mov EAX, [ESP + 4]");
			fw.WriteLine("cmp EAX, 0");
			fw.WriteLine("je .null_error_exit");
			fw.WriteLine("add EAX, 4");//size space
			fw.WriteLine("push EAX");
			fw.WriteLine($"push dword {NasmEmitter.PrintSFormatName}");
			fw.WriteLine("call _printf");
			fw.WriteLine("add ESP, 8");
			fw.WriteLine("xor EAX, EAX");
			fw.WriteLine("xor ECX, ECX");
			fw.WriteLine("ret");
			fw.WriteLine("");
			fw.WriteLine(".null_error_exit:");

			var m = bound.AddConstant("Null Reference");
			m.PutValueInRegister(Register.EAX, fw, null);
			fw.WriteLine("push EAX");
			fw.WriteLine($"call {NasmEmitter.PrintSFunctionLabel}");
			fw.WriteLine("add ESP, 4");
			fw.WriteLine("mov EAX, 3");
			fw.WriteLine($"mov ECX, {NasmEmitter.ErrorCode}");
			fw.WriteLine("ret");
			fw.DecrementIndentation();
		}

		public static NasmFunction AddPrintI(NasmEmitter bound)
			=> new NasmCFunction(NasmEmitter.PrintIFunctionLabel, false, bound, name: "PrintI");
	}
}
