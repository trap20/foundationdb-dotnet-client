﻿#region BSD Licence
/* Copyright (c) 2013-2018, Doxense SAS
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

namespace FoundationDB.Client.Tests
{
	using FoundationDB.Client;
	using NUnit.Framework;
	using System;
	using System.Text;

	[TestFixture]
	public class SliceComparerFacts : FdbTest
	{

		#region SliceComparer...

		[Test]
		public void Test_SliceComparer_Equals()
		{
			var cmp = SliceComparer.Default;
			Assert.That(cmp, Is.Not.Null);
			Assert.That(SliceComparer.Default, Is.SameAs(cmp));

			Assert.That(cmp.Equals(Slice.Nil, Slice.Nil), Is.True);
			Assert.That(cmp.Equals(Slice.Empty, Slice.Empty), Is.True);
			Assert.That(cmp.Equals(Slice.Nil, Slice.Empty), Is.False);
			Assert.That(cmp.Equals(Slice.Empty, Slice.Nil), Is.False);

			Assert.That(cmp.Equals(Slice.FromByte(42), Slice.FromByte(42)), Is.True);
			Assert.That(cmp.Equals(Slice.FromByte(42), Slice.Create(new byte[] { 42 })), Is.True);
			Assert.That(cmp.Equals(Slice.FromByte(42), Slice.FromByte(77)), Is.False);

			Assert.That(cmp.Equals(Slice.Create(new byte[] { 65, 66, 67 }), Slice.FromString("ABC")), Is.True);
			Assert.That(cmp.Equals(Slice.Create(new byte[] { 65, 66, 67, 68 }), Slice.FromString("ABC")), Is.False);

			var buf1 = Encoding.ASCII.GetBytes("ABBAABA");
			var buf2 = Encoding.ASCII.GetBytes("ABBAABA");
			Assert.That(cmp.Equals(Slice.Create(buf1, 0, 2), Slice.Create(buf1, 0, 2)), Is.True);
			Assert.That(cmp.Equals(Slice.Create(buf1, 0, 2), Slice.Create(buf1, 0, 3)), Is.False);
			Assert.That(cmp.Equals(Slice.Create(buf1, 0, 2), Slice.Create(buf1, 4, 2)), Is.True);
			Assert.That(cmp.Equals(Slice.Create(buf1, 0, 3), Slice.Create(buf1, 4, 3)), Is.False);
			Assert.That(cmp.Equals(Slice.Create(buf1, 0, 2), Slice.Create(buf2, 4, 2)), Is.True);
			Assert.That(cmp.Equals(Slice.Create(buf1, 0, 3), Slice.Create(buf2, 4, 3)), Is.False);
		}

		[Test]
		public void Test_SliceComparer_GetHashCode_Should_Return_Same_As_Slice()
		{
			var cmp = SliceComparer.Default;
			Assert.That(cmp, Is.Not.Null);

			Assert.That(cmp.GetHashCode(Slice.Nil), Is.EqualTo(Slice.Nil.GetHashCode()));
			Assert.That(cmp.GetHashCode(Slice.Empty), Is.EqualTo(Slice.Empty.GetHashCode()));
			Assert.That(cmp.GetHashCode(Slice.Nil), Is.Not.EqualTo(Slice.Empty));

			var rnd = new Random(123456);
			for (int i = 0; i < 100; i++)
			{
				var s = Slice.Random(rnd, rnd.Next(1, 16));
				Assert.That(cmp.GetHashCode(s), Is.EqualTo(s.GetHashCode()));
			}
		}

		[Test]
		public void Test_SliceComparer_Compare()
		{
			var cmp = SliceComparer.Default;
			Assert.That(cmp, Is.Not.Null);

			Assert.That(cmp.Compare(Slice.Nil, Slice.Nil), Is.EqualTo(0));
			Assert.That(cmp.Compare(Slice.Empty, Slice.Empty), Is.EqualTo(0));
			Assert.That(cmp.Compare(Slice.FromByte(42), Slice.FromByte(42)), Is.EqualTo(0));

			//REVIEW: Inconsistency: compare(nil, empty) == 0, but Equals(nil, empty) == false
			Assert.That(cmp.Compare(Slice.Nil, Slice.Empty), Is.EqualTo(0), "Nil and Empty are considered similar regarding ordering");
			Assert.That(cmp.Compare(Slice.Empty, Slice.Nil), Is.EqualTo(0), "Nil and Empty are considered similar regarding ordering");

			Assert.That(cmp.Compare(Slice.FromByte(42), Slice.FromByte(77)), Is.LessThan(0));
			Assert.That(cmp.Compare(Slice.FromByte(42), Slice.FromByte(21)), Is.GreaterThan(0));
			Assert.That(cmp.Compare(Slice.FromByte(42), Slice.Empty), Is.GreaterThan(0));
			Assert.That(cmp.Compare(Slice.FromByte(42), Slice.Nil), Is.GreaterThan(0));

			Assert.That(cmp.Compare(Slice.FromString("hello"), Slice.FromString("world")), Is.LessThan(0));
			Assert.That(cmp.Compare(Slice.FromString("world"), Slice.FromString("hello")), Is.GreaterThan(0));

			Assert.That(cmp.Compare(Slice.FromString("hell"), Slice.FromString("hello")), Is.LessThan(0));
			Assert.That(cmp.Compare(Slice.FromString("help"), Slice.FromString("hello")), Is.GreaterThan(0));
		}

		#endregion

	}
}
