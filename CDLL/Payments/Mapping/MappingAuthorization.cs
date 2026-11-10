// =====================================================================================
// EN: Who may reassign a terminal to another workstation (the mapping screen).
//       - appSettings "Mapping.AllowedUsersOrGroups" = comma-separated Windows users or groups,
//         e.g. "HOSPITAL\BillingSupervisors, HOSPITAL\it.admin".
//       - Empty = Windows Administrators only (the app must be started "Run as administrator").
//     The check uses the Windows login, so there is no extra password to leak.
// AR: من يحق له نقل الجهاز إلى حاسوب آخر (شاشة الربط).
//       - الإعداد "Mapping.AllowedUsersOrGroups" = مستخدمو أو مجموعات Windows مفصولة بفواصل.
//       - إذا كان فارغاً = مسؤولو Windows فقط (يجب تشغيل البرنامج "كمسؤول").
//     التحقق يعتمد على حساب Windows، فلا توجد كلمة مرور إضافية قد تتسرب.
// =====================================================================================

using System;
using System.Configuration;
using System.Linq;
using System.Security.Principal;

namespace CDLL.Payments.Mapping
{
    public static class MappingAuthorization
    {
        public static bool IsCurrentUserAllowed(out string reason)
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                return IsAllowed(identity.Name, new WindowsPrincipal(identity), ConfigurationManager.AppSettings["Mapping.AllowedUsersOrGroups"], out reason);
            }
        }

        public static bool IsAllowed(string userName, IPrincipal principal, string allowedUsersOrGroups, out string reason)
        {
            reason = null;
            if (string.IsNullOrWhiteSpace(allowedUsersOrGroups))
            {
                var windows = principal as WindowsPrincipal;
                if (windows != null && windows.IsInRole(WindowsBuiltInRole.Administrator))
                    return true;
                reason = "Only Windows administrators can change terminal mappings. Run as administrator, or add your user/group to " +
                         "Mapping.AllowedUsersOrGroups in CDLL.exe.config. / فقط مسؤولو Windows يمكنهم تغيير ربط الأجهزة.";
                return false;
            }

            foreach (string entry in allowedUsersOrGroups.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(e => e.Trim()))
            {
                if (string.Equals(entry, userName, StringComparison.OrdinalIgnoreCase))
                    return true;
                try
                {
                    if (principal.IsInRole(entry))
                        return true;
                }
                catch (SystemException)
                {
                    // EN: unknown group name: ignore it   AR: اسم مجموعة غير معروف: يتم تجاهله
                }
            }

            reason = userName + " is not in Mapping.AllowedUsersOrGroups. / المستخدم غير مسموح له بتغيير ربط الأجهزة.";
            return false;
        }
    }
}
