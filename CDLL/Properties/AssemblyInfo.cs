using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// EN: Assembly metadata (name, version, copyright) shown in CDLL.exe > Properties > Details.
// AR: بيانات تعريف البرنامج (الاسم، الإصدار، حقوق النسخ) التي تظهر في CDLL.exe > خصائص > التفاصيل.

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("CDLL")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("CDLL")]
[assembly: AssemblyCopyright("Copyright ©  2018")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("bb484f3e-18c3-46a9-b75b-2c48d39af133")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

// EN: Lets the CDLL.Tests project test internal helpers.   AR: يسمح لمشروع CDLL.Tests باختبار الدوال الداخلية.
[assembly: InternalsVisibleTo("CDLL.Tests")]
