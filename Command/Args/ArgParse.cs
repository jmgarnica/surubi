using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using props = Command.Args.Tuple<System.Reflection.PropertyInfo, Command.Args.ArgumentAttribute>;


namespace Command.Args
{
	public static class DefaultParsers
	{
		public static readonly Dictionary<Type, List<Func<string, ITuple<object, bool>>>> Parsers;

		static DefaultParsers()
		{
			Parsers = new Dictionary<Type, List<Func<string, ITuple<object, bool>>>>
			{
				[typeof(string)] = new List<Func<string, ITuple<object, bool>>> {s => new Tuple<string, bool>(s, true)},
				[typeof(int)] = new List<Func<string, ITuple<object, bool>>>
				{
					s =>
					{
						int val;
						bool succes = int.TryParse(s, out val);
						return new Tuple<object, bool>(val, succes);
					}
				},
				[typeof(long)] = new List<Func<string, ITuple<object, bool>>>
				{
					s =>
					{
						long val;
						bool succes = long.TryParse(s, out val);
						return new Tuple<object, bool>(val, succes);
					}
				},
				[typeof(float)] = new List<Func<string, ITuple<object, bool>>>
				{
					s =>
					{
						float val;
						bool succes = float.TryParse(s, out val);
						return new Tuple<object, bool>(val, succes);
					}
				},
				[typeof(double)] = new List<Func<string, ITuple<object, bool>>>
				{
					s =>
					{//a
						double val;
						bool succes = double.TryParse(s, out val);
						return new Tuple<object, bool>(val, succes);
					}
				},
				[typeof(DateTime)] = new List<Func<string, ITuple<object, bool>>>
				{
					s =>
					{
						DateTime val;
						bool succes = DateTime.TryParse(s, out val);
						return new Tuple<object, bool>(val, succes);
					}
				}
			};
		}
	}

