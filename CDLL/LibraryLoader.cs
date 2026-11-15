// =====================================================================================
// EN: Loads every library from ONE folder instead of the .exe folder, so DLLs of other apps / old builds can
//     never conflict. Folder = app.config "Libraries.Folder" (default "libs", relative to the .exe or absolute,
//     %VARIABLES% allowed). The build (build\Libraries.targets) puts all DLLs there.
//       - .NET assemblies (Newtonsoft.Json, SQLite, ...): <probing privatePath="libs"/> in app.config, and
//         AssemblyResolve below for anything probing does not find (e.g. a folder outside the .exe folder).
//       - Native DLLs (madaapi_v1_9.dll, madaapi_v1_8.dll, ECR.dll, SQLite): SetDllDirectory adds the folder to
//         the Windows DLL search path used by [DllImport].
//     Install() must run FIRST in Main, before any type that needs a library is touched.
//     If the folder does not exist, nothing changes (DLLs next to the .exe still work).
// AR: يحمّل كل المكتبات من مجلد واحد بدلاً من مجلد البرنامج، حتى لا تتعارض مكتبات برامج أخرى أو نسخ قديمة.
//     المجلد = "Libraries.Folder" في app.config (افتراضياً "libs" بجانب البرنامج أو مسار كامل).
//       - مكتبات ‎.NET: عبر probing في app.config، ثم AssemblyResolve لما لا يجده probing.
//       - المكتبات الأصلية (madaapi و ECR.dll و SQLite): عبر SetDllDirectory لمسار بحث Windows.
//     يجب استدعاء Install() أولاً في Main. إذا لم يوجد المجلد لا يتغير شيء.
// =====================================================================================

using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace CDLL
{
    internal static class LibraryLoader
    {
        public const string DefaultFolder = "libs";

        /// <summary>EN: Full path of the libraries folder (null before Install).  AR: المسار الكامل لمجلد المكتبات.</summary>
        public static string Folder { get; private set; }

        /// <summary>EN: True when the folder exists and is in use.  AR: صحيح إذا كان المجلد موجوداً ومستخدماً.</summary>
        public static bool Active { get; private set; }

        public static void Install()
        {
            Folder = ResolveFolder(ConfigurationManager.AppSettings["Libraries.Folder"], AppDomain.CurrentDomain.BaseDirectory);
            if (!Directory.Exists(Folder))
                return;

            Active = true;
            SetDllDirectory(Folder);
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
        }

        /// <summary>EN: "libs" / "..\shared\libs" / "C:\CDLL\libs" / "%ProgramData%\CDLL\libs" -> full path.  AR: يحوّل الإعداد إلى مسار كامل.</summary>
        internal static string ResolveFolder(string configured, string baseDirectory)
        {
            string folder = string.IsNullOrWhiteSpace(configured) ? DefaultFolder : Environment.ExpandEnvironmentVariables(configured.Trim());
            if (!Path.IsPathRooted(folder))
                folder = Path.Combine(baseDirectory, folder);
            return Path.GetFullPath(folder);
        }

        /// <summary>EN: The .dll / .exe file of an assembly in the folder, or null.  AR: ملف المكتبة في المجلد أو null.</summary>
        internal static string FindAssemblyFile(string folder, string assemblyFullName)
        {
            AssemblyName name;
            try { name = new AssemblyName(assemblyFullName); }
            catch (Exception) { return null; }

            // EN: satellite resources are optional; never answer for them   AR: ملفات الموارد اختيارية
            if (string.IsNullOrEmpty(name.Name) || name.Name.EndsWith(".resources", StringComparison.OrdinalIgnoreCase))
                return null;
            if (name.Name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                return null;

            foreach (string extension in new[] { ".dll", ".exe" })
            {
                string path = Path.Combine(folder, name.Name + extension);
                if (File.Exists(path))
                    return path;
            }
            return null;
        }

        private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            // EN: Already loaded (e.g. through probing)? Reuse it, never load a second copy.
            // AR: إذا كانت محمّلة مسبقاً تُستخدم نفسها، ولا تُحمّل نسخة ثانية.
            string simpleName = new AssemblyName(args.Name).Name;
            foreach (Assembly loaded in AppDomain.CurrentDomain.GetAssemblies())
                if (string.Equals(loaded.GetName().Name, simpleName, StringComparison.OrdinalIgnoreCase))
                    return loaded;

            string file = FindAssemblyFile(Folder, args.Name);
            return file == null ? null : Assembly.LoadFrom(file);
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetDllDirectory(string path);
    }
}
