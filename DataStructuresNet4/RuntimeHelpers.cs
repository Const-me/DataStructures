using System.Reflection;

namespace System.Collections.Generic
{
	/// <summary>Compatibility thing to use IntervalsList in legacy .NET 4.7.</summary>
	static class RuntimeHelpers
	{
		static readonly Dictionary<Type, bool> dict = new Dictionary<Type, bool>();

		static bool isReference( Type type, HashSet<Type> visited )
		{
			if( !type.IsValueType )
				return true;
			if( type.IsEnum || type.IsPrimitive )
				return false;
			const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
			foreach( var fi in type.GetFields( bindingFlags ) )
			{
				Type fieldType = fi.FieldType;
				if( visited.Contains( fieldType ) )
					continue;
				visited.Add( fieldType );
				if( isReference( fieldType, visited ) )
					return true;
			}
			return false;
		}

		public static bool IsReferenceOrContainsReferences<T>()
		{
			Type type = typeof( T );
			if( !type.IsValueType )
				return true;
			if( type.IsEnum || type.IsPrimitive )
				return false;

			lock( dict )
			{
				if( dict.TryGetValue( type, out bool res ) )
					return res;
				res = isReference( type, new HashSet<Type>() );
				dict.Add( type, res );
				return res;
			}
		}
	}
}