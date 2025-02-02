using FinalProjApi.Dto;
using FinalProjApi.Service.Game;
using FinalProjApi.Service.UserLogic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace FinalProjApi.Hubs
{
    [Authorize]
    public class HomeHub : Hub
    {

        internal readonly static ConcurrentDictionary<string, string> OnlineUsers = new ConcurrentDictionary<string, string>();

        private readonly IUserService _userService;
        private readonly IGameService _gameService;
        private readonly ILogger<HomeHub> _logger;


        public HomeHub(IUserService userService, IGameService gameService, ILogger<HomeHub> logger)
        {
            _userService = userService;
            _gameService = gameService;
            _logger = logger;
        }


        //  Handle  user connection
        public override async Task OnConnectedAsync()
        {

            var username = Context.User?.Identity?.Name;

            if (string.IsNullOrEmpty(username))
            {
                return;
            }
            try
            {
                var user = await _userService.GetUserByName(username);
                if (user != null)
                {
                    if ( user.IsOnline == false)
                    {
                        // Add to online users
                        OnlineUsers[username] = Context.ConnectionId;
                        await _userService.SetUserOnline(username);

                        await NotifyAllFriends(username); 
                        await SendInitialDataToTheClient(username); 
                        await BroadcastOnlineUsersAsync(); 

                    
                    }                  
                }
                await base.OnConnectedAsync();
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while {username} tried to connect HomeHub", username);
            }
        }


        //when user disconnects
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var username = Context.User?.Identity?.Name;

            if (string.IsNullOrEmpty(username))
            {
                return;
            }
            try
            {
                var user = await _userService.GetUserByName(username);
                if (user != null && OnlineUsers.TryRemove(username, out _))
                {
                    await _userService.SetUserOffline(username);
                    await NotifyAllFriends(username);
                    await BroadcastOnlineUsersAsync();
                }

                await base.OnDisconnectedAsync(exception);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " {username} Error during OnDisconnectedAsync ", username);
            }
         
        }


        // Sending a friend request to a specific user
        public async Task SendFriendRequest(string toUsername)
        {

            var fromUsername = Context.User?.Identity?.Name;
            if (fromUsername == null) return;

            try
            {
                // Check if a friend request already exists
                var existingRequest = await _userService.CheckIfFriendRequestExists(fromUsername, toUsername);
                if (existingRequest)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", new { message = "Friend Request already sent." }); 
                    return;
                }

                if (OnlineUsers.TryGetValue(toUsername, out var connectionId))
                {
                    // User is online, send real-time notification
                    await Clients.Client(connectionId).SendAsync("ReceiveFriendRequest", new FriendRequestDto
                    {
                        SenderUsername = fromUsername,
                        RequestSentAt = DateTime.UtcNow
                    });
                }
                // User is offline, save the friend request to the database for later
                await _userService.AddUserToFriendRequestList(fromUsername, toUsername);
            }
            catch (Exception ex)
            {
               _logger.LogError(ex, "An error occurred trying send friend request from {fromUsername} to {toUsername}", fromUsername, toUsername);
            }
        }

        // send a game invitation
        public async Task SendGameInvite(string toUsername)
        {
            var fromUsername = Context.User?.Identity?.Name;
            if (fromUsername == null) return;
            try
            {
                var existingInvite = await _userService.CheckIfGameInviteExists(fromUsername, toUsername);
                if (existingInvite)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", new { message = "Game invite already sent." });
                    return;
                }

                if (OnlineUsers.TryGetValue(toUsername, out var connectionId))
                {
                    await Clients.Client(connectionId).SendAsync("ReceiveGameInvite", new GameInviteDto
                    {
                        SenderUsername = fromUsername,
                        InviteSentAt = DateTime.UtcNow,
                        ExpiryTime = DateTime.UtcNow.AddMinutes(1)
                    });
                }
                await _userService.SaveGameInvite(fromUsername, toUsername);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred trying send game invite from {fromUsername} to {toUsername}", fromUsername, toUsername);
            }

        }

        public async Task AcceptGameInvite(string senderUsername)
        {
            var receiverUsername = Context.User?.Identity?.Name;
            if (receiverUsername == null) return;

            try
            {
                // Validate the game invite
                var validInvite = await _userService.CheckIfGameInviteExists(senderUsername, receiverUsername);
                if (!validInvite)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", new { message = "Game invite has expired or doesn't exist." });
                    return;
                }

                // Create a unique game room ID
                string gameRoomId = Guid.NewGuid().ToString();

                var gameState = _gameService.CreateGame(senderUsername, receiverUsername, gameRoomId);

                if (gameState == null)
                {
                    await Clients.Caller.SendAsync("ErrorMessage", new { message = "Failed to create game" });
                    return;
                }
               

                if (OnlineUsers.TryGetValue(senderUsername, out var senderConnectionId))
                {
                    await Clients.Client(senderConnectionId).SendAsync("GameReady", gameRoomId, receiverUsername);
                    await Clients.Caller.SendAsync("GameReady", gameRoomId, senderUsername);
                }
                else
                {
                    await Clients.Caller.SendAsync("ErrorMessage", new { message = "Sender is not connected." });
                }
                // Clean up the invite
                await _userService.RemoveGameInvite(senderUsername, receiverUsername);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred trying to accept game invite from {senderUsername} to {receiverUsername}", senderUsername, receiverUsername);
            }
        }

        public async Task UpdateFriendList(string username)
        {
            try
            {
                if (OnlineUsers.TryGetValue(username, out var connectionId))
                {
                    var friendsList = await _userService.GetAllUsersFriends(username);
                    await Clients.Client(connectionId).SendAsync("UpdateFriendList", friendsList);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred tying to get user : {username} friends", username);
            }

        }


        public async Task GetOnlineUsers()
        {
            var username = Context.User?.Identity?.Name;
            try
            {
                if (!string.IsNullOrEmpty(username) && await _userService.GetUserByName(username) != null)
                {
                    var usersOnline = OnlineUsers.Keys.ToList();


                    var onlineUsersDto = await _userService.GetUsersByNames(usersOnline);

                    await Clients.Caller.SendAsync("GetOnlineUsers", onlineUsersDto);
                }
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error get online users for the user {username}", username);
            }


        }


        private async Task NotifyAllFriends(string username)
        {
            var friendsOfUser = await _userService.GetAllUsersFriends(username);

            foreach (var friend in friendsOfUser)
            {
                if (OnlineUsers.TryGetValue(friend.Username, out var friendConnectionId))
                {
                    await Clients.Client(friendConnectionId).SendAsync("UpdateFriendList", await _userService.GetAllUsersFriends(friend.Username));
                }
            }
        }

        private async Task SendInitialDataToTheClient(string username)
        {
            var friendRequests = await _userService.GetFriendRequests(username);
            var gameInvites = await _userService.GetPendingGameInvites(username);
            var friendsList = await _userService.GetAllUsersFriends(username);

            await Clients.Caller.SendAsync("ReceiveFriendRequests", friendRequests);
            await Clients.Caller.SendAsync("ReceiveGameInvites", gameInvites);
            await Clients.Caller.SendAsync("UpdateFriendList", friendsList);

            var onlineUsers = OnlineUsers.Keys.ToList();
            var onlineUsersDto = await _userService.GetUsersByNames(onlineUsers);
            await Clients.Caller.SendAsync("GetOnlineUsers", onlineUsersDto);
        }

        private async Task BroadcastOnlineUsersAsync()
        {
            var onlineUsers = OnlineUsers.Keys.ToList();
            var onlineUsersDto = await _userService.GetUsersByNames(onlineUsers);
            await Clients.All.SendAsync("GetOnlineUsers", onlineUsersDto);
        }

    }
}
