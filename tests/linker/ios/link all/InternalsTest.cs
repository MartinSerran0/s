//
// Link All Unit Tests for __Internal native functions
//
// Authors:
//	Sebastien Pouliot  <sebastien@xamarin.com>
//
// Copyright 2013 Xamarin Inc. All rights reserved.
//

using System;
using System.Runtime.InteropServices;

using MonoTouch;
using Foundation;
using ObjCRuntime;
using NUnit.Framework;

namespace LinkAll.InernalCalls {

	[TestFixture]
	// we want the tests to be available because we use the linker
	[Preserve (AllMembers = true)]
	public class InternalsTest {

		[DllImport ("__Internal")]
		extern static string xamarin_get_locale_country_code ();

		[Test]
		public void RegionInfo_CountryCode ()
		{
			Assert.IsNotNull (xamarin_get_locale_country_code (), "xamarin_get_locale_country_code");
		}

		[DllImport ("__Internal", CharSet = CharSet.Unicode)]
		extern static void xamarin_log (string s);

		[Test]
		public void Console_Log ()
		{
			xamarin_log ("ascii");
			xamarin_log ("ЉЩщӃ");
		}

		[DllImport ("__Internal")]
		extern static IntPtr xamarin_timezone_get_names (ref int count);

		[Test]
		public void TimeZone_Names ()
		{
			int count = 0;
			IntPtr array = xamarin_timezone_get_names (ref count);
			Assert.That (count, Is.GreaterThan (400), "count");
			for (int i = 0, offset = 0; i < count; i++, offset += IntPtr.Size) {
				IntPtr p = Marshal.ReadIntPtr (array, offset);
				string s = Marshal.PtrToStringAnsi (p);
				Assert.NotNull (s, i.ToString ());
				Marshal.FreeHGlobal (p);
			}
			Marshal.FreeHGlobal (array);
		}

		[DllImport ("__Internal")]
		extern static IntPtr xamarin_timezone_get_data (string name, ref uint size);

		[Test]
		public void TimeZone_Data ()
		{
			uint size = 0;
			IntPtr data = xamarin_timezone_get_data (null, ref size);
			Assert.That (data, Is.Not.EqualTo (IntPtr.Zero), "default");
			Assert.That (size, Is.GreaterThan (0), "default size");
			Marshal.FreeHGlobal (data);

			data = xamarin_timezone_get_data ("America/Montreal", ref size);
			Assert.That (data, Is.Not.EqualTo (IntPtr.Zero), "Montreal");
			Assert.That (size, Is.GreaterThan (0), "Montreal size");
			Marshal.FreeHGlobal (data);
		}
	}
}
