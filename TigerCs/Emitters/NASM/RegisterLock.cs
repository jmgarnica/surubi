using System;
using System.Collections.Generic;
using System.Linq;

namespace TigerCs.Emitters.NASM
{
	public class RegisterLock
	{
		int rlock = 0;

		public bool Locked(Register r)
		{
			lock (this)
			{
				return (rlock & (int)r) != 0;
			}
		}

		public bool Lock(Register r)
		{
			lock (this)
			{
				if ((rlock & (int)r) != 0) return false;
				rlock |= (int)r;
				return true;
			}
		}

		public void Release(Register r)
		{
			lock (this)
			{
				rlock &= ~(int)r;
			}
		}

		public void ReleaseAll()
		{
			lock (this)
			{
				rlock = 0;
			}
		}

		public Register? LockGPR(Register? hinted = null)
		{
			lock (this)
			{
				if (hinted != null && (rlock & (int)hinted.Value) == 0)
				{
					rlock |= (int)hinted.Value;
					return hinted.Value;
				}
				for (int i = 1; i <= 8; i *= 2)
				{
					if ((rlock & i) != 0) continue;
					rlock |= i;
					return (Register)i;
				}
				return null;
			}
		}

		public List<Register> Locked()
		{
            lock (this)
			{
				return new List<Register>(Enum.GetValues(typeof(Register))
					.Cast<int>()
					.Where(i => (rlock & i) != 0)
					.Cast<Register>());
			}
		}

		public RegisterLock CloneState()
		{
			lock (this)
			{
				return new RegisterLock { rlock = rlock };
			}
		}
	}

	public enum Register
	{
		/// <summary>
		/// Acumulation, Return
		/// </summary>
		EAX = 1,
		/// <summary>
		/// Address Base
		/// </summary>
		EBX = 2,
		/// <summary>
		/// Counter
		/// </summary>
		ECX = 4,
		/// <summary>
		/// Auxiliar, Data Movement
		/// </summary>
		EDX = 8,

		/// <summary>
		/// Stack Top Pointer
		/// </summary>
		ESP = 16,
		/// <summary>
		/// Stack Base Pointer
		/// </summary>
		EBP = 32,

		/// <summary>
		/// Source Pointer, volatile
		/// </summary>
		ESI = 64,
		/// <summary>
		/// Destination Pointer, volatile
		/// </summary>
		EDI = 128,

		AL = 513,
		BL = 514,
		CL = 516,
		DL = 520
	}

	public enum WordSize
	{
		Byte = 1,
		Word = 2,
		DWord = 4,
		QWord = 8
	}

	public static class RegisterExtensions
	{
		public static bool GeneralPurposeRegister(this Register r)
		{
			switch (r)
			{
				case Register.EAX:
				case Register.EBX:
				case Register.ECX:
				case Register.EDX:
					return true;
				default:
					return false;
			}
		}

		public static Register ByteVersion(this Register r)
		{
			if (!r.GeneralPurposeRegister()) throw new InvalidOperationException("No General Purpose Register");
			return (Register)((int)r | 512);
		}
	}

}
