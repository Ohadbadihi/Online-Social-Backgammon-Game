using FinalProjApi.Service.UserLogic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace FinalProjApi.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly static ConcurrentDictionary<string, string> OnlineUsers = new();
        private static readonly ConcurrentDictionary<string, string> UserChattingWith = new();

        private readonly IUserService _userService;
        private readonly ILogger<ChatHub> _logger;


        public ChatHub(IUserService userService, ILogger<ChatHub> logger)
        {
            _userService = userService;
            _logger = logger;
        }


        public override async Task OnConnectedAsync()
        {

            var username = Context.User?.Identity?.Name;
            var friendUsername = Context.GetHttpContext()?.Request.Query["friendUsername"];
            if (!string.IsNullOrEmpty(username))
            {

                OnlineUsers.TryAdd(username, Context.ConnectionId);
                try
                {

                    if (!string.IsNullOrEmpty(friendUsername))
                    {
                        var friend = await _userService.GetUserByName(friendUsername!);

                        if (friend != null)
                        {
                            UserChattingWith[username] = friend.Username;

                            var recentMessages = await _userService.GetRecentMessagesOfChat(username, friend.Username);

                            if (recentMessages?.Any() == true)
                            {
                                await Clients.Caller.SendAsync("ReceiveRecentMessages", recentMessages);
                            }

                            await _userService.MarkMessagesAsRead(username, friend.Username);

                            var friendsList = await _userService.GetAllUsersFriends(username);
                            await Clients.Client(Context.ConnectionId).SendAsync("UpdateFriendList", friendsList);
                        }
                        else
                        {
                            _logger.LogWarning("Friend username {friendUsername} not found", friendUsername!);
                        }
                    }
                    await base.OnConnectedAsync();

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error connect to chat, {username} ", username);
                }
            }

        }




        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var username = Context.User?.Identity?.Name;
            if (!string.IsNullOrEmpty(username))
            {
                OnlineUsers.TryRemove(username, out _);
                UserChattingWith.TryRemove(username, out _);
            }
            await base.OnDisconnectedAsync(exception);
        }



        public async Task SendMessage(string receiverUsername, string message)
        {
            var senderUsername = Context.User?.Identity?.Name;
            if (string.IsNullOrEmpty(senderUsername)) return;

            try
            {
                var fromUser = await _userService.GetUserByName(senderUsername);
                var toUser = await _userService.GetUserByName(receiverUsername);

                if (toUser == null || fromUser == null)
                {                   
                    return;
                }


                await _userService.SaveMessage(senderUsername, receiverUsername, message);

                if (OnlineUsers.TryGetValue(receiverUsername, out var connectionId))
                {

                    await Clients.Client(connectionId).SendAsync("ReceiveMessage", senderUsername, message, DateTime.UtcNow.ToString("O"));

                    // Check if the recipient is chatting with the sender
                    if (UserChattingWith.TryGetValue(receiverUsername, out var chattingWith) && chattingWith == senderUsername)
                    {
                      
                        await _userService.UpdateLastReadMessageAt(toUser.Username, fromUser.Username, DateTime.UtcNow);

                        // Update recipient's friends list
                        var friendsList = await _userService.GetAllUsersFriends(receiverUsername);
                        await Clients.Client(connectionId).SendAsync("UpdateFriendList", friendsList);
                    }
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message from {senderUsername} to {receiverUsername},  this message {message}", senderUsername, receiverUsername, message);
            }
        }



    }
}
