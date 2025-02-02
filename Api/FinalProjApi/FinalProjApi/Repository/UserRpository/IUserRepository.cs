using FinalProjApi.Dto;
using FinalProjApi.Models;

namespace FinalProjApi.Repository.UserRpository
{
    public interface IUserRepository
    {
      
        Task AddUser(User user);
        Task RemoveUser(User user);

        Task<bool> CheckIfUserExist(string username);

        Task<User?> GetUser(string username);

        Task<IEnumerable<string>> GetAllUsersName();

        Task UpdateUser(User user);

        Task<IEnumerable<string>> SearchUsers(string searchText);
        Task<IEnumerable<User>> GetAllUserFriends(string username);
        Task AddToFriendsList(User user1, User user2);

        Task AddToFriendRequestList(User sender , User receiver);

        Task RemoveFriendRequest(User sender, User receiver);

        Task<bool> AreUsersFriends(User sender, User receiver);

        Task<IEnumerable<FriendRequest>> GetAllPendingFriendRequests(string username);
        Task<IEnumerable<GameInvite>> GetAllPendingGameInvites(string username);


        //Game invite
        Task<bool> CheckIfGameInviteExists(User sender, User receiver);
        Task RemoveExpiredGameInvites();

        Task SaveGameInvite(User sender, User receiver);

    


        //ChatRoom and messages
        Task CreateARoom(User user1, User user2);
        Task<bool> CheckIfChatRoomAlreadyExists(User user1, User user2);
        Task SaveMessage(User fromUser, User toUser, string message);
        Task<IEnumerable<ChatMessagesDto>?> GetRecentMessagesOfChat(string username1, string username2);

        Task<IEnumerable<Message>> GetUnreadMessages(User user);

        Task<bool> FriendRequestExists(User sender, User receiver);

        Task<bool> HasUnreadMessagesFromFriend(User user, User friend);

        Task SetUserOnline(User user);
        Task SetUserOffline(User user);

        Task<List<UserDto>> GetUsersByNames(List<string> usernames);

        Task DeleteGameInvitation(User sender, User receiver);
        Task MarkMessagesAsRead(User sender, User receiver);
        Task UpdateLastReadMessageAt(User user, User friend, DateTime messageTimeSent);
    }
}


