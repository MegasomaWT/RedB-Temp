using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using redb.Core;
using redb.Core.DBModels;
using redb.WebApp.DataModels;

namespace redb.WebApp.Controllers
{
    [Route(template: "Cnt/[controller]")]
    [ApiController]
    [Authorize]
    public class CRObjects(IRedbService redbService) : ControllerBase
    {
        [HttpGet("[action]")]
        public Task<List<ObjectView>> GetAllObjects() => redbService.GetAll<_RObject>()
            .Select(o => new ObjectView
            {
                Id = o.Id.ToString(),
                ParentId = o.IdParent.ToString(),
                Name = o.Name
            }).ToListAsync();

        [HttpGet("[action]")]
        public async Task<ObjectItemView?> Details(long id) => await redbService.GetById<_RObject>(id);
    }
}
