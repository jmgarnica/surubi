using System;
using System.Linq;
using TigerCs.Generation;
using TigerCs.Generation.ByteCode;

namespace TigerCs.Interpretation
{
	public class IntpObject
	{
		public readonly object content;
		public readonly ObjType content_type;
		public static readonly IntpObject Null = new IntpObject(new object(), ObjType.Null);

		protected IntpObject(object content, ObjType type)
		{
			content_type = type;
			this.content = content;
		}

		public static implicit operator IntpObject(int @const)
		{
			return new IntpObject(@const, ObjType.Integer);
		}

		public static implicit operator IntpObject(string @const)
		{
			return new IntpObject(@const, ObjType.String);
		}

		public static explicit operator int (IntpObject @const)
		{
			return (int)@const.content;
		}

		public static explicit operator string (IntpObject @const)
		{
			return (string)@const.content;
		}

		public static bool CreateRecord(TypeInfo recordtype, IntpObject[] members, ISemanticChecker sc, out IntpObject record)
		{
			IntpPack r;
			var b = IntpPack.CreateRecord(recordtype, members, sc, out r);
			record = r;
			return b;
		}

		public enum ObjType
		{
			Integer,
			String,
			Null,
			Record_Array
		}

		public virtual bool GenerateBCMMember<T, F, H>(IByteCodeMachine<T, F, H> cg, out H BCMmember)
			where T : class, IType<T, F>
			where F : class, IFunction<T, F>
			where H : class, IHolder
		{
			switch (content_type)
			{
				case ObjType.Integer:
					BCMmember = cg.AddConstant((int)content);
					return true;
				case ObjType.String:
					BCMmember = cg.AddConstant((string)content);
					return true;
				case ObjType.Null:
					return cg.TryBindSTDConst("nil", out BCMmember);

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public virtual bool TryGetMember(int index, out IntpObject member)
		{
			member = null;
			return false;
		}

		class IntpPack : IntpObject
		{
			readonly TypeInfo type;
			readonly bool array;

			IntpPack(TypeInfo type, int length, bool array, IntpObject[] members = null)
				: base(members ?? new IntpObject[length], ObjType.Record_Array)
			{
				this.type = type;
				this.array = array;
			}

			public static bool CreateRecord(TypeInfo recordtype, IntpObject[] members, ISemanticChecker sc, out IntpPack pack)
			{
				pack = null;
				if (recordtype.Members == null || recordtype.Members.Count != members.Length) return false;

				int length = recordtype.Members.Count;
				var _int = sc.Int();
				var _string = sc.String();

				for (int i = 0; i < length; i++)
				{
					var t = recordtype.Members[i].Item2;
					if (t == _int && members[i].content_type != ObjType.Integer) return false;
					if (t == _string && members[i].content_type != ObjType.String && !members[i].Equals(Null)) return false;

					if (t.Members == null && t.ArrayOf == null) continue;

					if (members[i].Equals(Null)) continue;

					var mpack = members[i] as IntpPack;
					if (mpack == null) return false;

					if (t.Members != null && (mpack.array || mpack.type != t)) return false;
					if (t.ArrayOf != null && (!mpack.array || mpack.type != t.ArrayOf)) return false;
				}

				pack = new IntpPack(recordtype, recordtype.Members.Count, false, members);
				return true;
			}

			public override bool GenerateBCMMember<T, F, H>(IByteCodeMachine<T, F, H> cg, out H BCMmember)
			{
				H h = BCMmember = default(H);
				var members = (from m in content as IntpObject[]
							   let b = m.GenerateBCMMember(cg, out h)
							   where b
							   select h).ToArray();

				if (members.Length != type.Members.Count) return false;
				var bcmtype = (T)type.BCMMember;

				BCMmember = cg.BindVar(bcmtype);
				cg.Call(bcmtype.Allocator, members, BCMmember);
				return true;
			}

			public override bool TryGetMember(int index, out IntpObject member)
			{
				member = null;
				if (array || index < 0 || index >= type.Members.Count) return false;

				member = ((IntpObject[])content)[index];
				return true;
			}
		}
	}
}
