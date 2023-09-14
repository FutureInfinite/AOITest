using AOIServer.HUB.Versions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace AOIServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ChatController : ControllerBase
    {
        #region Properties&Attributes
        IHubContext<AOIChatHubV1> ChatHubContext;
        #endregion Properties&Attributes

        #region LifeTime
        public ChatController(IHubContext<AOIChatHubV1> ChatHubContext)
        {
            this.ChatHubContext = ChatHubContext;
        }
        #endregion LifeTime

        #region Operations

        [HttpPost(Name = "SendMessageToAll")]
        public void SendMessageToAll(string Chat)
        {
            ChatHubContext.Clients.All.SendAsync(Chat);
        }
        #endregion Operations
    }
}
