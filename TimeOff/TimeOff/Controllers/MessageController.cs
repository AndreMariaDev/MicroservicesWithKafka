using Models;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Core.Interfaces;

namespace TimeOff.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MessageController : BaseController<Message>
    {
        private readonly IEmployee appService;

        public MessageController(IEmployee appService)
        {
            this.appService = appService;
            this.appService.OnCreateTopic();
        }


        [HttpPost("AddMessage")]
        //[Authorize]
        public async Task<IActionResult> AddUser([FromBody] Message item)
        {
            return await TryExecuteAction(async () =>
            {
                var result = await appService.OnMessage(item);
                return StatusCode((int)HttpStatusCode.Created, result);

            });
        }
    }
}
