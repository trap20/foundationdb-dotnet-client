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
	* Neither the name of the <organization> nor the
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

// enable this to help debug native calls to fdbc.dll
#undef DEBUG_NATIVE_CALLS

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace FoundationDb.Client.Native
{
	internal static unsafe class FdbNative
	{
		public const int FDB_API_VERSION = 21;

		private const string DLL_X86 = "fdb_c.dll";
		private const string DLL_X64 = "fdb_c.dll";

		/// <summary>Handle on the native FDB C API library</summary>
		private static readonly UnmanagedLibrary FdbCLib;

		/// <summary>Exception that was thrown when we last tried to load the native FDB C library (or null if nothing wrong happened)</summary>
		private static readonly Exception LibraryLoadError;

		/// <summary>Holds all the delegate types for the binding with the native C API</summary>
		public static class Delegates
		{

			public delegate byte* FdbGetError(FdbError code);
			public delegate FdbError FdbSelectApiVersionImpl(int runtimeVersion, int headerVersion);
			public delegate int FdbGetMaxApiVersion();

			public delegate FdbError FdbNetworkSetOption(FdbNetworkOption option, byte* value, int value_length);
			public delegate FdbError FdbSetupNetwork();
			public delegate FdbError FdbRunNetwork();
			public delegate FdbError FdbStopNetwork();

			public delegate /*Future*/IntPtr FdbCreateCluster(byte* clusterFilePath);
			public delegate void FdbClusterDestroy(/*FDBCluster*/IntPtr cluster);
			public delegate FdbError FdbClusterSetOption(ClusterHandle cluster, FdbClusterOption option, byte* value, int valueLength);
			public delegate /*Future*/IntPtr FdbClusterCreateDatabase(ClusterHandle cluster, byte* dbName, int dbNameLength);

			public delegate void FdbDatabaseDestroy(/*FDBDatabase*/IntPtr database);
			public delegate FdbError FdbDatabaseSetOption(DatabaseHandle handle, FdbDatabaseOption option, byte* value, int valueLength);
			public delegate FdbError FdbDatabaseCreateTransaction(DatabaseHandle database, out IntPtr transaction);

			public delegate void FdbTransactionDestroy(/*FdbTransaction*/IntPtr transaction);
			public delegate FdbError FdbTransactionSetOption(TransactionHandle handle, FdbTransactionOption option, byte* value, int valueLength);
			public delegate void FdbTransactionSetReadVersion(TransactionHandle handle, long version);
			public delegate void FdbTransactionSet(TransactionHandle transaction, byte* keyName, int keyNameLength, byte* value, int valueLength);
			public delegate /*Future*/IntPtr FdbTransactionCommit(TransactionHandle transaction);
			public delegate FdbError FdbTransactionGetCommmittedVersion(TransactionHandle transaction, out long version);
			public delegate /*Future*/IntPtr FdbTransactionGetReadVersion(TransactionHandle transaction);
			public delegate /*Future*/IntPtr FdbTransactionGet(TransactionHandle transaction, byte* keyName, int keyNameLength, bool snapshot);
			public delegate /*Future*/IntPtr FdbTransactionGetKey(TransactionHandle transaction, byte* keyName, int keyNameLength, bool orEqual, int offset, bool snapshot);
			public delegate void FdbTransactionClear(TransactionHandle transaction, byte* keyName, int keyNameLength);
			public delegate void FdbTransactionClearRange(
				TransactionHandle transaction,
				byte* beginKeyName, int beginKeyNameLength,
				byte* endKeyName, int endKeyNameLength
			);
			public delegate /*Future*/IntPtr FdbTransactionGetRange(
				TransactionHandle transaction,
				byte* beginKeyName, int beginKeyNameLength, bool beginOrEqual, int beginOffset,
				byte* endKeyName, int endKeyNameLength, bool endOrEqual, int endOffset,
				int limit, int targetBytes, FdbStreamingMode mode, int iteration, bool snapshot, bool reverse
			);
			public delegate /*Future*/IntPtr FdbTransactionOnError(TransactionHandle transaction, FdbError error);
			public delegate void FdbTransactionReset(TransactionHandle transaction);

			public delegate void FdbFutureDestroy(/*FDBFuture*/IntPtr futureHandle);
			public delegate bool FdbFutureIsReady(FutureHandle futureHandle);
			public delegate bool FdbFutureIsError(FutureHandle futureHandle);
			public delegate FdbError FdbFutureGetError(FutureHandle future, byte** description);
			public delegate FdbError FdbFutureBlockUntilReady(FutureHandle futureHandle);
			public delegate FdbError FdbFutureSetCallback(FutureHandle future, /*FDBCallback*/ IntPtr callback, IntPtr callbackParameter);
			public delegate FdbError FdbFutureGetCluster(FutureHandle future, out /*FDBCluster*/IntPtr cluster);
			public delegate FdbError FdbFutureGetDatabase(/*Future*/IntPtr future, out /*FDBDatabase*/IntPtr database);
			public delegate FdbError FdbFutureGetVersion(FutureHandle future, out long version);
			public delegate FdbError FdbFutureGetValue(FutureHandle future, out bool present, out byte* value, out int valueLength);
			public delegate FdbError FdbFutureGetKey(FutureHandle future, out byte* key, out int keyLength);
			public delegate FdbError FdbFutureGetKeyValue(FutureHandle future, out FdbKeyValue* kv, out int count, out bool more);

		}

		/// <summary>Contain all the stubs to the methods exposed by the C API library</summary>
		public static class Stubs
		{

			// Core

			public static Delegates.FdbSelectApiVersionImpl fdb_select_api_version_impl;
			public static Delegates.FdbGetMaxApiVersion fdb_get_max_api_version;
			public static Delegates.FdbGetError fdb_get_error;

			// Network

			public static Delegates.FdbNetworkSetOption fdb_network_set_option;
			public static Delegates.FdbSetupNetwork fdb_setup_network;
			public static Delegates.FdbRunNetwork fdb_run_network;
			public static Delegates.FdbStopNetwork fdb_stop_network;

			// Cluster

			public static Delegates.FdbCreateCluster fdb_create_cluster;
			public static Delegates.FdbClusterDestroy fdb_cluster_destroy;
			public static Delegates.FdbClusterSetOption fdb_cluster_set_option;
			public static Delegates.FdbClusterCreateDatabase fdb_cluster_create_database;

			// Database

			public static Delegates.FdbDatabaseDestroy fdb_database_destroy;
			public static Delegates.FdbDatabaseSetOption fdb_database_set_option;
			public static Delegates.FdbDatabaseCreateTransaction fdb_database_create_transaction;

			// Transaction

			public static Delegates.FdbTransactionDestroy fdb_transaction_destroy;
			public static Delegates.FdbTransactionSetOption fdb_transaction_set_option;
			public static Delegates.FdbTransactionSetReadVersion fdb_transaction_set_read_version;
			public static Delegates.FdbTransactionGetReadVersion fdb_transaction_get_read_version;
			public static Delegates.FdbTransactionGet fdb_transaction_get;
			public static Delegates.FdbTransactionGetKey fdb_transaction_get_key;
			public static Delegates.FdbTransactionGetRange fdb_transaction_get_range;
			public static Delegates.FdbTransactionSet fdb_transaction_set;
			public static Delegates.FdbTransactionClear fdb_transaction_clear;
			public static Delegates.FdbTransactionClearRange fdb_transaction_clear_range;
			public static Delegates.FdbTransactionCommit fdb_transaction_commit;
			public static Delegates.FdbTransactionGetCommmittedVersion fdb_transaction_get_committed_version;
			public static Delegates.FdbTransactionOnError fdb_transaction_on_error;
			public static Delegates.FdbTransactionReset fdb_transaction_reset;

			// Future

			public static Delegates.FdbFutureDestroy fdb_future_destroy;
			public static Delegates.FdbFutureBlockUntilReady fdb_future_block_until_ready;
			public static Delegates.FdbFutureIsReady fdb_future_is_ready;
			public static Delegates.FdbFutureIsError fdb_future_is_error;
			public static Delegates.FdbFutureSetCallback fdb_future_set_callback;
			public static Delegates.FdbFutureGetError fdb_future_get_error;
			public static Delegates.FdbFutureGetVersion fdb_future_get_version;
			public static Delegates.FdbFutureGetKey fdb_future_get_key;
			public static Delegates.FdbFutureGetCluster fdb_future_get_cluster;
			public static Delegates.FdbFutureGetDatabase fdb_future_get_database;
			public static Delegates.FdbFutureGetValue fdb_future_get_value;
			public static Delegates.FdbFutureGetKeyValue fdb_future_get_keyvalue_array;

			public static void LoadBindings(UnmanagedLibrary lib)
			{
				lib.Bind(ref Stubs.fdb_get_error, "fdb_get_error");
				lib.Bind(ref Stubs.fdb_select_api_version_impl, "fdb_select_api_version_impl");
				lib.Bind(ref Stubs.fdb_get_max_api_version, "fdb_get_max_api_version");

				lib.Bind(ref Stubs.fdb_network_set_option, "fdb_network_set_option");
				lib.Bind(ref Stubs.fdb_setup_network, "fdb_setup_network");
				lib.Bind(ref Stubs.fdb_run_network, "fdb_run_network");
				lib.Bind(ref Stubs.fdb_stop_network, "fdb_stop_network");

				lib.Bind(ref Stubs.fdb_create_cluster, "fdb_create_cluster");
				lib.Bind(ref Stubs.fdb_cluster_destroy, "fdb_cluster_destroy");
				lib.Bind(ref Stubs.fdb_cluster_set_option, "fdb_cluster_set_option");
				lib.Bind(ref Stubs.fdb_cluster_create_database, "fdb_cluster_create_database");

				lib.Bind(ref Stubs.fdb_database_destroy, "fdb_database_destroy");
				lib.Bind(ref Stubs.fdb_database_set_option, "fdb_database_set_option");
				lib.Bind(ref Stubs.fdb_database_create_transaction, "fdb_database_create_transaction");

				lib.Bind(ref Stubs.fdb_transaction_destroy, "fdb_transaction_destroy");
				lib.Bind(ref Stubs.fdb_transaction_set_option, "fdb_transaction_set_option");
				lib.Bind(ref Stubs.fdb_transaction_set, "fdb_transaction_set");
				lib.Bind(ref Stubs.fdb_transaction_clear, "fdb_transaction_clear");
				lib.Bind(ref Stubs.fdb_transaction_clear_range, "fdb_transaction_clear_range");
				lib.Bind(ref Stubs.fdb_transaction_commit, "fdb_transaction_commit");
				lib.Bind(ref Stubs.fdb_transaction_set_read_version, "fdb_transaction_set_read_version");
				lib.Bind(ref Stubs.fdb_transaction_get_read_version, "fdb_transaction_get_read_version");
				lib.Bind(ref Stubs.fdb_transaction_get_committed_version, "fdb_transaction_get_committed_version");
				lib.Bind(ref Stubs.fdb_transaction_get, "fdb_transaction_get");
				lib.Bind(ref Stubs.fdb_transaction_get_key, "fdb_transaction_get_key");
				lib.Bind(ref Stubs.fdb_transaction_get_range, "fdb_transaction_get_range");
				lib.Bind(ref Stubs.fdb_transaction_on_error, "fdb_transaction_on_error");
				lib.Bind(ref Stubs.fdb_transaction_reset, "fdb_transaction_reset");

				lib.Bind(ref Stubs.fdb_future_destroy, "fdb_future_destroy");
				lib.Bind(ref Stubs.fdb_future_is_error, "fdb_future_is_error");
				lib.Bind(ref Stubs.fdb_future_is_ready, "fdb_future_is_ready");
				lib.Bind(ref Stubs.fdb_future_block_until_ready, "fdb_future_block_until_ready");
				lib.Bind(ref Stubs.fdb_future_get_error, "fdb_future_get_error");
				lib.Bind(ref Stubs.fdb_future_set_callback, "fdb_future_set_callback");
				lib.Bind(ref Stubs.fdb_future_get_cluster, "fdb_future_get_cluster");
				lib.Bind(ref Stubs.fdb_future_get_database, "fdb_future_get_database");
				lib.Bind(ref Stubs.fdb_future_get_key, "fdb_future_get_key");
				lib.Bind(ref Stubs.fdb_future_get_value, "fdb_future_get_value");
				lib.Bind(ref Stubs.fdb_future_get_keyvalue_array, "fdb_future_get_keyvalue_array");
				lib.Bind(ref Stubs.fdb_future_get_version, "fdb_future_get_version");

			}

		}

		static FdbNative()
		{
			try
			{
				FdbCLib = UnmanagedLibrary.LoadLibrary(
					Path.Combine(Fdb.NativeLibPath, DLL_X86),
					Path.Combine(Fdb.NativeLibPath, DLL_X64)
				);

				Stubs.LoadBindings(FdbCLib);

			}
			catch (Exception e)
			{
				if (FdbCLib != null) FdbCLib.Dispose();
				FdbCLib = null;
				LibraryLoadError = e;
			}
		}

		public static bool IsLoaded
		{
			get { return LibraryLoadError == null && FdbCLib != null; }
		}

		private static void EnsureLibraryIsLoaded()
		{
			// should be inlined
			if (LibraryLoadError != null || FdbCLib == null) FailLibraryDidNotLoad();
		}

		private static void FailLibraryDidNotLoad()
		{
			throw new InvalidOperationException("An error occured while loading native FoundationDB library", LibraryLoadError);
		}

		private static string ToManagedString(byte* nativeString)
		{
			if (nativeString == null) return null;
			return Marshal.PtrToStringAnsi(new IntPtr((void*)nativeString));
		}

		private static string ToManagedString(IntPtr nativeString)
		{
			if (nativeString == IntPtr.Zero) return null;
			return Marshal.PtrToStringAnsi(nativeString);
		}

		/// <summary>Converts a string into an ANSI byte array</summary>
		/// <param name="value">String to convert (or null)</param>
		/// <param name="nullTerminated">If true, adds a terminating \0 at the end (C-style strings)</param>
		/// <param name="length">Receives the size of the string including the optional NUL terminator (or 0 if <paramref name="value"/> is null)</param>
		/// <returns>Byte array with the ANSI-encoded string with an optional NUL terminator, or null if <paramref name="value"/> was null</returns>
		public static ArraySegment<byte> ToNativeString(string value, bool nullTerminated)
		{
			if (value == null) return Fdb.Nil;

			byte[] result;
			if (nullTerminated)
			{ // NULL terminated ANSI string
				result = new byte[value.Length + 1];
				Encoding.Default.GetBytes(value, 0, value.Length, result, 0);
			}
			else
			{
				result = Encoding.Default.GetBytes(value);
			}
			return new ArraySegment<byte>(result);
		}

		#region Core..

		/// <summary>fdb_get_error</summary>
		public static string GetError(FdbError code)
		{
			EnsureLibraryIsLoaded();
			return ToManagedString(Stubs.fdb_get_error(code));
		}

		/// <summary>fdb_select_api_impl</summary>
		public static FdbError SelectApiVersionImpl(int runtimeVersion, int headerVersion)
		{
			EnsureLibraryIsLoaded();
			return Stubs.fdb_select_api_version_impl(runtimeVersion, headerVersion);
		}

		/// <summary>fdb_select_api_impl</summary>
		public static FdbError SelectApiVersion(int version)
		{
			return SelectApiVersionImpl(version, FDB_API_VERSION);
		}

		/// <summary>fdb_get_max_api_version</summary>
		public static int GetMaxApiVersion()
		{
			EnsureLibraryIsLoaded();
			return Stubs.fdb_get_max_api_version();
		}

		#endregion

		#region Futures...

		public static bool FutureIsReady(FutureHandle futureHandle)
		{
			EnsureLibraryIsLoaded();
			return Stubs.fdb_future_is_ready(futureHandle);
		}

		public static void FutureDestroy(IntPtr futureHandle)
		{
			EnsureLibraryIsLoaded();
			Stubs.fdb_future_destroy(futureHandle);
		}

		public static bool FutureIsError(FutureHandle futureHandle)
		{
			EnsureLibraryIsLoaded();
			return Stubs.fdb_future_is_error(futureHandle);
		}

		/// <summary>Return the error got from a FDBFuture</summary>
		/// <param name="futureHandle"></param>
		/// <returns></returns>
		public static FdbError FutureGetError(FutureHandle future)
		{
			EnsureLibraryIsLoaded();
			return Stubs.fdb_future_get_error(future, null);
		}

		public static FdbError FutureGetError(FutureHandle future, out string description)
		{
			EnsureLibraryIsLoaded();

			byte* ptr = null;
			var err = Stubs.fdb_future_get_error(future, &ptr);
			description = ToManagedString(ptr);
			return err;
		}

		public static FdbError FutureBlockUntilReady(FutureHandle future)
		{
			EnsureLibraryIsLoaded();

#if DEBUG_NATIVE_CALLS
			Debug.WriteLine("calling fdb_future_block_until_ready(0x" + future.Handle.ToString("x") + ")...");
#endif
			var err = Stubs.fdb_future_block_until_ready(future);
#if DEBUG_NATIVE_CALLS
			Debug.WriteLine("fdb_future_block_until_ready(0x" + future.Handle.ToString("x") + ") => err=" + err);
#endif
			return err;
		}

		public static FdbError FutureSetCallback(FutureHandle future, FdbFutureCallback callback, IntPtr callbackParameter)
		{
			EnsureLibraryIsLoaded();
			var ptrCallback = Marshal.GetFunctionPointerForDelegate(callback);
			var err = Stubs.fdb_future_set_callback(future, ptrCallback, callbackParameter);
#if DEBUG_NATIVE_CALLS
			Debug.WriteLine("fdb_future_set_callback(0x" + future.Handle.ToString("x") + ", 0x" + ptrCallback.ToString("x") + ") => err=" + err);
#endif
			return err;
		}

		#endregion

		#region Network...

		public static FdbError NetworkSetOption(FdbNetworkOption option, byte* value, int valueLength)
		{
			EnsureLibraryIsLoaded();
			return Stubs.fdb_network_set_option(option, value, valueLength);
		}

		public static FdbError SetupNetwork()
		{
			return Stubs.fdb_setup_network();
		}

		public static FdbError RunNetwork()
		{
			return Stubs.fdb_run_network();
		}

		public static FdbError StopNetwork()
		{
			return Stubs.fdb_stop_network();
		}

		#endregion

		#region Clusters...

		public static FutureHandle CreateCluster(string path)
		{
			EnsureLibraryIsLoaded();

			var data = ToNativeString(path, nullTerminated: true);
			fixed (byte* ptr = data.Array)
			{
				var future = new FutureHandle();
				var handle = Stubs.fdb_create_cluster(ptr + data.Offset);
#if DEBUG_NATIVE_CALLS
				Debug.WriteLine("fdb_create_cluster(" + path + ") => 0x" + handle.ToString("x"));
#endif
				future.TrySetHandle(handle);
				return future;
			}
		}

		public static void ClusterDestroy(IntPtr handle)
		{
			EnsureLibraryIsLoaded();
			RuntimeHelpers.PrepareConstrainedRegions();
			try { }
			finally
			{
				Stubs.fdb_cluster_destroy(handle);
			}
		}

		public static FdbError ClusterSetOption(ClusterHandle cluster, FdbClusterOption option, byte* value, int valueLength)
		{
			EnsureLibraryIsLoaded();
			return Stubs.fdb_cluster_set_option(cluster, option, value, valueLength);
		}

		public static FdbError FutureGetCluster(FutureHandle future, out ClusterHandle cluster)
		{
			EnsureLibraryIsLoaded();
			cluster = new ClusterHandle();
			FdbError err;

			RuntimeHelpers.PrepareConstrainedRegions();
			try { }
			finally
			{
				IntPtr handle;
				err = Stubs.fdb_future_get_cluster(future, out handle);
#if DEBUG_NATIVE_CALLS
				Debug.WriteLine("fdb_future_get_cluster(0x" + future.Handle.ToString("x") + ") => err=" + err + ", handle=0x" + handle.ToString("x"));
#endif
				//TODO: check is err == Success ?
				cluster.TrySetHandle(handle);
			}
			return err;
		}

		#endregion

		#region Databases...

		public static FdbError FutureGetDatabase(FutureHandle future, out DatabaseHandle database)
		{
			EnsureLibraryIsLoaded();

			database = new DatabaseHandle();
			FdbError err;

			RuntimeHelpers.PrepareConstrainedRegions();
			try { }
			finally
			{
				IntPtr handle;
				err = Stubs.fdb_future_get_database(future.Handle, out handle);
				//TODO: check is err == Success ?
				database.TrySetHandle(handle);
			}
			return err;
		}

		public static FdbError DatabaseSetOption(DatabaseHandle database, FdbDatabaseOption option, byte* value, int valueLength)
		{
			EnsureLibraryIsLoaded();
			return Stubs.fdb_database_set_option(database, option, value, valueLength);
		}

		public static void DatabaseDestroy(IntPtr handle)
		{
			EnsureLibraryIsLoaded();

			RuntimeHelpers.PrepareConstrainedRegions();
			try { }
			finally
			{
				Stubs.fdb_database_destroy(handle);
			}
		}

		public static FutureHandle ClusterCreateDatabase(ClusterHandle cluster, string name)
		{
			EnsureLibraryIsLoaded();

			var data = ToNativeString(name, nullTerminated: false);
			fixed (byte* ptr = data.Array)
			{
				var future = new FutureHandle();

				RuntimeHelpers.PrepareConstrainedRegions();
				try { }
				finally
				{
					var handle = Stubs.fdb_cluster_create_database(cluster, ptr + data.Offset, data.Count);
#if DEBUG_NATIVE_CALLS
					Debug.WriteLine("fdb_cluster_create_database(0x" + cluster.Handle.ToString("x") + ", name: '" + name + "') => 0x" + handle.ToString("x"));
#endif
					future.TrySetHandle(handle);
				}
				return future;
			}
		}

		#endregion

		#region Transactions...

		public static void TransactionDestroy(IntPtr handle)
		{
			EnsureLibraryIsLoaded();

			RuntimeHelpers.PrepareConstrainedRegions();
			try { }
			finally
			{
				Stubs.fdb_transaction_destroy(handle);
			}
		}

		public static FdbError TransactionSetOption(TransactionHandle transaction, FdbTransactionOption option, byte* value, int valueLength)
		{
			EnsureLibraryIsLoaded();
			return Stubs.fdb_transaction_set_option(transaction, option, value, valueLength);
		}

		public static FdbError DatabaseCreateTransaction(DatabaseHandle database, out TransactionHandle transaction)
		{
			EnsureLibraryIsLoaded();
			transaction = new TransactionHandle();
			FdbError err;

			RuntimeHelpers.PrepareConstrainedRegions();
			try { }
			finally
			{
				IntPtr handle;
				err = Stubs.fdb_database_create_transaction(database, out handle);
#if DEBUG_NATIVE_CALLS
				Debug.WriteLine("fdb_database_create_transaction(0x" + database.Handle.ToString("x") + ") => err=" + err + ", handle=0x" + handle.ToString("x"));
#endif
				transaction.TrySetHandle(handle);
			}
			return err;

		}

		public static FutureHandle TransactionCommit(TransactionHandle transaction)
		{
			EnsureLibraryIsLoaded();
			var future = new FutureHandle();

			RuntimeHelpers.PrepareConstrainedRegions();
			try { }
			finally
			{
				var handle = Stubs.fdb_transaction_commit(transaction);
#if DEBUG_NATIVE_CALLS
				Debug.WriteLine("fdb_transaction_commit(0x" + transaction.Handle.ToString("x") + ") => 0x" + handle.ToString("x"));
#endif
				future.TrySetHandle(handle);
			}
			return future;
		}

		public static void TransactionReset(TransactionHandle transaction)
		{
			EnsureLibraryIsLoaded();

#if DEBUG_NATIVE_CALLS
			Debug.WriteLine("fdb_transaction_reset(0x" + transaction.Handle.ToString("x") + ")");
#endif
			Stubs.fdb_transaction_reset(transaction);
		}

		public static void TransactionSetReadVersion(TransactionHandle transaction, long version)
		{
			EnsureLibraryIsLoaded();

#if DEBUG_NATIVE_CALLS
			Debug.WriteLine("fdb_transaction_set_read_version(0x" + transaction.Handle.ToString("x") + ", version: " + version.ToString() + ")");
#endif
			Stubs.fdb_transaction_set_read_version(transaction, version);
		}

		public static FutureHandle TransactionGetReadVersion(TransactionHandle transaction)
		{
			EnsureLibraryIsLoaded();
			var future = new FutureHandle();

			RuntimeHelpers.PrepareConstrainedRegions();
			try { }
			finally
			{
				var handle = Stubs.fdb_transaction_get_read_version(transaction);
#if DEBUG_NATIVE_CALLS
				Debug.WriteLine("fdb_transaction_get_read_version(0x" + transaction.Handle.ToString("x") + ") => 0x" + handle.ToString("x"));
#endif
				future.TrySetHandle(handle);
			}
			return future;
		}

		public static FdbError TransactionGetCommittedVersion(TransactionHandle transaction, out long version)
		{
			EnsureLibraryIsLoaded();

#if DEBUG_NATIVE_CALLS
			Debug.WriteLine("fdb_transaction_get_committed_version(0x" + transaction.Handle.ToString("x") + ")");
#endif
			return Stubs.fdb_transaction_get_committed_version(transaction, out version);
		}

		public static FdbError FutureGetVersion(FutureHandle future, out long version)
		{
			EnsureLibraryIsLoaded();

			return Stubs.fdb_future_get_version(future, out version);
		}

		public static FutureHandle TransactionGet(TransactionHandle transaction, ArraySegment<byte> key, bool snapshot)
		{
			EnsureLibraryIsLoaded();
			if (key.Array == null || key.Count == 0) throw new ArgumentException("Key cannot be empty", "key");

			var future = new FutureHandle();

			RuntimeHelpers.PrepareConstrainedRegions();
			try { }
			finally
			{
				fixed (byte* ptrKey = key.Array)
				{
					var handle = Stubs.fdb_transaction_get(transaction, ptrKey + key.Offset, key.Count, snapshot);
#if DEBUG_NATIVE_CALLS
					Debug.WriteLine("fdb_transaction_get(0x" + transaction.Handle.ToString("x") + ", key: '" + FdbKey.Dump(key) + "', snapshot: " + snapshot + ") => 0x" + handle.ToString("x"));
#endif
					future.TrySetHandle(handle);
				}
			}
			return future;
		}

		public static FutureHandle TransactionGetRange(TransactionHandle transaction, FdbKeySelector begin, FdbKeySelector end, int limit, int targetBytes, FdbStreamingMode mode, int iteration, bool snapshot, bool reverse)
		{
			EnsureLibraryIsLoaded();

			var future = new FutureHandle();

			RuntimeHelpers.PrepareConstrainedRegions();
			try { }
			finally
			{
				fixed (byte* ptrBegin = begin.Key.Array)
				fixed (byte* ptrEnd = end.Key.Array)
				{
					var handle = Stubs.fdb_transaction_get_range(
						transaction,
						ptrBegin + begin.Key.Offset, begin.Key.Count, begin.OrEqual, begin.Offset,
						ptrEnd + end.Key.Offset, end.Key.Count, end.OrEqual, end.Offset,
						limit, targetBytes, mode, iteration, snapshot, reverse);
#if DEBUG_NATIVE_CALLS
					Debug.WriteLine("fdb_transaction_get_range(0x" + transaction.Handle.ToString("x") + ", begin: {'" + FdbKey.Dump(begin.Key) + "'," + begin.OrEqual + "," + begin.Offset + "}, end: {'" + FdbKey.Dump(end.Key) + "'," + end.OrEqual + "," + end.Offset + "}, " + snapshot + ") => 0x" + handle.ToString("x"));
#endif
					future.TrySetHandle(handle);
				}
			}
			return future;
		}

		public static FutureHandle TransactionGetKey(TransactionHandle transaction, FdbKeySelector selector, bool snapshot)
		{
			EnsureLibraryIsLoaded();
			if (selector.Key.Array == null || selector.Key.Count == 0) throw new ArgumentException("Key cannot be empty", "selector");

			var future = new FutureHandle();

			RuntimeHelpers.PrepareConstrainedRegions();
			try { }
			finally
			{
				fixed (byte* ptrKey = selector.Key.Array)
				{
					var handle = Stubs.fdb_transaction_get_key(transaction, ptrKey + selector.Key.Offset, selector.Key.Count, selector.OrEqual, selector.Offset, snapshot);
#if DEBUG_NATIVE_CALLS
					Debug.WriteLine("fdb_transaction_get_key(0x" + transaction.Handle.ToString("x") + ", {'" + FdbKey.Dump(selector.Key) + "'," + selector.OrEqual + "," + selector.Offset + "}, " + snapshot + ") => 0x" + handle.ToString("x"));
#endif
					future.TrySetHandle(handle);
				}
			}
			return future;
		}

		public static FdbError FutureGetValue(FutureHandle future, out bool valuePresent, out ArraySegment<byte> value)
		{
			EnsureLibraryIsLoaded();

			byte* ptr = null;
			int valueLength = 0;
			var err = Stubs.fdb_future_get_value(future, out valuePresent, out ptr, out valueLength);
#if DEBUG_NATIVE_CALLS
			Debug.WriteLine("fdb_future_get_value(0x" + future.Handle.ToString("x") + ") => err=" + err + ", present=" + valuePresent + ", valueLength=" + valueLength);
#endif
			if (ptr != null && valueLength >= 0)
			{
				var bytes = new byte[valueLength];
				Marshal.Copy(new IntPtr(ptr), bytes, 0, valueLength);
				value = new ArraySegment<byte>(bytes, 0, valueLength);
			}
			else
			{
				value = default(ArraySegment<byte>);
			}
			return err;
		}

		public static FdbError FutureGetKey(FutureHandle future, out ArraySegment<byte> key)
		{
			EnsureLibraryIsLoaded();

			byte* ptr = null;
			int keyLength = 0;
			var err = Stubs.fdb_future_get_key(future, out ptr, out keyLength);
#if DEBUG_NATIVE_CALLS
			Debug.WriteLine("fdb_future_get_key(0x" + future.Handle.ToString("x") + ") => err=" + err + ", keyLength=" + keyLength);
#endif
			if (ptr != null && keyLength >= 0)
			{
				var bytes = new byte[keyLength];
				Marshal.Copy(new IntPtr(ptr), bytes, 0, keyLength);
				key = new ArraySegment<byte>(bytes, 0, keyLength);
			}
			else
			{
				key = default(ArraySegment<byte>);
			}

			return err;
		}

		public static FdbError FutureGetKeyValueArray(FutureHandle future, out KeyValuePair<ArraySegment<byte>, ArraySegment<byte>>[] result, out bool more)
		{
			result = null;
			more = false;

			int count;
			FdbKeyValue* kvp;

			var err = Stubs.fdb_future_get_keyvalue_array(future, out kvp, out count, out more);
#if DEBUG_NATIVE_CALLS
			Debug.WriteLine("fdb_future_get_keyvalue_array(0x" + future.Handle.ToString("x") + ") => err=" + err + ", count=" + count + ", more=" + more);
#endif

			if (Fdb.Success(err))
			{
				Debug.Assert(count >= 0, "Return count was negative");

				result = new KeyValuePair<ArraySegment<byte>, ArraySegment<byte>>[count];

				if (count > 0)
				{ // convert the keyvalue result into an array

					Debug.Assert(kvp != null, "We have results but array pointer was null");

					// in order to reduce allocations, we want to merge all keys and values
					// into a single byte{] and return  list of ArraySegment<byte> that will
					// link to the different chunks of this buffer.

					// first pass to compute the total size needed
					int total = 0;
					for (int i = 0; i < count; i++)
					{
						//TODO: protect against negative values or values too big ?
						Debug.Assert(kvp[i].KeyLength >= 0 && kvp[i].KeyLength >= 0);
						total += kvp[i].KeyLength + kvp[i].ValueLength;
					}

					// allocate all memory in one chunk, and make the key/values point to it
					// Does fdb allocate all keys into a single buffer ? We could copy everything in one pass,
					// but it would rely on implementation details that could break at anytime...

					//TODO: protect against too much memory allocated ?
					// what would be a good max value? we need to at least be able to handle FDB_STREAMING_MODE_WANT_ALL

					var page = new byte[total];
					int p = 0;
					for (int i = 0; i < result.Length; i++)
					{
						int kl = kvp[i].KeyLength;
						int vl = kvp[i].ValueLength;

						//TODO: some keys/values will be small (32 bytes or less) while other will be big
						//consider having to copy methods, optimized for each scenario ?

						Marshal.Copy(kvp[i].Key, page, p, kl);
						Marshal.Copy(kvp[i].Value, page, p + kl, vl);

						result[i] = new KeyValuePair<ArraySegment<byte>, ArraySegment<byte>>(
							new ArraySegment<byte>(page, p, kl),
							new ArraySegment<byte>(page, p + kl, vl)
						);

						p += kl + vl;
					}
					Debug.Assert(p == total);
				}
			}

			return err;
		}

		public static void TransactionSet(TransactionHandle transaction, ArraySegment<byte> key, ArraySegment<byte> value)
		{
			EnsureLibraryIsLoaded();

			fixed (byte* pKey = key.Array)
			fixed (byte* pValue = value.Array)
			{
#if DEBUG_NATIVE_CALLS
				Debug.WriteLine("fdb_transaction_set(0x" + transaction.Handle.ToString("x") + ", key: '" + FdbKey.Dump(key) + "', value: '" + FdbKey.Dump(value) + "')");
#endif
				Stubs.fdb_transaction_set(transaction, pKey + key.Offset, key.Count, pValue + value.Offset, value.Count);
			}
		}

		public static void TransactionClear(TransactionHandle transaction, ArraySegment<byte> key)
		{
			EnsureLibraryIsLoaded();

			fixed (byte* pKey = key.Array)
			{
#if DEBUG_NATIVE_CALLS
				Debug.WriteLine("fdb_transaction_clear(0x" + transaction.Handle.ToString("x") + ", key: '" + FdbKey.Dump(key) + "')");
#endif
				Stubs.fdb_transaction_clear(transaction, pKey + key.Offset, key.Count);
			}
		}

		public static void TransactionClearRange(TransactionHandle transaction, ArraySegment<byte> beginKey, ArraySegment<byte> endKey)
		{
			EnsureLibraryIsLoaded();

			fixed (byte* pBeginKey = beginKey.Array)
			fixed (byte* pEndKey = endKey.Array)
			{
#if DEBUG_NATIVE_CALLS
				Debug.WriteLine("fdb_transaction_clear_range(0x" + transaction.Handle.ToString("x") + ", beginKey: '" + FdbKey.Dump(beginKey) + ", endKey: '" + FdbKey.Dump(endKey) + "')");
#endif
				Stubs.fdb_transaction_clear_range(transaction, pBeginKey + beginKey.Offset, beginKey.Count, pEndKey + endKey.Offset, endKey.Count);
			}
		}

		#endregion

	}

}
