using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using NUnit.Framework;

using Xamarin;

namespace Microsoft.MaciOS.Nnyeah.Tests {
	public class TestRunning {
		static string GetInvokingTestName (out string callingMethodClass, string callingMethodName)
		{
			var stackTrace = new System.Diagnostics.StackTrace ();
			var callingMethod = stackTrace.GetFrame (2)!.GetMethod ();
			Assert.NotNull (callingMethod, "unable to get calling test from stack frame.");

			if (string.IsNullOrEmpty (callingMethodName)) {
				if (!callingMethod!.CustomAttributes.Any (x => x.AttributeType.Name == "TestAttribute")) {
					Assert.Fail ("TestRunning expect invocations without an explicit `testName` parameter to be invoked from the [Test] method directly. Consider passing an explicit `testName`.");
				}
				callingMethodName = callingMethod.Name;
			}
			callingMethodClass = callingMethod!.DeclaringType!.Name;
			return callingMethodName;
		}

		public static async Task TestAndExecute (string libraryCode, string callingCode, string expectedOutput,
			string callingMethodName = "")
		{
			var testName = GetInvokingTestName (out var nameSpace, callingMethodName);
			var testClassName = "NnyeahTest" + testName;

			var initialLibraryDir = Cache.CreateTemporaryDirectory ();
			var finalLibraryDir = Cache.CreateTemporaryDirectory ();

			var libCompilerOutput = await BuildLibrary (libraryCode, testName, initialLibraryDir);
		}

		public static Task<string> BuildLibrary (string libraryCode, string libName, string outputDirectory, PlatformName platformName = PlatformName.macOS)
		{
			var outputPath = Path.Combine (outputDirectory, $"{libName}.dll");
			return Compiler.CompileText (libraryCode, outputPath, platformName, isLibrary: true);
		}
	}
}
