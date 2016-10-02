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
					if ((rlock & i) == 0)
					{
						rlock |= i;
						return (Register)i;
					}
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
	}

	public enum Register : int
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
		/// Source Pointer
		/// </summary>
		ESI = 64,
		/// <summary>
		/// Destination Pointer
		/// </summary>
		EDI = 128,
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
	}

}
