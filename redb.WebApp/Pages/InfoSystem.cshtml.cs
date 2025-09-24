using Microsoft.AspNetCore.Mvc.RazorPages;
using redb.Core;
using redb.WebApp.DataModels;
using System.Runtime.InteropServices;

namespace redb.WebApp.Pages
{
    using diInfo = IDictionary<String, String>;
    using static System.Globalization.CultureInfo;
    public class InfoSystem(IRedbService redbService) : PageModel
    {
        //readonly IRedbService _redbService = redbService;

        public DataModels.InfoSystem InfoView { get; private set; } = new DataModels.InfoSystem(redbService);

        public diInfo EnvironmentInfo { get; private set; } = new Dictionary<String, String>
        {
            { "Framework Description", RuntimeInformation.FrameworkDescription},
            { "Process Architecture", RuntimeInformation.ProcessArchitecture.ToString()},
            { "Runtime Identifier", RuntimeInformation.RuntimeIdentifier.ToString()}
        };

        public diInfo OsInfo { get; private set; } = new Dictionary<String, String>
        {
            { "Platform", RuntimeInformation.OSDescription},
            { "Version", Environment.OSVersion.VersionString.ToString()},
            { "Service Pack", String.IsNullOrEmpty(Environment.OSVersion.ServicePack) ? "-" : Environment.OSVersion.ServicePack},
            { "User Name", Environment.UserName},
            { "User Domain Name", Environment.UserDomainName},
        };

        public diInfo CultureInfo { get; private set; } = new Dictionary<String, String>
        {
            { "Compare Info", CurrentCulture.CompareInfo.ToString()},
            { "Display Name", CurrentCulture.DisplayName},
            { "English Name", CurrentCulture.EnglishName},
            { "Is Neutral Culture", CurrentCulture.IsNeutralCulture.ToString()},
            { "Is ReadOnly", CurrentCulture.IsReadOnly.ToString()},
            { "LCID", CurrentCulture.LCID.ToString()},
            { "Name", CurrentCulture.Name},
            { "Native Name", CurrentCulture.NativeName},
            { "Parent", CurrentCulture.Parent.ToString()},
            { "Text Info", CurrentCulture.TextInfo.ToString()},
            { "Three Letter ISOLanguage Name", CurrentCulture.ThreeLetterISOLanguageName},
            { "Three Letter Windows Language Name", CurrentCulture.ThreeLetterWindowsLanguageName},
            { "Two Letter ISOLanguage Name", CurrentCulture.TwoLetterISOLanguageName}
        };

        public diInfo DeviceInfo { get; private set; } = new Dictionary<String, String>
        {
            { "Processor Architecture", Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE") ?? "-"},
            { "Processor Model", Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "-"},
            { "Processor Level", Environment.GetEnvironmentVariable("PROCESSOR_LEVEL") ?? "-"},
            { "Processor Count", Environment.ProcessorCount.ToString()},
            { "System Directory", String.IsNullOrEmpty(Environment.SystemDirectory) ? "-" : Environment.SystemDirectory}
        };

        //public IActionResult OnGet()
        //{
        //    return Page();
        //}

        public void OnGet()
        {
        }

    }
}
