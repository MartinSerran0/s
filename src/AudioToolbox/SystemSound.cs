// 
// SystemSound.cs: AudioServices system sound
//
// Authors: Mono Team
//          Marek Safar (marek.safar@gmail.com)
//     
// Copyright 2009 Novell, Inc
// Copyright 2012 Xamarin Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

#nullable enable

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Foundation;
using CoreFoundation;
using ObjCRuntime;

namespace AudioToolbox {

	enum SystemSoundId : uint { // UInt32 SystemSoundID
		Vibrate = 0x00000FFF,
	}

#if NET
	[SupportedOSPlatform ("ios")]
	[SupportedOSPlatform ("maccatalyst")]
	[SupportedOSPlatform ("macos")]
	[SupportedOSPlatform ("tvos")]
	public class SystemSound : IDisposable {
#else
	public class SystemSound : INativeObject, IDisposable {
#endif
#if MONOMAC
		// TODO:
#else
		public static readonly SystemSound Vibrate = new SystemSound ((uint) SystemSoundId.Vibrate, false);
#endif

		uint soundId;
		bool ownsHandle;

		Action? completionRoutine;
		GCHandle gc_handle;

#if !NET
		static readonly AddSystemSoundCompletionCallback SoundCompletionCallback = SoundCompletionShared;
#endif

		internal SystemSound (uint soundId, bool ownsHandle)
		{
			this.soundId = soundId;
			this.ownsHandle = ownsHandle;
		}

		public SystemSound (uint soundId) : this (soundId, false) { }


		~SystemSound ()
		{
			Dispose (false);
		}

#if NET
		public uint SoundId {
			get {
				AssertNotDisposed ();
				return soundId;
			}
		}
#else
		public IntPtr Handle {
			get {
				AssertNotDisposed ();
				return (IntPtr) soundId;
			}
		}
#endif

		public bool IsUISound {
			get {
				uint out_size = sizeof (uint);
				uint data;

				var res = AudioServices.AudioServicesGetProperty (AudioServicesPropertyKey.IsUISound, sizeof (AudioServicesPropertyKey), ref soundId, out out_size, out data);
				if (res != AudioServicesError.None)
					throw new ArgumentException (res.ToString ());

				return data == 1;
			}

			set {
				uint data = value ? (uint) 1 : 0;

				var res = AudioServices.AudioServicesSetProperty (AudioServicesPropertyKey.IsUISound, sizeof (AudioServicesPropertyKey), ref soundId, sizeof (uint), ref data);
				if (res != AudioServicesError.None)
					throw new ArgumentException (res.ToString ());
			}
		}

		public bool CompletePlaybackIfAppDies {
			get {
				uint out_size = sizeof (uint);
				uint data;

				var res = AudioServices.AudioServicesGetProperty (AudioServicesPropertyKey.CompletePlaybackIfAppDies, sizeof (AudioServicesPropertyKey), ref soundId, out out_size, out data);
				if (res != AudioServicesError.None)
					throw new ArgumentException (res.ToString ());

				return data == 1;
			}

			set {
				uint data = value ? (uint) 1 : 0;

				var res = AudioServices.AudioServicesSetProperty (AudioServicesPropertyKey.CompletePlaybackIfAppDies, sizeof (AudioServicesPropertyKey), ref soundId, sizeof (uint), ref data);
				if (res != AudioServicesError.None)
					throw new ArgumentException (res.ToString ());
			}
		}

		void AssertNotDisposed ()
		{
			if (soundId == 0)
				throw new ObjectDisposedException ("SystemSound");
		}

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		protected virtual void Dispose (bool disposing)
		{
			Cleanup (false);
		}

		[DllImport (Constants.AudioToolboxLibrary)]
		static extern AudioServicesError AudioServicesDisposeSystemSoundID (uint soundId);

		void Cleanup (bool checkForError)
		{
			if (soundId == 0 || !ownsHandle)
				return;

			if (gc_handle.IsAllocated) {
				gc_handle.Free ();
			}

			if (completionRoutine is not null) {
				RemoveSystemSoundCompletion ();
			}

			var error = AudioServicesDisposeSystemSoundID (soundId);
			var oldId = soundId;
			soundId = 0;
			if (checkForError && error != AudioServicesError.None) {
				throw new InvalidOperationException (string.Format ("Error while disposing SystemSound with ID {0}: {1}",
							oldId, error.ToString ()));
			}
		}

