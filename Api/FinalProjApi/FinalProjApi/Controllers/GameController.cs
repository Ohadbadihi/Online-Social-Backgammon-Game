using FinalProjApi.Dto;
using FinalProjApi.Hubs;
using FinalProjApi.Service.UserLogic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace FinalProjApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class GameController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IHubContext<HomeHub> _hubContext;
        private readonly ILogger<GameController> _logger;

        public GameController(IUserService userService, IHubContext<HomeHub> hubContext, ILogger<GameController> logger)
        {
            _userService = userService;
            _hubContext = hubContext;
            _logger = logger;
        }

        [HttpPost("gameInvite/decline")]
        public async Task<IActionResult> DeclineGameInvite([FromBody] UsersDto usersDto)
        {
            try
            {


                if (usersDto == null || string.IsNullOrEmpty(usersDto.UserSendReq.Username) || string.IsNullOrEmpty(usersDto.UserReceiveReq.Username))
                {
                    _logger.LogWarning("Invalid game invite decline attempt.");
                    return BadRequest("Invalid request.");
                }

                if (await _userService.RemoveGameInvite(usersDto.UserSendReq.Username, usersDto.UserReceiveReq.Username))
                {
                    if (HomeHub.OnlineUsers.TryGetValue(usersDto.UserSendReq.Username, out var senderConnectionId))
                    {
                        await _hubContext.Clients.Client(senderConnectionId).SendAsync("GameInviteDeclined", usersDto.UserReceiveReq.Username);
                    }
                    return Ok(new { message = "Game invite declined successfully." });
                }

                _logger.LogWarning("Game invite not found for sender: {SenderUsername}, receiver: {ReceiverUsername}.", usersDto.UserSendReq.Username, usersDto.UserReceiveReq.Username);

                return BadRequest("Game invite not found.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while declining game invite from {SenderUsername} to {ReceiverUsername}.", usersDto.UserSendReq.Username, usersDto.UserReceiveReq.Username);
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }
    }
}
