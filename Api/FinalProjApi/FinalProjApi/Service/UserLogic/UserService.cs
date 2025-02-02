using FinalProjApi.Dto;
using FinalProjApi.Hubs;
using FinalProjApi.Models;
using FinalProjApi.Repository.UserRpository;
using Microsoft.AspNetCore.Identity;

namespace FinalProjApi.Service.UserLogic
{

    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher<User> _passwordHasher;

        public UserService(IUserRepository userRepository, IPasswordHasher<User> passwordHasher)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;

        }


        public async Task<User?> Login(string username, string password)
        {
            var user = await _userRepository.GetUser(username);
            if (user == null || _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password) == PasswordVerificationResult.Failed)
            {
                return null;
            }
            return user;
        }


        public async Task<bool> Register(string username, string password)
        {

            if (await _userRepository.CheckIfUserExist(username))
            {
                return true;
            }

            User newUser = new User()
            {
                Username = username,
                PasswordHash = _passwordHasher.HashPassword(new User(), password)

            };
            await _userRepository.AddUser(newUser);
            return false;

        }


        public async Task UpdateUser(User user)
        {
            if (user != null)
                await _userRepository.UpdateUser(user);
        }

        public async Task UpdateUserWinsOrLoses(UserDto userDto)
        {
            if (userDto != null)
            {
                var userToUpdate = await _userRepository.GetUser(userDto.Username);
                if (userToUpdate != null)
                {
                    userToUpdate.Wins = userDto.Wins;
                    userToUpdate.Loses = userDto.Loses;
                    await _userRepository.UpdateUser(userToUpdate);
                }
            }
        }


        public async Task DeleteUser(string username)
        {
            if (await _userRepository.CheckIfUserExist(username))
            {
                var userToDelete = await _userRepository.GetUser(username);
                await _userRepository.RemoveUser(userToDelete!);
            }
        }


        public async Task<UserDto?> GetUserByName(string username)
        {
            var user = await _userRepository.GetUser(username);
            if (user != null)
            {
                var userDto = new UserDto()
                {
                    Username = user.Username,
                    Wins = user.Wins,
                    Loses = user.Loses,
                    IsOnline = user.IsOnline
                };
                return userDto;
            }

            return null;
        }


        public async Task<IEnumerable<UserDto>> GetAllUsers()
        {
            IEnumerable<string> users = await _userRepository.GetAllUsersName();

            if (!users.Any())
            {
                return Enumerable.Empty<UserDto>();
            }

            IEnumerable<UserDto> usersNameDto = users.Select(user => new UserDto
            {
                Username = user
            });

            return usersNameDto;
        }



        public async Task<IEnumerable<FriendDto>> GetAllUsersFriends(string username)
        {
            var user = await _userRepository.GetUser(username);
            if (user == null) return Enumerable.Empty<FriendDto>();

            var friends = await _userRepository.GetAllUserFriends(username);

            if (!friends.Any()) return Enumerable.Empty<FriendDto>();

            var friendDtos = new List<FriendDto>();
            foreach (var friend in friends)
            {
                bool hasUnreadMessages = await _userRepository.HasUnreadMessagesFromFriend(user, friend);

                friendDtos.Add(new FriendDto
                {
                    Username = friend.Username,
                    Wins = friend.Wins,
                    Loses = friend.Loses,
                    HasUnreadMessages = hasUnreadMessages,
                    IsOnline = HomeHub.OnlineUsers.ContainsKey(friend.Username)
                });
            }

            return friendDtos;
        }

        //req

        public async Task<IEnumerable<FriendRequestDto>> GetFriendRequests(string username)
        {
            if (await _userRepository.CheckIfUserExist(username))
            {
                var friendRequests = await _userRepository.GetAllPendingFriendRequests(username);

                return friendRequests.Select(fr => new FriendRequestDto
                {
                    SenderUsername = fr.Sender.Username,
                    RequestSentAt = fr.RequestSentAt,
                }).ToList();
            }

            return Enumerable.Empty<FriendRequestDto>();
        }

        public async Task<IEnumerable<GameInviteDto>> GetPendingGameInvites(string username)
        {
            if (await _userRepository.CheckIfUserExist(username))
            {
                var invitesToPlay = await _userRepository.GetAllPendingGameInvites(username);

                return invitesToPlay.Select(inv => new GameInviteDto
                {
                    SenderUsername = inv.Sender.Username,
                    InviteSentAt = inv.InviteSentAt,
                    ExpiryTime = inv.ExpiryTime
                }).ToList();
            }

            return Enumerable.Empty<GameInviteDto>();
        }

        public async Task<bool> AcceptFriendRequest(string senderUsername, string receiverUsername)
        {
            var sender = await _userRepository.GetUser(senderUsername);
            var receiver = await _userRepository.GetUser(receiverUsername);

            if (sender != null && receiver != null)
            {
                await _userRepository.AddToFriendsList(sender, receiver);

                // Remove the pending friend request if applicable
                await _userRepository.RemoveFriendRequest(sender, receiver);
                return true;
            }

            return false;
        }

        public async Task AddUserToFriendRequestList(string senderUsername, string receiverUsername)
        {
            var sender = await _userRepository.GetUser(senderUsername);
            var receiver = await _userRepository.GetUser(receiverUsername);
            if (sender != null && receiver != null && !await _userRepository.FriendRequestExists(sender, receiver) && !await _userRepository.AreUsersFriends(sender, receiver))
            {
                await _userRepository.AddToFriendRequestList(sender, receiver);
            }
        }

        public async Task<bool> RemoveFriendRequest(string userSenderUsername, string userReceiverUsername)
        {
            var sender = await _userRepository.GetUser(userSenderUsername);
            var receiver = await _userRepository.GetUser(userReceiverUsername);

            if (sender != null && receiver != null)
            {
                bool isFrExist = await _userRepository.FriendRequestExists(sender, receiver);
                if (isFrExist)
                {
                    if (sender != null && receiver != null && !await _userRepository.AreUsersFriends(sender, receiver))
                    {
                        await _userRepository.RemoveFriendRequest(sender, receiver);
                        return true;
                    }
                }
            }

            return false;
        }


        //chat
      
        public async Task SaveMessage(string fromUsername, string toUsername, string message)
        {
            var fromUser = await _userRepository.GetUser(fromUsername);
            var toUser = await _userRepository.GetUser(toUsername);

            if (fromUser != null && toUser != null)
            {
                var chatExists = await _userRepository.CheckIfChatRoomAlreadyExists(fromUser, toUser);
                if (!chatExists)
                {
                    await _userRepository.CreateARoom(fromUser, toUser);
                }
                await _userRepository.SaveMessage(fromUser, toUser, message);
            }
        }



        public async Task<IEnumerable<ChatMessagesDto>> GetRecentMessagesOfChat(string username1, string username2)
        {
            var user1 = await _userRepository.GetUser(username1);
            var user2 = await _userRepository.GetUser(username2);

            if (user1 != null && user2 != null)
            {
                if (await _userRepository.AreUsersFriends(user1, user2))
                {
                    var messages = await _userRepository.GetRecentMessagesOfChat(username1, username2);

                    if (messages != null && messages.Count() > 0)
                    {
                        return messages;
                    }
                }
            }
            return Enumerable.Empty<ChatMessagesDto>();
        }


        // game
        public async Task SaveGameInvite(string senderUsername, string receiverUsername)
        {
            var sender = await _userRepository.GetUser(senderUsername);
            var receiver = await _userRepository.GetUser(receiverUsername);

            if (sender != null && receiver != null)
            {
                await _userRepository.SaveGameInvite(sender, receiver);
            }
        }

        public async Task<bool> CheckIfGameInviteExists(string senderUsername, string receiverUsername)
        {
            var sender = await _userRepository.GetUser(senderUsername);
            var receiver = await _userRepository.GetUser(receiverUsername);

            if (sender != null && receiver != null)
            {
                if (await _userRepository.CheckIfGameInviteExists(sender, receiver))
                {
                    return true;
                }

            }
            return false;
        }
  

        public async Task MarkMessagesAsRead(string username, string friendUsername)
        {
            var user = await _userRepository.GetUser(username);
            var friend = await _userRepository.GetUser(friendUsername);

            if (user != null && friend != null)
            {
                await _userRepository.MarkMessagesAsRead(user, friend);
            }
        }


        public async Task<bool> CheckIfFriendRequestExists(string senderUsername, string receiverUsername)
        {
            var sender = await _userRepository.GetUser(senderUsername);
            var receiver = await _userRepository.GetUser(receiverUsername);

            if (sender == null || receiver == null)
            {
                return false;
            }

            return await _userRepository.FriendRequestExists(sender, receiver);
        }

        public async Task SetUserOnline(string username)
        {
            var user = await _userRepository.GetUser(username);

            if (user != null && user.IsOnline != true)
            {
                user.IsOnline = true;
                await _userRepository.UpdateUser(user);
            }
        }

        public async Task SetUserOffline(string username)
        {
            var user = await _userRepository.GetUser(username);

            if (user != null && user.IsOnline != false)
            {
                user.IsOnline = false;
                await _userRepository.UpdateUser(user);
            }
        }

        public async Task<bool> CheckIfUserOnline(string username)
        {
            var user = await _userRepository.GetUser(username);
            if (user != null)
            {
                if (user.IsOnline == true)
                {
                    return true;
                }

            }
            return false;
        }

        public async Task<List<UserDto>> GetUsersByNames(List<string> usernames)
        {

            return await _userRepository.GetUsersByNames(usernames);
        }

        public async Task<bool> RemoveGameInvite(string senderUsername, string receiverUsername)
        {
            var sender = await _userRepository.GetUser(senderUsername);
            var receiver = await _userRepository.GetUser(receiverUsername);

            if (sender == null || receiver == null)
            {
                return false;
            }
            await _userRepository.DeleteGameInvitation(sender, receiver);
            return true;
        }

        public async Task UpdateLastReadMessageAt(string username, string friendUsername, DateTime messageTimeSent)
        {
            var user = await _userRepository.GetUser(username);
            var friend = await _userRepository.GetUser(friendUsername);

            if (user != null && friend != null)
            {
                await _userRepository.UpdateLastReadMessageAt(user, friend, messageTimeSent);
            }
        }


    }
}
