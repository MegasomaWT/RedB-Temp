using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using redb.Core;
using redb.WebApp.DataModels;
using redb.Core.DBModels;

namespace redb.WebApp.Controllers
{
    [Route(template: "Cnt/[controller]")]
    [ApiController]
    public class SidebarList(IRedbService redbService) : ControllerBase
    {
        private Task<List<SidebarListItem>> sidebarList(String? name) => redbService.GetAll<_RObject>()
                        .Where(o => o.ParentNavigation != null && o.ParentNavigation.Name == $"WebApp.Sidebar.{name}")
                        .SelectMany(v => v.Values)
                        .GroupBy(k => k.IdObject)
                        .Select(g => new SidebarListItem
                        {
                            name = g.Where(s => s.StructureNavigation.Name == "name").First().String,
                            path = g.Where(s => s.StructureNavigation.Name == "path").First().String
                        }).OrderBy(o => o.name).ToListAsync();

        [HttpGet("[action]")]
        public async Task<List<SidebarListItem>> General() => await sidebarList(GetActualAsyncMethodName());

        [HttpGet("[action]")]
        public async Task<List<SidebarListItem>> Global_Settings() => await sidebarList(GetActualAsyncMethodName());

        [HttpPost("[action]")]
        public void SetSidebarListIndex([FromQuery] int itemIndex)
        {
            SidebarListIndex = itemIndex;
        }


        //use for async method  (<-moveNext)
        static string? GetActualAsyncMethodName([CallerMemberName] string? name = null) => name;

        public static int? SidebarListIndex = null;
    }
}
