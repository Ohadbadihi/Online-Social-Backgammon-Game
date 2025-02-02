using FinalProjApi.Dto;
using FinalProjApi.Models;

namespace FinalProjApi.Service.UserLogic
{
    public interface IUserService
    {
        Task<User?> Login(string username, string password);

        Task<bool> Register(string username, string password);

        Task UpdateUser(User user);

        Task DeleteUser(string username);

        Task<UserDto?> GetUserByName(string username);

        Task<IEnumerable<UserDto>> GetAllUsers();
   

        Task<IEnumerable<FriendRequestDto>> GetFriendRequests(string username);
        Task<IEnumerable<FriendDto>> GetAllUsersFriends(string username);
        Task<bool> AcceptFriendRequest(string username1, string username2);

        Task AddUserToFriendRequestList(string senderUsername, string receiverUsername);

        Task<bool> RemoveFriendRequest(string userSenderUsername, string userReceiverUsername);


        //messages and roomChat
        Task SaveMessage(string fromUsername, string toUsername, string message);
        Task<IEnumerable<ChatMessagesDto>> GetRecentMessagesOfChat(string username1, string username2);


        // Invitation to play
        Task<IEnumerable<GameInviteDto>> GetPendingGameInvites(string username);
        Task<bool> CheckIfGameInviteExists(string sender, string receiver);
        Task SaveGameInvite(string senderUsername, string receiverUsername);


        Task<bool> CheckIfFriendRequestExists(string senderUsername, string receiverUsername);

        Task SetUserOnline(string username);
        Task SetUserOffline(string username);
        Task<bool> CheckIfUserOnline(string username);

        Task<List<UserDto>> GetUsersByNames(List<string> usernames);

        Task UpdateUserWinsOrLoses(UserDto userDto);

        Task<bool> RemoveGameInvite(string senderUsername, string receiverUsername);
        Task MarkMessagesAsRead(string username, string friendUsername);
        Task UpdateLastReadMessageAt(string username, string friendUsername, DateTime messageTimeSent);

    }
}
