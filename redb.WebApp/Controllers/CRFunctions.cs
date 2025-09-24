using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using redb.Core;
using redb.Core.DBModels;

namespace redb.WebApp.Controllers
{
    [Route(template: "Cnt/[controller]")]
    [ApiController]
    [Authorize]
    public class CRFunctions(IRedbService redbService) : ControllerBase
    {
        [HttpGet("[action]")]
        public IActionResult Details(string sn, string fn) => new ContentResult()
        {
            Content = redbService.GetAll<_RFunction>()
                      .Where(f => f.SchemeNavigation.Name == sn && f.Name == fn)
                      .Select(o => o.Body).Single(),
            ContentType = "application/javascript",
            StatusCode = 200
        };
    }
}