		public void Close ()
		{
			Cleanup (true);
		}

		[DllImport (Constants.AudioToolboxLibrary)]
		static extern void AudioServicesPlayAlertSound (uint inSystemSoundID);
		public void PlayAlertSound ()
		{
			AssertNotDisposed ();
			AudioServicesPlayAlertSound (soundId);
		}

		[DllImport (Constants.AudioToolboxLibrary)]
		static extern void AudioServicesPlaySystemSound (uint inSystemSoundID);
		public void PlaySystemSound ()
		{
			AssertNotDisposed ();
			AudioServicesPlaySystemSound (soundId);
		}

		delegate void TrampolineCallback (IntPtr blockPtr);

		static unsafe readonly TrampolineCallback static_action = TrampolineAction;

		[MonoPInvokeCallback (typeof (TrampolineCallback))]
		static void TrampolineAction (IntPtr blockPtr)
		{
			var del = BlockLiteral.GetTarget<Action> (blockPtr);
			if (del is not null)
				del ();
		}

#if NET
		[SupportedOSPlatform ("ios9.0")]
		[SupportedOSPlatform ("macos10.11")]
		[SupportedOSPlatform ("maccatalyst")]
		[SupportedOSPlatform ("tvos")]
#else
		[iOS (9, 0)]
		[Mac (10, 11)]
#endif
		[BindingImpl (BindingImplOptions.Optimizable)]
		public void PlayAlertSound (Action onCompletion)
		{
			if (onCompletion is null)
				ObjCRuntime.ThrowHelper.ThrowArgumentNullException (nameof (onCompletion));

			AssertNotDisposed ();

			var block_handler = new BlockLiteral ();
			block_handler.SetupBlockUnsafe (static_action, onCompletion);
			try {
				AudioServicesPlayAlertSoundWithCompletion (soundId, ref block_handler);
			} finally {
				block_handler.CleanupBlock ();
			}
		}

#if NET
		[SupportedOSPlatform ("ios9.0")]
		[SupportedOSPlatform ("macos10.11")]
		[SupportedOSPlatform ("maccatalyst")]
		[SupportedOSPlatform ("tvos")]
#else
		[iOS (9, 0)]
		[Mac (10, 11)]
#endif
		public Task PlayAlertSoundAsync ()
		{
			var tcs = new TaskCompletionSource<bool> ();
			PlayAlertSound (() => {
				tcs.SetResult (true);
			});
			return tcs.Task;
		}

#if NET
		[SupportedOSPlatform ("ios9.0")]
		[SupportedOSPlatform ("macos10.11")]
		[SupportedOSPlatform ("maccatalyst")]
		[SupportedOSPlatform ("tvos")]
#else
		[iOS (9, 0)]
		[Mac (10, 11)]
#endif
		[BindingImpl (BindingImplOptions.Optimizable)]
		public void PlaySystemSound (Action onCompletion)
		{
			if (onCompletion is null)
				ObjCRuntime.ThrowHelper.ThrowArgumentNullException (nameof (onCompletion));

			AssertNotDisposed ();

			var block_handler = new BlockLiteral ();
			block_handler.SetupBlockUnsafe (static_action, onCompletion);
			try {
				AudioServicesPlaySystemSoundWithCompletion (soundId, ref block_handler);
			} finally {
				block_handler.CleanupBlock ();
			}
		}

#if NET
		[SupportedOSPlatform ("ios9.0")]
		[SupportedOSPlatform ("macos10.11")]
		[SupportedOSPlatform ("maccatalyst")]
		[SupportedOSPlatform ("tvos")]
#else
		[iOS (9, 0)]
		[Mac (10, 11)]
#endif
		public Task PlaySystemSoundAsync ()
		{
			var tcs = new TaskCompletionSource<bool> ();
			PlaySystemSound (() => {
				tcs.SetResult (true);
			});
			return tcs.Task;
		}

#if NET
		[SupportedOSPlatform ("ios9.0")]
		[SupportedOSPlatform ("macos10.11")]
		[SupportedOSPlatform ("maccatalyst")]
		[SupportedOSPlatform ("tvos")]
#else
		[iOS (9, 0)]
		[Mac (10, 11)]
#endif
		[DllImport (Constants.AudioToolboxLibrary)]
		static extern void AudioServicesPlayAlertSoundWithCompletion (uint inSystemSoundID, ref BlockLiteral inCompletionBlock);

#if NET
		[SupportedOSPlatform ("ios9.0")]
		[SupportedOSPlatform ("macos10.11")]
		[SupportedOSPlatform ("maccatalyst")]
		[SupportedOSPlatform ("tvos")]
#else
		[iOS (9, 0)]
		[Mac (10, 11)]
#endif
		[DllImport (Constants.AudioToolboxLibrary)]
		static extern void AudioServicesPlaySystemSoundWithCompletion (uint inSystemSoundID, ref BlockLiteral inCompletionBlock);