	public class ArgParse<P> : IArgParse<P>
		where P : class, new()
	{
		readonly ArgumentCandidateAttribute handlermark;
		readonly List<props> positional;
		readonly Dictionary<string, props> optional;
		readonly List<Type> Candidates;
		readonly Dictionary<Type, IArgParse<object>> reflectedtypes;
		readonly Dictionary<Type, List<Func<string, ITuple<object, bool>>>> parsers;

		public ArgParse()
		{
			var handler = typeof(P);
			handlermark = handler.GetCustomAttribute<ArgumentCandidateAttribute>();
			Candidates = new List<Type>(handlermark.Candidates ?? Enumerable.Empty<Type>());

			var props = (from p in handler.GetProperties()
			             let arg = p.GetCustomAttribute<ArgumentAttribute>()
			             where p.CanWrite
			             select new props(p, arg)).ToArray();

			positional = new List<props>(from p in props
			                             where p.Item2 != null && p.Item2.positionalargument >= 0
			                             orderby p.Item2.positionalargument
			                             select p);
			optional = new Dictionary<string, props>();

			var op = from p in props
			         where p.Item2 != null &&
			               p.Item2.positionalargument < 0 &&
			               !string.IsNullOrEmpty(p.Item2.OptionName)
			         select p;

			foreach (var p in op)
				optional.Add(p.Item2.OptionName, p);

			reflectedtypes = new Dictionary<Type, IArgParse<object>>();
			parsers = new Dictionary<Type, List<Func<string, ITuple<object, bool>>>>();
		}

		public P Activate(string line)
		{
			return Activate(new Lexer(line));
		}

		public P Activate(Lexer lex)
		{
			P p = new P();

			var assign = new HashSet<string>();

			int posit = 0;
			props prop = null;
			bool op = false;
			foreach (var t in lex)
			{
				if (op)
				{
					if (t.Op) return null;
				}
				else
				{
					if (t.Op)
					{
						if (!optional.TryGetValue(t.Lex, out prop)) return null;
						if (!prop.Item2.flag)
						{
							op = true;
							continue;
						}
					}
					else
					{
						if (posit >= positional.Count) return null;
						prop = positional[posit];
						posit++;
					}
				}

				object value;
				if (!(tryFlag(prop.Item1.PropertyType, prop.Item2, out value) ||
					  tryParse((t as CopToken)?.CmpLex ?? t.Lex, prop.Item1.PropertyType, prop.Item2.ParseMethod, p, out value) ||
					  tryComposite(t as CopToken, prop.Item1.PropertyType, out value)))
				{
					if (!op) return null;
				}
				else
				{
					prop.Item1.SetValue(p, value);
					if (!string.IsNullOrWhiteSpace(prop.Item2.ActionMethod))
						getMethod(typeof(P), prop.Item2.ActionMethod, new[] { prop.Item1.PropertyType })?.Invoke(p, new[] { value });
					assign.Add(prop.Item1.Name);
				}

				op = false;
			}

			if (posit < positional.Count) return null;

			foreach (var pp in optional)
			{
				if (assign.Contains(pp.Value.Item1.Name)) continue;
				object value;
				if (!tryDefault(pp.Value.Item1.PropertyType, pp.Value.Item2.DefaultValue, out value)) continue;
				pp.Value.Item1.SetValue(p, value);
				if (!string.IsNullOrWhiteSpace(pp.Value.Item2.ActionMethod))
					getMethod(typeof(P), pp.Value.Item2.ActionMethod, new[] { pp.Value.Item1.PropertyType })?.Invoke(p, new[] { value });
			}

			if (!string.IsNullOrWhiteSpace(handlermark.AfterInitializationActionMethod))
				getMethod(typeof(P), handlermark.AfterInitializationActionMethod, Type.EmptyTypes)?.Invoke(p, new object[0]);

			return p;
		}

		static bool tryFlag(Type target, ArgumentAttribute arg, out object value)
		{
			value = null;
			if (!arg.flag || arg.positionalargument >= 0) return false;

			if (!target.IsAssignableFrom(typeof(bool))) return false;
			bool val;
			try
			{
				val = (bool)arg.DefaultValue;
			}
			catch (Exception)
			{
				val = false;
			}

			value = !val;
			return true;
		}

		bool tryParse(string t, Type target, string parser, object parser_from, out object value)
		{
			value = null;
			if (t == null || target == null) return false;
			ITuple<object, bool> p;
            if (!string.IsNullOrWhiteSpace(parser))
            {
	            var parse = getMethod(typeof(P), parser, new[] {typeof(string)});
				if (parse != null && typeof(ITuple<object, bool>).IsAssignableFrom(parse.ReturnType))
				{
					p = (ITuple<object, bool>)parse.Invoke(parser_from, new object[] {t});
					if (p.Item2 && target.IsInstanceOfType(p.Item1))
						value = p.Item1;
					return p.Item2;
				}
			}

			List<Func<string, ITuple<object, bool>>> prs;
			if (parsers.TryGetValue(target, out prs))
				foreach (var func in prs)
				{
					p = func(t);
					if (!p.Item2) continue;
					value = p.Item1;
					return true;
				}

			if (DefaultParsers.Parsers.TryGetValue(target, out prs))
				foreach (var func in prs)
				{
					p = func(t);
					if (!p.Item2) continue;
					value = p.Item1;
					return true;
				}

			foreach (var candidate in Candidates)
			{
				var arg = candidate.GetCustomAttribute<ArgumentCandidateAttribute>();
				if(arg == null || arg.OptionName != t) continue;

				IArgParse<object> argpars = getChildParse(candidate);
				if (argpars == null) continue;

				value = argpars.Activate("");
				if (value != null) return true;
			}

			return false;
		}

		bool tryComposite(CopToken t, Type target, out object value)
		{
			value = null;
			if (t == null) return false;
			foreach (var candidate in Candidates)
			{
				if (!target.IsAssignableFrom(candidate)) continue;

				var arg = candidate.GetCustomAttribute<ArgumentCandidateAttribute>();
				if (arg == null || arg.OptionName != t.Lex) continue;

				IArgParse<object> argpars = getChildParse(candidate);
				if(argpars == null) continue;

				value = argpars.Activate(t.Composite);
				if (value != null) return true;
			}
			return false;
		}

		bool tryDefault(Type target, object dflt, out object value)
		{
			if (dflt != null)
			{
				if( target.IsInstanceOfType(dflt))
				{
					value = dflt;
					return true;
				}
				string tdflt = dflt as string;
				if (tdflt != null)
				{
					if (tryParse(tdflt, target, null, null, out value)) return true;
					var ct = new Lexer(tdflt).FirstOrDefault() as CopToken;
					if (ct != null)
						return tryComposite(ct, target, out value);
				}
			}
			value = null;
			return false;
		}

		public void RegisterComposite<T>()
			where T : class
		{
			Candidates.Add(typeof(T));
		}

		public void RegisterComposite(Type t)
		{
			Candidates.Add(t);
		}

		public void RegisterParser<T>(Func<string, ITuple<T, bool>> parser)
			where T : class
		{
			if (parsers.ContainsKey(typeof(T))) parsers[typeof(T)].Add(parser);
			else
				parsers[typeof(T)] = new List<Func<string, ITuple<object, bool>>> {parser};
		}

		public void RegisterParser(Type t, Func<string, ITuple<object, bool>> parser)
		{
			if (parsers.ContainsKey(t)) parsers[t].Add(parser);
			else
				parsers[t] = new List<Func<string, ITuple<object, bool>>> { parser };
		}

		public void Help(TextWriter t, string command = null)
		{
			throw new NotImplementedException();
		}

		IArgParse<object> getChildParse(Type t)
		{
			IArgParse<object> argpars;
			if (reflectedtypes.TryGetValue(t, out argpars)) return argpars;
			var argparsctor = typeof(ArgParse<>).MakeGenericType(t).GetConstructor(Type.EmptyTypes);
			if (argparsctor == null) return null;

			argpars = (IArgParse<object>)argparsctor.Invoke(new object[0]);
			foreach (var type in Candidates)
				argpars.RegisterComposite(type);
			foreach (var parser in parsers)
			{
				foreach (var func in parser.Value)
					argpars.RegisterParser(parser.Key, func);
			}
			reflectedtypes[t] = argpars;
			return argpars;
		}

		static MethodInfo getMethod(IReflect handler, string name, Type[] args)
		{
			var method =
				from m in
				handler.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
				where m.Name == name && (from p in m.GetParameters() select p.ParameterType).SequenceEqual(args)
				select m;
			return method.FirstOrDefault();
		}
	}
}
