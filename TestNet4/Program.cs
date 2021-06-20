using System;
using System.Collections.Generic;
using System.Linq;
using Interval = System.ValueTuple<int, int, string>;
using List = System.Collections.Generic.IntervalsList<int, string>;

namespace TestNet4
{
	static class Program
	{
		static void assert( List iv, IEnumerable<Interval> expected )
		{
			bool eq = iv.SequenceEqual( expected );
			if( eq )
				return;
			throw new ApplicationException();
		}

		static void assert( List iv, params object[] expected )
		{
			IEnumerable<Interval> makeTuples()
			{
				for( int i = 0; i < expected.Length; i += 3 )
					yield return ((int)expected[ i ], (int)expected[ i + 1 ], (string)expected[ i + 2 ]);
			}
			assert( iv, makeTuples() );
		}

		static void Main( string[] args )
		{
			var iv = new List();

			iv.SetInterval( 0, 5, "11" );
			assert( iv, 0, 5, "11" );

			iv.SetInterval( 5, 6, "12" );
			assert( iv, 0, 5, "11", 5, 6, "12" );

			iv.SetInterval( 3, 6, "33" );
			assert( iv, 0, 3, "11", 3, 6, "33" );

			iv.SetInterval( -1, 10, "4" );
			assert( iv, -1, 10, "4" );

			iv.Clear();
			assert( iv );

			// Test intervals collapse
			iv.SetInterval( 1, 2, "a" );
			iv.SetInterval( 2, 3, "b" );
			iv.SetInterval( 3, 4, "a" );
			assert( iv, 1, 2, "a", 2, 3, "b", 3, 4, "a" );
			iv.SetInterval( 2, 3, "a" );
			assert( iv, 1, 4, "a" );

			iv.Clear();
			iv.SetInterval( 1, 2, "a" );
			iv.SetInterval( 3, 4, "c" );
			assert( iv, 1, 2, "a", 2, 3, null, 3, 4, "c" );
			iv.SetInterval( 2, 3, "c" );
			assert( iv, 1, 2, "a", 2, 4, "c" );
			iv.SetInterval( 2, 3, "a" );
			assert( iv, 1, 3, "a", 3, 4, "c" );

			Console.WriteLine( "Passed" );
		}
	}
}