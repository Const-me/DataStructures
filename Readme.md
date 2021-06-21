# DataStructures

.NET standard library is awesome in general, but still, I think a few important pieces are missing.

## IntervalsList

The problem statement is written in [this SO question](https://stackoverflow.com/q/19473671/126995) from 2013.

The problem statement looks trivially simple yet it’s surprisingly difficult to solve in a good way.

The higher-level collection classes 
([SortedList](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.sortedlist-2?view=net-5.0),
[SortedDictionary](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.sorteddictionary-2?view=net-5.0),
[SortedSet](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.sortedset-1?view=net-5.0))
ain’t good enough for that thing. None of them expose sufficient amount of implementation details to implement these intervals efficiently.
For optimal performance, need to replace keys, and insert/erase ranges of nodes, none of these containers support these use cases.

For this reason, I’ve made a generic solution on top of sorted arrays.

Internally, the data structure is very similar to the [SortedList](https://github.com/dotnet/corefx/blob/master/src/System.Collections/src/System/Collections/Generic/SortedList.cs/)
from the standard library. I’ve copy-pasted a few lines of code from there, fortunately the MIT license allows that.