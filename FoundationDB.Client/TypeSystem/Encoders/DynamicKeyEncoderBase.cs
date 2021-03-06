using System;
using FoundationDB.Layers.Tuples;

namespace FoundationDB.Client
{
	public abstract class DynamicKeyEncoderBase : IDynamicKeyEncoder
	{

		public abstract IFdbKeyEncoding Encoding { get; }

		public virtual KeyRange ToRange(Slice prefix)
		{
			return KeyRange.StartsWith(prefix);
		}

		public abstract void PackKey(ref SliceWriter writer, ITuple items);

		public virtual void EncodeKey<T1>(ref SliceWriter writer, T1 item1)
		{
			PackKey(ref writer, STuple.Create(item1));
		}

		public virtual void EncodeKey<T1, T2>(ref SliceWriter writer, T1 item1, T2 item2)
		{
			PackKey(ref writer, STuple.Create(item1, item2));
		}

		public virtual void EncodeKey<T1, T2, T3>(ref SliceWriter writer, T1 item1, T2 item2, T3 item3)
		{
			PackKey(ref writer, STuple.Create(item1, item2, item3));
		}

		public virtual void EncodeKey<T1, T2, T3, T4>(ref SliceWriter writer, T1 item1, T2 item2, T3 item3, T4 item4)
		{
			PackKey(ref writer, STuple.Create(item1, item2, item3, item4));
		}

		public virtual void EncodeKey<T1, T2, T3, T4, T5>(ref SliceWriter writer, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5)
		{
			PackKey(ref writer, STuple.Create(item1, item2, item3, item4, item5));
		}

		public virtual void EncodeKey<T1, T2, T3, T4, T5, T6>(ref SliceWriter writer, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6)
		{
			PackKey(ref writer, STuple.Create(item1, item2, item3, item4, item5, item6));
		}

		public virtual void EncodeKey<T1, T2, T3, T4, T5, T6, T7>(ref SliceWriter writer, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7)
		{
			PackKey(ref writer, STuple.Create(item1, item2, item3, item4, item5, item6, item7));
		}

		public virtual void EncodeKey<T1, T2, T3, T4, T5, T6, T7, T8>(ref SliceWriter writer, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8)
		{
			PackKey(ref writer, STuple.Create(item1, item2, item3, item4, item5, item6, item7, item8));
		}

		public abstract ITuple UnpackKey(Slice packed);

		public virtual T DecodeKey<T>(Slice packed)
		{
			return UnpackKey(packed).OfSize(1).Get<T>(0);
		}

		public virtual T DecodeKeyFirst<T>(Slice packed)
		{
			return UnpackKey(packed).OfSizeAtLeast(1).Get<T>(0);
		}

		public virtual T DecodeKeyLast<T>(Slice packed)
		{
			return UnpackKey(packed).OfSizeAtLeast(1).Get<T>(-1);
		}

		public virtual STuple<T1, T2> DecodeKey<T1, T2>(Slice packed)
		{
			return UnpackKey(packed).With((T1 a, T2 b) => STuple.Create(a, b));
		}

		public virtual STuple<T1, T2, T3> DecodeKey<T1, T2, T3>(Slice packed)
		{
			return UnpackKey(packed).With((T1 a, T2 b, T3 c) => STuple.Create(a, b, c));
		}

		public virtual STuple<T1, T2, T3, T4> DecodeKey<T1, T2, T3, T4>(Slice packed)
		{
			return UnpackKey(packed).With((T1 a, T2 b, T3 c, T4 d) => STuple.Create(a, b, c, d));
		}

		public virtual STuple<T1, T2, T3, T4, T5> DecodeKey<T1, T2, T3, T4, T5>(Slice packed)
		{
			return UnpackKey(packed).With((T1 a, T2 b, T3 c, T4 d, T5 e) => STuple.Create(a, b, c, d, e));
		}

		public virtual KeyRange ToRange(Slice prefix, ITuple items)
		{
			var writer = new SliceWriter(prefix, 16);
			PackKey(ref writer, items);
			return ToRange(writer.ToSlice());
		}

		public virtual KeyRange ToKeyRange<T1>(Slice prefix, T1 item1)
		{
			return ToRange(prefix, STuple.Create(item1));
		}

		public virtual KeyRange ToKeyRange<T1, T2>(Slice prefix, T1 item1, T2 item2)
		{
			return ToRange(prefix, STuple.Create(item1, item2));
		}

		public virtual KeyRange ToKeyRange<T1, T2, T3>(Slice prefix, T1 item1, T2 item2, T3 item3)
		{
			return ToRange(prefix, STuple.Create(item1, item3, item3));
		}

		public virtual KeyRange ToKeyRange<T1, T2, T3, T4>(Slice prefix, T1 item1, T2 item2, T3 item3, T4 item4)
		{
			return ToRange(prefix, STuple.Create(item1, item3, item3, item4));
		}

		public virtual KeyRange ToKeyRange<T1, T2, T3, T4, T5>(Slice prefix, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5)
		{
			return ToRange(prefix, STuple.Create(item1, item3, item3, item4, item5));
		}

		public virtual KeyRange ToKeyRange<T1, T2, T3, T4, T5, T6>(Slice prefix, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6)
		{
			return ToRange(prefix, STuple.Create(item1, item3, item3, item4, item5, item6));
		}

		public virtual KeyRange ToKeyRange<T1, T2, T3, T4, T5, T6, T7>(Slice prefix, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7)
		{
			return ToRange(prefix, STuple.Create(item1, item3, item3, item4, item5, item6, item7));
		}

		public virtual KeyRange ToKeyRange<T1, T2, T3, T4, T5, T6, T7, T8>(Slice prefix, T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8)
		{
			return ToRange(prefix, STuple.Create(item1, item3, item3, item4, item5, item6, item7, item8));
		}
	}
}