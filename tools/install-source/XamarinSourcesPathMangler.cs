using System;
using System.IO;

namespace InstallSources {
	/// <summary>
	/// Path manipulator that can deal with the Xamarin sources.
	/// </summary>
	public class XamarinSourcesPathMangler : IPathMangler {
		static string GeneratedExtension = ".g.cs";
		static string NativeTypeSubpath = "NativeTypes";
		static string CommonBuildSourceSubpath = "/build/common/";
		static string CommonToolsSourceSubpath = "/tools/common/";
		static string RuntimeSubpath = "generated.cs";

		/// <summary>
		/// Gets and sets the path to the xamarin source.
		/// </summary>
		/// <value>The xamarin source path.</value>
		public string XamarinSourcePath { get; set; }

		/// <summary>
		/// Gets and sets the path used as the dir name for the framework.
		/// </summary>
		/// <value>The framework path.</value>
		public string FrameworkPath { get; set; }

		/// <summary>
		/// Gets or sets the install dir.
		/// </summary>
		/// <value>The install dir.</value>
		public string InstallDir { get; set; }

		/// <summary>
		/// Gets or sets the frame work dir.
		/// </summary>
		/// <value>The frame work dir.</value>
		public string DestinationDir { get; set; }

		/// <summary>
		/// Returns if the provided path was compiler generated or not.
		/// </summary>
		/// <returns><c>true</c>, if generated path was generated, <c>false</c> otherwise.</returns>
		/// <param name="path">The source path to mangle.</param>
		static bool IsGeneratedPath (string path)
		{
			return path.EndsWith (GeneratedExtension, StringComparison.InvariantCulture);
		}

		/// <summary>
		/// Returns if the source file is part of the common source generated by the generator.
		/// </summary>
		/// <returns><c>true</c>, if common source was ised, <c>false</c> otherwise.</returns>
		/// <param name="path">Path.</param>
		public static bool IsManualSource (string path)
		{
			return path.EndsWith (".cs", StringComparison.CurrentCulture) && !path.Contains (CommonBuildSourceSubpath) && !path.Contains (CommonToolsSourceSubpath) && !path.Contains (NativeTypeSubpath);
		}

		/// <summary>
		/// Returns if the provided path is a NativeType.
		/// </summary>
		/// <returns><c>true</c>, if native type was ised, <c>false</c> otherwise.</returns>
		/// <param name="path">Path.</param>
		static bool IsNativeType (string path)
		{
			return path.Contains (NativeTypeSubpath);
		}

		/// <summary>
		/// Returns if the pat is from the runtime generated code.
		/// </summary>
		/// <returns><c>true</c>, if runrime was ised, <c>false</c> otherwise.</returns>
		/// <param name="path">Path.</param>
		static bool IsRunrime (string path)
		{
			return path.Contains (RuntimeSubpath);
		}

		/// <summary>
		/// Returns the source path for a generated file.
		/// </summary>
		/// <returns>The source path for native type.</returns>
		/// <param name="path">Path.</param>
		string GetSourcePathForGeneratedPath (string path) => path;

		/// <summary>
		/// Returns the source path for the native common types.
		/// </summary>
		/// <returns>The source path for native type.</returns>
		/// <param name="path">Path.</param>
		string GetSourcePathForNativeType (string path)
		{
			string src = "";
			var pos = path.IndexOf (CommonBuildSourceSubpath, StringComparison.InvariantCulture);
			if (pos >= 0) {
				src = path.Remove (0, pos + CommonBuildSourceSubpath.Length);
				src = Path.Combine (XamarinSourcePath, "build", "common", src);
			} else {
				pos = path.IndexOf (CommonToolsSourceSubpath, StringComparison.InvariantCulture);
				if (pos >= 0) {
					src = path.Remove (0, pos + CommonToolsSourceSubpath.Length);
					src = Path.GetFullPath (Path.Combine (XamarinSourcePath, "..", "tools", "common", src));
				} else {
					pos = path.IndexOf (NativeTypeSubpath, StringComparison.InvariantCulture);
					if (pos >= 0) {
						src = path.Remove (0, pos);
						src = Path.Combine (XamarinSourcePath, src);
					} else {
						Console.WriteLine ($"Ignoring path {path}");
						return "";
					}
				}
			}
			return src;
		}

