using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;

internal static class Walk
{
	public static object Target;
	public static void FindLargeRecordArrays(object root, int minLength)
	{
		var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
		var queue = new Queue<(object Obj, string Path)>();
		queue.Enqueue((root, "engine"));
		seen.Add(root);
		foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
		{
			if (!asm.GetName().Name!.StartsWith("BlueDotBrigade", StringComparison.Ordinal)) continue;
			Type[] types;
			try { types = asm.GetTypes(); } catch (ReflectionTypeLoadException e) { types = e.Types.Where(x => x != null).ToArray()!; }
			foreach (var st in types)
			{
				if (st.ContainsGenericParameters) continue;
				foreach (var f in st.GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly))
				{
					if (f.FieldType.IsPrimitive || f.FieldType == typeof(string) || f.IsLiteral) continue;
					object v; try { v = f.GetValue(null); } catch { continue; }
					if (v != null && seen.Add(v)) queue.Enqueue((v, st.Name + "::" + f.Name));
				}
			}
		}
		while (queue.Count > 0)
		{
			var (obj, path) = queue.Dequeue();
			if (ReferenceEquals(obj, Target)) { Console.WriteLine("TARGET via " + path); continue; }
			var tn = obj.GetType().Name;
			if (tn is "LogLineReader" or "MultilineRecordParser" or "SerializedRecordRepository" or "StreamReader" or "FileStream") Console.WriteLine($"RETAINED {tn} via {path}");
			if (obj is byte[] big && big.Length > 20_000_000) Console.WriteLine($"BIG byte[{big.Length}] via {path}");
			var type = obj.GetType();
			if (type.IsArray)
			{
				var arr = (Array)obj;
				if (arr.Length >= minLength && typeof(BlueDotBrigade.Weevil.Data.IRecord).IsAssignableFrom(type.GetElementType()))
				{
					Console.WriteLine($"{path} : {type.Name}[{arr.Length}]");
					continue;
				}
				var et = type.GetElementType()!;
				if (et.IsPrimitive || et.IsEnum) continue;
				if (et.IsValueType)
				{
					var refFields = et.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Where(x => !x.FieldType.IsValueType && x.FieldType != typeof(string)).ToArray();
					if (refFields.Length == 0) continue;
					var j = 0;
					foreach (var item in arr)
					{
						foreach (var rf in refFields)
						{
							var rv = rf.GetValue(item);
							if (rv != null && seen.Add(rv)) queue.Enqueue((rv, path + $"[{j}]." + rf.Name));
						}
						if (++j > 200000) break;
					}
					continue;
				}
				if (arr.Length > 200000) continue;
				var i = 0;
				foreach (var item in arr)
				{
					if (item != null && !(item is string) && seen.Add(item)) queue.Enqueue((item, path + $"[{i}]"));
					i++;
				}
				continue;
			}
			if (path.Split('.').Length > 40) continue;
			for (var t = type; t != null; t = t.BaseType)
			{
				foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly))
				{
					if (f.FieldType.IsPrimitive || f.FieldType == typeof(string) || f.FieldType.IsPointer) continue;
					object v;
					try { v = f.GetValue(obj); } catch { continue; }
					if (v == null) continue;
					if (f.FieldType.IsValueType)
					{
						// Look inside structs such as ImmutableArray<T>.
						var vt = v.GetType();
						foreach (var sf in vt.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
						{
							if (sf.FieldType.IsValueType || sf.FieldType == typeof(string)) continue;
							var sv = sf.GetValue(v);
							if (sv != null && seen.Add(sv)) queue.Enqueue((sv, path + "." + f.Name + "." + sf.Name));
						}
						continue;
					}
					if (seen.Add(v)) queue.Enqueue((v, path + "." + f.Name));
				}
			}
		}
	}
}
