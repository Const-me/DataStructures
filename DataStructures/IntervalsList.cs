using System.Runtime.CompilerServices;

namespace System.Collections.Generic
{
	/// <summary>Represents a set of non-intersecting intervals, with a value stored per interval.</summary>
	/// <remarks>The data structure inside this thing is very similar to <see cref="SortedList{TKey, TValue}" />, but exposes different API.</remarks>
	public sealed class IntervalsList<TKey, TValue>:
		IEnumerable<(TKey, TKey, TValue)>
	{
		TKey[] keys;
		TValue[] values;
		// Count of points in the collections; count of intervals is 1 less than this.
		int _size;

		/// <summary>Defines order of keys in the data structure</summary>
		public IComparer<TKey> KeyComparer { get; }
		/// <summary>Defines equality of the values in the data structure</summary>
		public IEqualityComparer<TValue> ValueComparer { get; }
		/// <summary>If you call SetInterval passing two disjoint intervals, the empty space between them will use this value for the third interval.</summary>
		public TValue EmptyValue { get; }

		/// <summary>Create an empty collection</summary>
		public IntervalsList( IComparer<TKey> keyComparer = null, IEqualityComparer<TValue> valueComparer = null, TValue emptyValue = default )
		{
			KeyComparer = keyComparer ?? Comparer<TKey>.Default;
			ValueComparer = valueComparer ?? EqualityComparer<TValue>.Default;
			EmptyValue = emptyValue;
			keys = Array.Empty<TKey>();
			values = Array.Empty<TValue>();
			_size = 0;
		}

		/// <summary>Removes all entries from this collection.</summary>
		public void Clear()
		{
			if( RuntimeHelpers.IsReferenceOrContainsReferences<TKey>() )
				Array.Clear( keys, 0, _size );
			if( RuntimeHelpers.IsReferenceOrContainsReferences<TValue>() )
				Array.Clear( values, 0, _size );
			_size = 0;
		}

		/// <summary>Returns the number of intervals in the collection.</summary>
		public int Count => ( _size > 0 ) ? _size - 1 : 0;

		/// <summary>Return interval by index</summary>
		public (TKey, TKey, TValue) this[ int index ]
		{
			get
			{
				if( index < 0 || index >= Count )
					throw new ArgumentOutOfRangeException();
				return (keys[ index ], keys[ index + 1 ], values[ index ]);
			}
		}

		IEnumerable<(TKey, TKey, TValue)> enumerate()
		{
			if( _size < 2 )
				yield break;

			TKey key = keys[ 0 ];
			TValue value = values[ 0 ];
			for( int i = 1; i < _size; i++ )
			{
				TKey nextKey = keys[ i ];
				yield return (key, nextKey, value);
				key = nextKey;
				value = values[ i ];
			}
		}

		IEnumerator<(TKey, TKey, TValue)> IEnumerable<(TKey, TKey, TValue)>.GetEnumerator() =>
			enumerate().GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() =>
			enumerate().GetEnumerator();

		/// <summary>Gets or sets the number of nodes the collection can contain.</summary>
		public int Capacity
		{
			get => keys.Length;
			set
			{
				if( value != keys.Length )
				{
					if( value < _size )
						throw new ArgumentOutOfRangeException( nameof( value ), value, "The capacity is too small" );

					if( value > 0 )
					{
						TKey[] newKeys = new TKey[ value ];
						TValue[] newValues = new TValue[ value ];
						if( _size > 0 )
						{
							Array.Copy( keys, newKeys, _size );
							Array.Copy( values, newValues, _size );
						}
						keys = newKeys;
						values = newValues;
					}
					else
					{
						keys = Array.Empty<TKey>();
						values = Array.Empty<TValue>();
					}
				}
			}
		}

		const int DefaultCapacity = 4;
		const int MaxArrayLength = 0x7FEFFFFF;

		// Ensures that the capacity of this collection is at least the given minimum value.
		// If the current capacity of the list is less than min, the capacity is increased to twice the current capacity or to min, whichever is larger.
		void ensureCapacity( int min )
		{
			if( min <= Capacity )
				return;

			int newCapacity = keys.Length == 0 ? DefaultCapacity : keys.Length * 2;
			// Allow the list to grow to maximum possible capacity (~2G elements) before encountering overflow.
			// Note that this check works even when _items.Length overflowed thanks to the (uint) cast
			if( (uint)newCapacity > MaxArrayLength ) newCapacity = MaxArrayLength;
			if( newCapacity < min ) newCapacity = min;
			Capacity = newCapacity;
		}

		/// <summary>Run binary search on the keys</summary>
		int binarySearch( TKey value, out bool found, int lower = 0 )
		{
			int upper = _size - 1;

			while( lower <= upper )
			{
				int middle = lower + ( upper - lower ) / 2;
				int comparisonResult = KeyComparer.Compare( value, keys[ middle ] );
				if( comparisonResult == 0 )
				{
					found = true;
					return middle;
				}
				else if( comparisonResult < 0 )
					upper = middle - 1;
				else
					lower = middle + 1;
			}
			found = false;
			return lower;
		}

		void eraseNodes( int index, int count )
		{
			if( count < 0 || index < 0 || index + count > _size )
				throw new ArgumentOutOfRangeException();
			if( count == 0 )
				return;

			if( index + count < _size )
			{
				int tail = _size - ( index + count );
				Array.Copy( keys, index + count, keys, index, tail );
				Array.Copy( values, index + count, values, index, tail );
			}

			_size -= count;

			if( RuntimeHelpers.IsReferenceOrContainsReferences<TKey>() )
				keys.AsSpan().Slice( _size, count ).Fill( default );

			if( RuntimeHelpers.IsReferenceOrContainsReferences<TValue>() )
				values.AsSpan().Slice( _size, count ).Fill( default );
		}

		void insertNodes( int index, int count )
		{
			if( count < 0 || index < 0 )
				throw new ArgumentOutOfRangeException();
			if( count == 0 )
				return;

			if( index + count < _size )
			{
				int tail = _size - ( index + count );
				Array.Copy( keys, index, keys, index + count, tail );
				Array.Copy( values, index, values, index + count, tail );
			}

			_size += count;
		}

		bool isSameValueAt( int i, TValue value ) =>
			ValueComparer.Equals( values[ i ], value );

		bool isSameValue( TValue a, TValue b ) =>
			ValueComparer.Equals( a, b );

		/// <summary>Assign value to an interval of keys</summary>
		/// <remarks>The method automatically collapses intervals with equal values into fewer larger intervals.</remarks>
		/// <param name="rangeBegin">Start point of the interval</param>
		/// <param name="rangeEnd">End point of the interval; must be &gt;> rangeBegin. When they are equal, this method will do nothing.</param>
		/// <param name="value">Value to assign to the interval.</param>
		/// <returns>Zero-based index of the new interval, or -1 if there's none</returns>
		public int SetInterval( TKey rangeBegin, TKey rangeEnd, TValue value )
		{
			// Validate the input interval
			int cmp = KeyComparer.Compare( rangeBegin, rangeEnd );
			if( cmp > 0 )
				throw new ArgumentOutOfRangeException( "Must be less than or equal to upperValue." );   // SR.SortedSet_LowerValueGreaterThanUpperValue
			if( cmp == 0 )
				return -1; // The interval is empty, nothing to do

			// Index of the rangeBegin in the list
			int idxBegin = binarySearch( rangeBegin, out bool foundBegin );

			// Count of points before the affected area, including the new or updated point with the rangeBegin key
			if( !foundBegin )
			{
				// Might need a new point at the start
				if( idxBegin > 0 )
				{
					if( isSameValueAt( idxBegin - 1, value ) )
					{
						// No need to insert new points, the previous interval has the same value
						foundBegin = true;
						idxBegin--;
					}
				}
			}
			else
			{
				// The start point is already there..
				if( idxBegin > 0 && isSameValueAt( idxBegin - 1, value ) )
				{
					// .. the previous interval has the same value we're setting for the new one, merging the two intervals
					foundBegin = true;
					idxBegin--;
				}
			}

			// Index of the rangeEnd element in the current list. Can be negative, will become normal once the arrays are resized.
			int idxEnd = binarySearch( rangeEnd, out bool foundEnd, idxBegin );
			// Deal with the end point of the interval
			TValue intervalEndValue = EmptyValue;
			if( !foundEnd )
			{
				// Need a new point at the end of the interval
				if( idxEnd < _size )
				{
					// The end of the range was in the middle of an existing interval in the collection..
					intervalEndValue = values[ idxEnd - 1 ];
					if( isSameValue( intervalEndValue, value ) && idxEnd > idxBegin )
					{
						// That interval had the same value
						foundEnd = true;
						idxEnd -= 2;
					}
					else
						idxEnd--;
				}
				else
					idxEnd--;
			}
			else
			{
				// The end point is already there..
				if( isSameValueAt( idxEnd, value ) )
				{
					// And it has the same value so we can collapse
					idxEnd++;
				}
			}

			// Finally, we're ready to update these arrays.
			int newSize = idxBegin + 1 + ( _size - idxEnd );
			ensureCapacity( newSize );

			if( newSize > _size )
				insertNodes( idxBegin + 1, newSize - _size );
			else if( newSize < _size )
				eraseNodes( idxBegin + 1, _size - newSize );

			if( !foundBegin )
				keys[ idxBegin ] = rangeBegin;
			values[ idxBegin ] = value;

			if( !foundEnd )
				keys[ idxBegin + 1 ] = rangeEnd;
			values[ idxBegin + 1 ] = intervalEndValue;

			return idxBegin;
		}

		/// <summary>List all intervals, excluding ones with the empty value.</summary>
		public IEnumerable<(TKey, TKey, TValue)> NonEmptyIntervals()
		{
			if( _size < 2 )
				yield break;

			TKey key = keys[ 0 ];
			TValue value = values[ 0 ];
			for( int i = 1; i < _size; i++ )
			{
				TKey nextKey = keys[ i ];
				if( !isSameValue( value, EmptyValue ) )
					yield return (key, nextKey, value);
				key = nextKey;
				value = values[ i ];
			}
		}
	}
}