		/// <summary>
		/// Returns the source path for the bindings that were manually done.
		/// </summary>
		/// <returns>The source path for manual source.</returns>
		/// <param name="path">Path.</param>
		string GetSourcePathForManualSource (string path)
		{
			var removalPath = $"/{FrameworkPath}/";
			var srcFolder = Path.Combine ("src", FrameworkPath.Remove (FrameworkPath.IndexOf (".framework", StringComparison.InvariantCulture))) + Path.DirectorySeparatorChar;
			var pos = path.IndexOf (removalPath, StringComparison.InvariantCulture);
			var src = path.Remove (0, pos + FrameworkPath.Length + 2);
			pos = src.IndexOf (srcFolder, StringComparison.InvariantCulture);
			if (pos >= 0)
				src = src.Remove (0, src.IndexOf (srcFolder, StringComparison.InvariantCulture) + srcFolder.Length);
			else // we are dealing with a correct path (this does happen on the mac side)
				src = src.Remove (0, src.IndexOf ("/src/", StringComparison.InvariantCulture) + "/src/".Length);
			src = Path.Combine (XamarinSourcePath, src);
			return src;
		}

		string GetSourcePathForRuntimeSource (string path)
		{
			Console.WriteLine ($"Path is {path}");
			if (path.StartsWith (InstallDir, StringComparison.Ordinal)) {
				var removalPath = Path.Combine (InstallDir, FrameworkPath.Replace (".framework", ""), "src");
				Console.WriteLine ($"Removal path s {removalPath}");
				var src = path.Remove (0, removalPath.Length);
				Console.WriteLine ($"Src is {src}");
				if (src.StartsWith ("/", StringComparison.Ordinal))
					src = src.Remove (0, 1);
				return Path.Combine (XamarinSourcePath.Replace ("src", "runtime"), src);
			}
			return path;
		}

		public string GetSourcePath (string path)
		{
			if (IsRunrime (path)) {
				return GetSourcePathForRuntimeSource (path);
			}
			// decide what type of path we are dealing with and return the correct source.
			if (IsGeneratedPath (path)) {
				return GetSourcePathForGeneratedPath (path);
			}
			if (IsManualSource (path))
				return GetSourcePathForManualSource (path);
			return GetSourcePathForNativeType (path);
		}

		string GetTargetPathForGeneratedPath (string path)
		{
			var subpath = (InstallDir.Contains ("Xamarin.iOS.framework")) ? "native" : "full";
			var pos = path.IndexOf (subpath, StringComparison.InvariantCulture);
			if (pos >= 0) {
				var relativePath = path.Remove (0, pos + subpath.Length + 1);
				return Path.Combine (DestinationDir, "src", FrameworkPath.Remove (FrameworkPath.IndexOf (".framework", StringComparison.InvariantCulture)), relativePath);
			}
			return null;
		}

		string GetTargetPathForManualSource (string path)
		{
			var pos = path.IndexOf (XamarinSourcePath, StringComparison.InvariantCulture);
			if (pos >= 0) {
				var relativePath = path.Remove (0, pos + XamarinSourcePath.Length);
				return Path.Combine (DestinationDir, "src", FrameworkPath.Remove (FrameworkPath.IndexOf (".framework", StringComparison.InvariantCulture)), relativePath);
			}
			return null;
		}

		string GetTargetPathForNativeType (string path)
		{
			var pos = path.IndexOf (NativeTypeSubpath, StringComparison.InvariantCulture);
			if (pos >= 0) {
				var relativePath = path.Remove (0, pos);
				return Path.Combine (DestinationDir, "src", FrameworkPath.Remove (FrameworkPath.IndexOf (".framework", StringComparison.InvariantCulture)), relativePath);
			}
			return null;
		}

		string GetTargetPathForRuntimeSource (string path)
		{
			var pos = path.IndexOf (RuntimeSubpath, StringComparison.InvariantCulture);
			if (pos >= 0) {
				var relativePath = path.Remove (0, pos + 1); // +1 is used to remove the leading / from RuntimeSubpath
				var result = Path.Combine (DestinationDir, "src", FrameworkPath.Remove (FrameworkPath.IndexOf (".framework", StringComparison.InvariantCulture)), relativePath);
				return result;
			}
			return null;
		}

		public string GetTargetPath (string path)
		{
			if (IsRunrime (path))
				return GetTargetPathForRuntimeSource (path);
			if (IsGeneratedPath (path))
				return GetTargetPathForGeneratedPath (path);
			if (IsManualSource (path))
				return GetTargetPathForManualSource (path);
			return GetTargetPathForNativeType (path);
		}

	}
}