		[DllImport (Constants.AudioToolboxLibrary)]
		static extern AudioServicesError AudioServicesCreateSystemSoundID (IntPtr fileUrl, out uint soundId);

		static uint Create (NSUrl fileUrl)
		{
			if (fileUrl is null)
				ObjCRuntime.ThrowHelper.ThrowArgumentNullException (nameof (fileUrl));

			var error = AudioServicesCreateSystemSoundID (fileUrl.Handle, out var soundId);
			if (error != AudioServicesError.None)
				throw new InvalidOperationException (string.Format ("Could not create system sound ID for url {0}; error={1}",
							fileUrl, error));
			return soundId;
		}

		public SystemSound (NSUrl fileUrl)
			: this (Create (fileUrl))
		{
		}

		public static SystemSound? FromFile (NSUrl fileUrl)
		{
			if (fileUrl is null)
				ObjCRuntime.ThrowHelper.ThrowArgumentNullException (nameof (fileUrl));

			var error = AudioServicesCreateSystemSoundID (fileUrl.Handle, out var soundId);
			if (error != AudioServicesError.None)
				return null;
			return new SystemSound (soundId, true);
		}

		public static SystemSound? FromFile (string filename)
		{
			if (filename is null)
				ObjCRuntime.ThrowHelper.ThrowArgumentNullException (nameof (filename));

			using (var url = new NSUrl (filename)) {
				var error = AudioServicesCreateSystemSoundID (url.Handle, out var soundId);
				if (error != AudioServicesError.None)
					return null;
				return new SystemSound (soundId, true);
			}
		}

#if !NET
		delegate void AddSystemSoundCompletionCallback (SystemSoundId id, IntPtr clientData);
#endif

		[DllImport (Constants.AudioToolboxLibrary)]
#if NET
		unsafe static extern AudioServicesError AudioServicesAddSystemSoundCompletion (uint soundId, IntPtr runLoop, IntPtr runLoopMode, delegate* unmanaged<SystemSoundId, IntPtr, void> completionRoutine, IntPtr clientData);
#else
		static extern AudioServicesError AudioServicesAddSystemSoundCompletion (uint soundId, IntPtr runLoop, IntPtr runLoopMode, AddSystemSoundCompletionCallback completionRoutine, IntPtr clientData);
#endif

#if NET
		[UnmanagedCallersOnly]
#else
		[MonoPInvokeCallback (typeof (Action<SystemSoundId, IntPtr>))]
#endif
		static void SoundCompletionShared (SystemSoundId id, IntPtr clientData)
		{
			GCHandle gch = GCHandle.FromIntPtr (clientData);
			var ss = gch.Target as SystemSound;

			if (ss?.completionRoutine is not null)
				ss.completionRoutine ();
		}

		public AudioServicesError AddSystemSoundCompletion (Action routine, CFRunLoop? runLoop = null)
		{
			if (gc_handle.IsAllocated)
				throw new ArgumentException ("Only single completion routine is supported");

			gc_handle = GCHandle.Alloc (this);
			completionRoutine = routine;

			unsafe {
				return AudioServicesAddSystemSoundCompletion (soundId,
														  runLoop.GetHandle (),
														  IntPtr.Zero, // runLoopMode should be enum runLoopMode.GetHandle (),
#if NET
			                                              &SoundCompletionShared,
#else
														  SoundCompletionCallback,
#endif
														  GCHandle.ToIntPtr (gc_handle));
			}
		}

		[DllImport (Constants.AudioToolboxLibrary)]
		static extern void AudioServicesRemoveSystemSoundCompletion (uint soundId);

		public void RemoveSystemSoundCompletion ()
		{
			completionRoutine = null;
			AudioServicesRemoveSystemSoundCompletion (soundId);
		}
	}
}
