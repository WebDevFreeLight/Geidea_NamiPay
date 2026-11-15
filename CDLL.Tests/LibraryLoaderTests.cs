// =====================================================================================
// EN: LibraryLoader: the libraries folder setting (relative / absolute / %VARIABLE%) and which file answers an
//     assembly request (dll or exe by simple name, never satellite resources or path tricks).
// AR: اختبار LibraryLoader: إعداد مجلد المكتبات، وأي ملف يجيب على طلب تحميل مكتبة.
// =====================================================================================

using System;
using System.IO;
using CDLL.Tests.Storage;
using Xunit;

namespace CDLL.Tests
{
    public sealed class LibraryLoaderTests : IDisposable
    {
        private readonly string _folder = TestRecords.NewTempFolder();

        [Fact]
        public void Folder_setting_relative_absolute_variable_or_empty()
        {
            string app = Path.Combine(_folder, "app") + Path.DirectorySeparatorChar;

            Assert.Equal(Path.Combine(_folder, "app", "libs"), LibraryLoader.ResolveFolder(null, app));
            Assert.Equal(Path.Combine(_folder, "app", "libs"), LibraryLoader.ResolveFolder("  ", app));
            Assert.Equal(Path.Combine(_folder, "app", "vendor"), LibraryLoader.ResolveFolder("vendor", app));
            Assert.Equal(Path.Combine(_folder, "shared"), LibraryLoader.ResolveFolder(@"..\shared", app));
            Assert.Equal(@"C:\CDLL\libs", LibraryLoader.ResolveFolder(@"C:\CDLL\libs", app));
            Assert.Equal(Path.Combine(Environment.GetEnvironmentVariable("ProgramData"), "CDLL", "libs"),
                         LibraryLoader.ResolveFolder(@"%ProgramData%\CDLL\libs", app));
        }

        [Fact]
        public void Assembly_file_is_found_by_simple_name_dll_first()
        {
            File.WriteAllText(Path.Combine(_folder, "Newtonsoft.Json.dll"), "");
            File.WriteAllText(Path.Combine(_folder, "Tool.exe"), "");

            Assert.Equal(Path.Combine(_folder, "Newtonsoft.Json.dll"),
                         LibraryLoader.FindAssemblyFile(_folder, "Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed"));
            Assert.Equal(Path.Combine(_folder, "Tool.exe"), LibraryLoader.FindAssemblyFile(_folder, "Tool"));
            Assert.Null(LibraryLoader.FindAssemblyFile(_folder, "Missing.Library"));
        }

        [Fact]
        public void Resources_and_bad_names_are_never_answered()
        {
            File.WriteAllText(Path.Combine(_folder, "CDLL.resources.dll"), "");

            Assert.Null(LibraryLoader.FindAssemblyFile(_folder, "CDLL.resources, Version=1.0.0.0, Culture=ar"));
            Assert.Null(LibraryLoader.FindAssemblyFile(_folder, ""));
            Assert.Null(LibraryLoader.FindAssemblyFile(_folder, "not a valid , = name ,,"));
        }

        public void Dispose()
        {
            try { Directory.Delete(_folder, true); } catch (IOException) { }
        }
    }
}
