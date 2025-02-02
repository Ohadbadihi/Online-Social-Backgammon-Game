using FinalProjApi.Dto;
using FinalProjApi.Hubs;
using FinalProjApi.Models;
using FinalProjApi.Service.UserLogic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace FinalProjApi.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class HomeController : ControllerBase
    {

        private readonly ILogger<HomeController> _logger;
        private readonly IUserService _userService;
        private readonly IHubContext<HomeHub> _hubContext;

        public HomeController(IUserService userService, ILogger<HomeController> logger, IHubContext<HomeHub> hubContext)
        {
            _logger = logger;
            _userService = userService;
            _hubContext = hubContext;
        }


        [HttpGet("users")]
        public async Task<IActionResult> GetUsers([FromQuery] string text)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    _logger.LogWarning("Search text is empty.");
                    return BadRequest("Search text cannot be empty.");
                }

                var users = await _userService.GetAllUsers();

                var matchedUsers = users
                    .Where(user => user.Username.Contains(text, StringComparison.OrdinalIgnoreCase))
                    .Select(user => user.Username)
                    .Take(10)
                    .ToList();

                if (!matchedUsers.Any())
                {
                    _logger.LogInformation("No users match the search text: {SearchText}", text);
                    return NotFound("No users match the search text.");
                }

                return Ok(matchedUsers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while searching users.");
                return StatusCode(500, "An unexpected error occurred while processing your request.");
            }
        }

        [HttpGet("getNotifications/{username}")]
        public async Task<IActionResult> GetAllNotificationsOfAUser(string username)
        {
            try
            {
                var trimmedUsername = username.Trim();

                if (trimmedUsername.Length < 2 || trimmedUsername.Length > 15)
                {
                    _logger.LogWarning("Invalid username length for notifications: {Username}", username);
                    return BadRequest("Username must be between 2 and 15 characters.");
                }

                var user = await _userService.GetUserByName(trimmedUsername);
                if (user == null)
                {
                    _logger.LogWarning("User not found for notifications: {Username}", username);
                    return NotFound("User not found.");
                }

                var gameInvites = await _userService.GetPendingGameInvites(trimmedUsername);
                var friendReq = await _userService.GetFriendRequests(trimmedUsername);

                return Ok(new
                {
                    FriendRequests = friendReq,
                    GameInvites = gameInvites
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while getting notifications for user {Username}.", username);
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }



        [HttpPost("friendRequest/accept")]
        public async Task<IActionResult> AcceptFriendRequest([FromBody] UsersDto usersDto)
        {
            if (usersDto == null || string.IsNullOrEmpty(usersDto.UserSendReq?.Username) || string.IsNullOrEmpty(usersDto.UserReceiveReq?.Username))
            {
                return BadRequest("Invalid request.");
            }
            try
            {
                bool isAccepted = await _userService.AcceptFriendRequest(usersDto.UserSendReq.Username, usersDto.UserReceiveReq.Username);
                if (isAccepted)
                {
                    var isSenderOnline = HomeHub.OnlineUsers.TryGetValue(usersDto.UserSendReq.Username, out var senderConnectionId);
                    var isReceiverOnline = HomeHub.OnlineUsers.TryGetValue(usersDto.UserReceiveReq.Username, out var receiverConnectionId);

                    if (isSenderOnline && isReceiverOnline)
                    {
                        // Notify both users about the updated friend list
                        await _hubContext.Clients.Client(senderConnectionId!).SendAsync("UpdateFriendList", await _userService.GetAllUsersFriends(usersDto.UserSendReq.Username));
                        await _hubContext.Clients.Client(receiverConnectionId!).SendAsync("UpdateFriendList", await _userService.GetAllUsersFriends(usersDto.UserReceiveReq.Username));

                        await _hubContext.Clients.Client(senderConnectionId!).SendAsync("FriendRequestAccepted", usersDto.UserReceiveReq.Username);

                    }
                    else if (isReceiverOnline)
                    {
                        await _hubContext.Clients.Client(receiverConnectionId!).SendAsync("UpdateFriendList", await _userService.GetAllUsersFriends(usersDto.UserReceiveReq.Username));
                    }
                    return Ok("Friend request accepted.");
                }

                _logger.LogWarning("Failed to accept friend request from {Sender} to {Receiver}.", usersDto.UserSendReq.Username, usersDto.UserReceiveReq.Username);
                return BadRequest("Failed to accept the friend request.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while accepting friend request from {Sender} to {Receiver}.", usersDto.UserSendReq.Username, usersDto.UserReceiveReq.Username);
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }


        [HttpPost("friendRequest/decline")]
        public async Task<IActionResult> DeclineFriendRequest([FromBody] UsersDto usersDto)
        {
            if (usersDto == null || string.IsNullOrEmpty(usersDto.UserSendReq.Username) || string.IsNullOrEmpty(usersDto.UserReceiveReq.Username))
            {

                return BadRequest("Somthing went wrong. Check if users exists.");
            }

            try
            {
                if (await _userService.RemoveFriendRequest(usersDto.UserSendReq.Username, usersDto.UserReceiveReq.Username))
                {
                    if (HomeHub.OnlineUsers.TryGetValue(usersDto.UserSendReq.Username, out var senderConnectionId))
                    {
                        await _hubContext.Clients.Client(senderConnectionId).SendAsync("FriendRequestDeclined", usersDto.UserReceiveReq.Username);
                    }
                    return Ok(new { message = "Friend request deleted successfully." });
                }

                _logger.LogWarning("Friend request not found for decline from {Sender} to {Receiver}.", usersDto.UserSendReq.Username, usersDto.UserReceiveReq.Username);

                return BadRequest("Request was not found.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while declining friend request from {Sender} to {Receiver}.", usersDto.UserSendReq.Username, usersDto.UserReceiveReq.Username);
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpGet("friends/{username}")]
        public async Task<IActionResult> GetFriends(string username)
        {
            try
            {
                var user = await _userService.GetUserByName(username);
                if (user == null)
                {
                    return BadRequest("Username is not exists.");
                }
                var userFriends = await _userService.GetAllUsersFriends(username);
                if (!userFriends.Any())
                {
                    return Ok(new List<FriendDto>());
                }
                return Ok(userFriends);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while getting friends for user {Username}.", username);
                return StatusCode(500, "An error occurred while processing your request.");
            }
        }

        [HttpGet("chatRoom/messages/{username}/{friendusername}")]
        public async Task<IActionResult> GetUsersLastMessagesFromChatRoom(string username, string friendUsername)
        {
            try
            {


                var user1 = await _userService.GetUserByName(username);
                var user2 = await _userService.GetUserByName(friendUsername);
                if (user1 == null || user2 == null)
                {
                    return BadRequest("Usernames does not exists.");
                }
                var messages = await _userService.GetRecentMessagesOfChat(username, friendUsername);

                return Ok(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while getting chat messages between {Username1} and {Username2}.", username, friendUsername);
                return StatusCode(500, "An error occurred while processing your request.");
            }

        }

    }
}
