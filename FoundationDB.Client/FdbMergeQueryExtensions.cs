﻿#region BSD Licence
/* Copyright (c) 2013, Doxense SARL
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:
	* Redistributions of source code must retain the above copyright
	  notice, this list of conditions and the following disclaimer.
	* Redistributions in binary form must reproduce the above copyright
	  notice, this list of conditions and the following disclaimer in the
	  documentation and/or other materials provided with the distribution.
	* Neither the name of Doxense nor the
	  names of its contributors may be used to endorse or promote products
	  derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */
#endregion

namespace FoundationDB.Client
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Doxense.Diagnostics.Contracts;
	using JetBrains.Annotations;
	using FoundationDB.Async;
	using FoundationDB.Linq;

	public static class FdbMergeQueryExtensions
	{

		#region MergeSort (x OR y)

		[Pure, NotNull, LinqTunnel]
		public static IFdbAsyncEnumerable<KeyValuePair<Slice, Slice>> MergeSort<TKey>([NotNull] this IFdbReadOnlyTransaction trans, IEnumerable<KeySelectorPair> ranges, Func<KeyValuePair<Slice, Slice>, TKey> keySelector, IComparer<TKey> keyComparer = null)
		{
			//TODO: Range options ?
			Contract.NotNull(trans, nameof(trans));
			Contract.NotNull(ranges, nameof(ranges));
			Contract.NotNull(keySelector, nameof(keySelector));

			trans.EnsureCanRead();
			return new FdbMergeSortIterator<KeyValuePair<Slice, Slice>, TKey, KeyValuePair<Slice, Slice>>(
				ranges.Select(range => trans.GetRange(range, new FdbRangeOptions { Mode = FdbStreamingMode.Iterator })),
				default(int?),
				keySelector,
				(kv) => kv,
				keyComparer
			);
		}

		[Pure, NotNull, LinqTunnel]
		public static IFdbAsyncEnumerable<TResult> MergeSort<TKey, TResult>([NotNull] this IFdbReadOnlyTransaction trans, [NotNull] IEnumerable<KeySelectorPair> ranges, Func<KeyValuePair<Slice, Slice>, TKey> keySelector, Func<KeyValuePair<Slice, Slice>, TResult> resultSelector, IComparer<TKey> keyComparer = null)
		{
			//TODO: Range options ?
			Contract.NotNull(trans, nameof(trans));
			Contract.NotNull(ranges, nameof(ranges));
			Contract.NotNull(keySelector, nameof(keySelector));
			Contract.NotNull(resultSelector, nameof(resultSelector));

			trans.EnsureCanRead();
			return new FdbMergeSortIterator<KeyValuePair<Slice, Slice>, TKey, TResult>(
				ranges.Select(range => trans.GetRange(range, new FdbRangeOptions { Mode = FdbStreamingMode.Iterator })),
				default(int?),
				keySelector,
				resultSelector,
				keyComparer
			);
		}

		[Pure, NotNull, LinqTunnel]
		public static IFdbAsyncEnumerable<TResult> Union<TKey, TResult>([NotNull] IEnumerable<IFdbAsyncEnumerable<TResult>> sources, Func<TResult, TKey> keySelector, IComparer<TKey> keyComparer = null)
		{
			Contract.NotNull(sources, nameof(sources));
			Contract.NotNull(keySelector, nameof(keySelector));
			return new FdbMergeSortIterator<TResult, TKey, TResult>(
				sources,
				null,
				keySelector,
				(x) => x,
				keyComparer
			);
		}

		[Pure, NotNull, LinqTunnel]
		public static IFdbAsyncEnumerable<TResult> Union<TResult>([NotNull] IEnumerable<IFdbAsyncEnumerable<TResult>> sources, IComparer<TResult> keyComparer = null)
		{
			Contract.NotNull(sources, nameof(sources));
			return new FdbMergeSortIterator<TResult, TResult, TResult>(
				sources,
				null,
				(x) => x,
				(x) => x,
				keyComparer
			);
		}

		#endregion

		#region Intersect (x AND y)

		[Pure, NotNull, LinqTunnel]
		public static IFdbAsyncEnumerable<KeyValuePair<Slice, Slice>> Intersect<TKey>([NotNull] this IFdbReadOnlyTransaction trans, IEnumerable<KeySelectorPair> ranges, Func<KeyValuePair<Slice, Slice>, TKey> keySelector, IComparer<TKey> keyComparer = null)
		{
			//TODO: Range options ?
			Contract.NotNull(trans, nameof(trans));
			Contract.NotNull(ranges, nameof(ranges));
			Contract.NotNull(keySelector, nameof(keySelector));

			trans.EnsureCanRead();
			return new FdbIntersectIterator<KeyValuePair<Slice, Slice>, TKey, KeyValuePair<Slice, Slice>>(
				ranges.Select(range => trans.GetRange(range, new FdbRangeOptions { Mode = FdbStreamingMode.Iterator })),
				default(int?),
				keySelector,
				(kv) => kv,
				keyComparer
			);
		}

		[Pure, NotNull, LinqTunnel]
		public static IFdbAsyncEnumerable<TResult> Intersect<TKey, TResult>([NotNull] this IFdbReadOnlyTransaction trans, IEnumerable<KeySelectorPair> ranges, Func<KeyValuePair<Slice, Slice>, TKey> keySelector, Func<KeyValuePair<Slice, Slice>, TResult> resultSelector, IComparer<TKey> keyComparer = null)
		{
			//TODO: Range options ?

			trans.EnsureCanRead();
			return new FdbIntersectIterator<KeyValuePair<Slice, Slice>, TKey, TResult>(
				ranges.Select(range => trans.GetRange(range, new FdbRangeOptions { Mode = FdbStreamingMode.Iterator })),
				default(int?),
				keySelector,
				resultSelector,
				keyComparer
			);
		}

		[Pure, NotNull, LinqTunnel]
		public static IFdbAsyncEnumerable<TResult> Intersect<TKey, TResult>([NotNull] this IFdbAsyncEnumerable<TResult> first, IFdbAsyncEnumerable<TResult> second, Func<TResult, TKey> keySelector, IComparer<TKey> keyComparer = null)
		{
			Contract.NotNull(first, nameof(first));
			Contract.NotNull(second, nameof(second));
			return new FdbIntersectIterator<TResult, TKey, TResult>(
				new[] { first, second },
				null,
				keySelector,
				(x) => x,
				keyComparer
			);
		}

		[Pure, NotNull, LinqTunnel]
		public static IFdbAsyncEnumerable<TResult> Intersect<TResult>([NotNull] this IFdbAsyncEnumerable<TResult> first, IFdbAsyncEnumerable<TResult> second, IComparer<TResult> comparer = null)
		{
			Contract.NotNull(first, nameof(first));
			Contract.NotNull(second, nameof(second));
			return new FdbIntersectIterator<TResult, TResult, TResult>(
				new [] { first, second },
				null,
				(x) => x,
				(x) => x,
				comparer
			);
		}

		[Pure, NotNull, LinqTunnel]
		public static IFdbAsyncEnumerable<TResult> Intersect<TKey, TResult>([NotNull] IEnumerable<IFdbAsyncEnumerable<TResult>> sources, Func<TResult, TKey> keySelector, IComparer<TKey> keyComparer = null)
		{
			Contract.NotNull(sources, nameof(sources));
			Contract.NotNull(keySelector, nameof(keySelector));
			return new FdbIntersectIterator<TResult, TKey, TResult>(
				sources,
				null,
				keySelector,
				(x) => x,
				keyComparer
			);
		}

		[Pure, NotNull, LinqTunnel]
		public static IFdbAsyncEnumerable<TResult> Intersect<TResult>([NotNull] IEnumerable<IFdbAsyncEnumerable<TResult>> sources, IComparer<TResult> keyComparer = null)
		{
			Contract.NotNull(sources, nameof(sources));
			return new FdbIntersectIterator<TResult, TResult, TResult>(
				sources,
				null,
				(x) => x,
				(x) => x,
				keyComparer
			);
		}

		#endregion

		#region Except (x AND NOT y)

		/// <summary>Return the keys that are in the first range, but not in the others</summary>
		/// <typeparam name="TKey">Type of the keys returned by the query</typeparam>
		/// <param name="trans">Transaction used by the operation</param>
		/// <param name="ranges">List of at least one key selector pairs</param>
		/// <param name="keySelector">Lambda called to extract the keys from the ranges</param>
		/// <param name="keyComparer">Instance used to compare the keys returned by <paramref name="keySelector"/></param>
		/// <returns>Async query that returns only the results that are in the first range, and not in any other range.</returns>
		[Pure, NotNull, LinqTunnel]
		public static IFdbAsyncEnumerable<KeyValuePair<Slice, Slice>> Except<TKey>([NotNull] this IFdbReadOnlyTransaction trans, IEnumerable<KeySelectorPair> ranges, Func<KeyValuePair<Slice, Slice>, TKey> keySelector, IComparer<TKey> keyComparer = null)
		{
			//TODO: Range options ?
			Contract.NotNull(trans, nameof(trans));
			Contract.NotNull(ranges, nameof(ranges));
			Contract.NotNull(keySelector, nameof(keySelector));

			trans.EnsureCanRead();
			return new FdbExceptIterator<KeyValuePair<Slice, Slice>, TKey, KeyValuePair<Slice, Slice>>(
				ranges.Select(range => trans.GetRange(range, new FdbRangeOptions { Mode = FdbStreamingMode.Iterator })),
				default(int?),
				keySelector,
				(kv) => kv,
				keyComparer
			);
		}

		/// <summary>Return the keys that are in the first range, but not in the others</summary>
		/// <typeparam name="TKey">Type of the keys returned by the query</typeparam>
		/// <param name="trans">Transaction used by the operation</param>
		/// <param name="ranges">List of at least one key range</param>
		/// <param name="keySelector">Lambda called to extract the keys from the ranges</param>
		/// <param name="keyComparer">Instance used to compare the keys returned by <paramref name="keySelector"/></param>
		/// <returns>Async query that returns only the results that are in the first range, and not in any other range.</returns>
		[Pure, NotNull, LinqTunnel]
		public static IFdbAsyncEnumerable<KeyValuePair<Slice, Slice>> Except<TKey>([NotNull] this IFdbReadOnlyTransaction trans, IEnumerable<KeyRange> ranges, Func<KeyValuePair<Slice, Slice>, TKey> keySelector, IComparer<TKey> keyComparer = null)
		{
			Contract.NotNull(ranges, nameof(ranges));
			return Except<TKey>(trans, ranges.Select(r => KeySelectorPair.Create(r)), keySelector, keyComparer);
		}

		/// <summary>Return the keys that are in the first range, but not in the others</summary>
		/// <typeparam name="TKey">Type of the keys used for the comparison</typeparam>
		/// <typeparam name="TResult">Type of the results returned by the query</typeparam>
		/// <param name="trans">Transaction used by the operation</param>
		/// <param name="ranges">List of at least one key selector pairs</param>
		/// <param name="keySelector">Lambda called to extract the keys used by the sort</param>
		/// <param name="resultSelector">Lambda called to extract the values returned by the query</param>
		/// <param name="keyComparer">Instance used to compare the keys returned by <paramref name="keySelector"/></param>
		/// <returns>Async query that returns only the results that are in the first range, and not in any other range.</returns>
		[Pure, NotNull, LinqTunnel]
		public static IFdbAsyncEnumerable<TResult> Except<TKey, TResult>([NotNull] this IFdbReadOnlyTransaction trans, IEnumerable<KeySelectorPair> ranges, Func<KeyValuePair<Slice, Slice>, TKey> keySelector, Func<KeyValuePair<Slice, Slice>, TResult> resultSelector, IComparer<TKey> keyComparer = null)
		{
			//TODO: Range options ?

			trans.EnsureCanRead();
			return new FdbExceptIterator<KeyValuePair<Slice, Slice>, TKey, TResult>(
				ranges.Select(range => trans.GetRange(range, new FdbRangeOptions { Mode = FdbStreamingMode.Iterator })),
				default(int?),
				keySelector,
				resultSelector,
				keyComparer
			);
		}

		/// <summary>Return the keys that are in the first range, but not in the others</summary>
		/// <typeparam name="TKey">Type of the keys used for the comparison</typeparam>
		/// <typeparam name="TResult">Type of the results returned by the query</typeparam>
		/// <param name="trans">Transaction used by the operation</param>
		/// <param name="ranges">List of at least one key ranges</param>
		/// <param name="keySelector">Lambda called to extract the keys used by the sort</param>
		/// <param name="resultSelector">Lambda called to extract the values returned by the query</param>
		/// <param name="keyComparer">Instance used to compare the keys returned by <paramref name="keySelector"/></param>
		/// <returns>Async query that returns only the results that are in the first range, and not in any other range.</returns>
		[Pure, NotNull, LinqTunnel]
		public static IFdbAsyncEnumerable<TResult> Except<TKey, TResult>([NotNull] this IFdbReadOnlyTransaction trans, [NotNull] IEnumerable<KeyRange> ranges, [NotNull] Func<KeyValuePair<Slice, Slice>, TKey> keySelector, [NotNull] Func<KeyValuePair<Slice, Slice>, TResult> resultSelector, IComparer<TKey> keyComparer = null)
		{
			Contract.NotNull(ranges, nameof(ranges));
			return Except<TKey, TResult>(trans, ranges.Select(r => KeySelectorPair.Create(r)), keySelector, resultSelector, keyComparer);
		}

		/// <summary>Sequence the return only the elements of <paramref name="first"/> that are not in <paramref name="second"/>, using a custom key comparison</summary>
		/// <typeparam name="TKey">Type of the keys that will be used for comparison</typeparam>
		/// <typeparam name="TResult">Type of the results of the query</typeparam>
		/// <param name="first">Fisrt query that contains the elements that could be in the result</param>
		/// <param name="second">Second query that contains the elements that cannot be in the result</param>
		/// <param name="keySelector">Lambda used to extract keys from both queries.</param>
		/// <param name="keyComparer">Instance used to compare keys</param>
		/// <returns>Async query that returns only the elements that are in <paramref name="first"/>, and not in <paramref name="second"/></returns>
		[Pure, NotNull, LinqTunnel]
		public static IFdbAsyncEnumerable<TResult> Except<TKey, TResult>([NotNull] this IFdbAsyncEnumerable<TResult> first, [NotNull] IFdbAsyncEnumerable<TResult> second, [NotNull] Func<TResult, TKey> keySelector, IComparer<TKey> keyComparer = null)
		{
			Contract.NotNull(first, nameof(first));
			Contract.NotNull(second, nameof(second));
			Contract.NotNull(keySelector, nameof(keySelector));
			return new FdbExceptIterator<TResult, TKey, TResult>(
				new[] { first, second },
				null,
				keySelector,
				(x) => x,
				keyComparer
			);
		}

		/// <summary>Sequence the return only the elements of <paramref name="first"/> that are not in <paramref name="second"/></summary>
		/// <typeparam name="TResult">Type of the results of the query</typeparam>
		/// <param name="first">Fisrt query that contains the elements that could be in the result</param>
		/// <param name="second">Second query that contains the elements that cannot be in the result</param>
		/// <param name="comparer">Instance used to compare elements</param>
		/// <returns>Async query that returns only the elements that are in <paramref name="first"/>, and not in <paramref name="second"/></returns>
		[Pure, NotNull, LinqTunnel]
		public static IFdbAsyncEnumerable<TResult> Except<TResult>([NotNull] this IFdbAsyncEnumerable<TResult> first, [NotNull] IFdbAsyncEnumerable<TResult> second, IComparer<TResult> comparer = null)
		{
			Contract.NotNull(first, nameof(first));
			Contract.NotNull(second, nameof(second));
			return new FdbExceptIterator<TResult, TResult, TResult>(
				new[] { first, second },
				null,
				(x) => x,
				(x) => x,
				comparer
			);
		}

		#endregion

	}

}
