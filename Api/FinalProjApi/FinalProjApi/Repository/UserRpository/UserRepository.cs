using FinalProjApi.Data;
using FinalProjApi.Dto;
using FinalProjApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FinalProjApi.Repository.UserRpository
{
    public class UserRepository : IUserRepository
    {
        private readonly DataBaseContext _context;

        public UserRepository(DataBaseContext context)
        {
            _context = context;

        }

        public async Task AddUser(User user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> CheckIfUserExist(string username)
        {
            return await _context.Users.AsNoTracking().AnyAsync(x => x.Username == username);
        }

        public async Task<IEnumerable<string>> GetAllUsersName()
        {
            return await _context.Users.AsNoTracking()
                .Select(user => user.Username).ToListAsync();
        }

        public async Task<User?> GetUser(string username)
        {
            return await _context.Users.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Username == username);
        }

        public async Task RemoveUser(User user)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateUser(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<string>> SearchUsers(string searchText)
        {
            return await _context.Users
                .Where(u => u.Username.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                .Select(u => u.Username)
                .ToListAsync();
        }

        public async Task<IEnumerable<User>> GetAllUserFriends(string username)
        {

            var user = await _context.Users
                .Include(u => u.Friendships)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null)
            {
                throw new Exception("User not found");
            }

            // Fetch friends where the user is either User1 or User2
            var userFriends = await _context.Friendships
                .Where(f => f.User1Id == user.Id || f.User2Id == user.Id)
                .Include(f => f.User1)
                .Include(f => f.User2)
                .ToListAsync();

            // Select friends based on which side of the relationship the user is
            var friends = userFriends.Select(f => f.User1Id == user.Id ? f.User2 : f.User1).ToList();

            return friends;
        }



        // Friend request
        public async Task AddToFriendsList(User user1, User user2)
        {
            var friendship = new Friendship
            {
                User1Id = user1.Id,
                User2Id = user2.Id
            };
            _context.Friendships.Add(friendship);
            await _context.SaveChangesAsync();

        }

        public async Task AddToFriendRequestList(User sender, User receiver)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var newFriendReq = new FriendRequest
                {
                    SenderId = sender.Id,
                    ReceiverId = receiver.Id
                };
                await _context.FriendRequests.AddAsync(newFriendReq);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw new Exception("Failed to add user to friend list", ex);
            }
        }

        public async Task RemoveFriendRequest(User sender, User receiver)
        {

            var req = await _context.FriendRequests.Where(fr => fr.Sender == sender && fr.Receiver == receiver).ToListAsync();
            foreach (var fr in req)
            {
                if (req != null)
                {
                    _context.FriendRequests.Remove(fr);
                    await _context.SaveChangesAsync();
                }
            }
        }

        public async Task<bool> AreUsersFriends(User user1, User user2)
        {
            return await _context.Friendships
            .AnyAsync(f => (f.User1Id == user1.Id && f.User2Id == user2.Id) ||
                       (f.User1Id == user2.Id && f.User2Id == user1.Id));
        }


        // Get all friend request that user have
        public async Task<IEnumerable<FriendRequest>> GetAllPendingFriendRequests(string username)
        {
            return await _context.FriendRequests
                .Where(fr => fr.Receiver.Username == username)
                .Include(fr => fr.Sender)
                .ToListAsync();
        }

        public async Task<IEnumerable<GameInvite>> GetAllPendingGameInvites(string username)
        {
            return await _context.GameInvites
                .Where(inv => inv.Receiver.Username == username && inv.ExpiryTime > DateTime.UtcNow)
                .Include(inv => inv.Sender)
                .ToListAsync();
        }


        // Invite to play
        public async Task SaveGameInvite(User sender, User receiver)
        {
            _context.Attach(sender);
            _context.Attach(receiver);
            var newInvitationToPlay = new GameInvite
            {
                SenderId = sender.Id,
                ReceiverId = receiver.Id,
                InviteSentAt = DateTime.UtcNow,
                ExpiryTime = DateTime.UtcNow.AddMinutes(1)
            };
            await _context.GameInvites.AddAsync(newInvitationToPlay);
            await _context.SaveChangesAsync();

        }

        public async Task RemoveExpiredGameInvites()
        {
            DateTime expirationThreshold = DateTime.UtcNow;

            await _context.GameInvites.Where(invite => invite.ExpiryTime <= expirationThreshold)
                .ExecuteDeleteAsync();
        }

        public async Task<bool> CheckIfGameInviteExists(User sender, User receiver)
        {
            var gameInvite = await _context.GameInvites.SingleOrDefaultAsync(gi => gi.Sender == sender && gi.Receiver == receiver);

            if (gameInvite != null)
            {
                return true;
            }
            return false;
        }


        // Messages

        // Save message to database
        public async Task SaveMessage(User fromUser, User toUser, string message)
        {
            // Track entitiies 
            fromUser = await _context.Users.FindAsync(fromUser.Id);
            toUser = await _context.Users.FindAsync(toUser.Id);

            if (fromUser == null || toUser == null)
            {
                throw new InvalidOperationException("One or both users do not exist.");
            }

            var chatRoom = await _context.ChatRooms
                .Include(r => r.ChatRoomUsers)
                .FirstOrDefaultAsync(r => r.ChatRoomUsers.Any(cru => cru.UserId == fromUser.Id) &&
                                           r.ChatRoomUsers.Any(cru => cru.UserId == toUser.Id));

            if (chatRoom == null)
            {
                chatRoom = new ChatRoom();
                chatRoom.ChatRoomUsers.Add(new ChatRoomUser { User = fromUser });
                chatRoom.ChatRoomUsers.Add(new ChatRoomUser { User = toUser });
                await _context.ChatRooms.AddAsync(chatRoom);
            }

            var newMessage = new Message
            {
                TheMessage = message,
                Sender = fromUser,
                TimeSent = DateTime.UtcNow,
                ChatRoom = chatRoom,
            };

            await _context.Messages.AddAsync(newMessage);
            await _context.SaveChangesAsync();
        }


        public async Task CreateARoom(User user1, User user2)
        {
            user1 = await _context.Users.FindAsync(user1.Id);
            user2 = await _context.Users.FindAsync(user2.Id);

            if (user1 == null || user2 == null)
            {
                throw new InvalidOperationException("One or both users do not exist.");
            }

            var newChatRoom = new ChatRoom();

            newChatRoom.ChatRoomUsers.Add(new ChatRoomUser { User = user1 });
            newChatRoom.ChatRoomUsers.Add(new ChatRoomUser { User = user2 });

            await _context.ChatRooms.AddAsync(newChatRoom);
            await _context.SaveChangesAsync();

        }

        public async Task<bool> CheckIfChatRoomAlreadyExists(User user1, User user2)
        {
            var chatRoom = await _context.ChatRooms
                .Where(cr => cr.ChatRoomUsers.Any(cru => cru.UserId == user1.Id) &&
                     cr.ChatRoomUsers.Any(cru => cru.UserId == user2.Id))
                    .Include(cr => cr.Messages)
                    .FirstOrDefaultAsync();
            if (chatRoom != null)
            {
                return true;
            }
            return false;
        }

        //Get messages from specific room
        public async Task<IEnumerable<ChatMessagesDto>?> GetRecentMessagesOfChat(string username1, string username2)
        {
            var user1 = await _context.Users.FirstOrDefaultAsync(u => u.Username == username1);
            var user2 = await _context.Users.FirstOrDefaultAsync(u => u.Username == username2);

            if (user1 == null || user2 == null)
            {
                throw new InvalidOperationException("One or both users do not exist.");
            }

            var messages = await _context.Messages.Where(m => m.ChatRoom.ChatRoomUsers
            .Any(cru => cru.UserId == user1.Id) && m.ChatRoom.ChatRoomUsers.Any(cru => cru.UserId == user2.Id))
                .OrderBy(m => m.TimeSent)
                .Take(20).Select(m => new ChatMessagesDto
                {
                    SenderUsername = m.Sender.Username,
                    Message = m.TheMessage,
                    TimeSent = m.TimeSent.ToString("O")
                })
                .ToListAsync();

            return messages;
        }


        public async Task<IEnumerable<Message>> GetUnreadMessages(User user)  // *****
        {
            return await _context.Messages
                .Where(m => m.ChatRoom.ChatRoomUsers.Any(cru => cru.UserId == user.Id))
                .Include(m => m.Sender)
                .ToListAsync();
        }

        public async Task<bool> HasUnreadMessagesFromFriend(User user, User friend) // ***** 
        {
            var chatRoomUser = await _context.ChatRoomUsers
            .FirstOrDefaultAsync(cru =>
                cru.UserId == user.Id &&
                cru.ChatRoom.ChatRoomUsers.Any(cu => cu.UserId == friend.Id));

            if (chatRoomUser != null)
            {
                var lastRead = chatRoomUser.LastReadMessageAt;

                var hasUnreadMessages = await _context.Messages.AnyAsync(m =>
                    m.UserId == friend.Id &&
                    m.ChatRoomId == chatRoomUser.ChatRoomId &&
                    m.TimeSent > lastRead);

                return hasUnreadMessages;
            }
            return false;
        }

        public async Task MarkMessagesAsRead(User sender, User receiver)
        {
            var chatRoomUser = await _context.ChatRoomUsers
                    .Include(cru => cru.ChatRoom.Messages)
                        .FirstOrDefaultAsync(cru =>
            cru.UserId == sender.Id &&
            cru.ChatRoom.ChatRoomUsers.Any(cu => cu.UserId == receiver.Id));

            if (chatRoomUser != null)
            {
                var lastMessageTime = chatRoomUser.ChatRoom.Messages
                    .Where(m => m.UserId == receiver.Id)
                    .Max(m => (DateTime?)m.TimeSent) ?? DateTime.MinValue;

                if (lastMessageTime > chatRoomUser.LastReadMessageAt)
                {
                    chatRoomUser.LastReadMessageAt = lastMessageTime;
                    await _context.SaveChangesAsync();
                }
            }
        }


        public async Task<bool> FriendRequestExists(User sender, User receiver)
        {
            return await _context.FriendRequests.AnyAsync(fr => fr.Sender.Id == sender.Id && fr.Receiver.Id == receiver.Id);
        }

        public async Task SetUserOnline(User user)
        {
            user.IsOnline = true;
            await _context.SaveChangesAsync();
        }

        public async Task SetUserOffline(User user)
        {
            user.IsOnline = false;
            await _context.SaveChangesAsync();
        }

        public async Task<List<UserDto>> GetUsersByNames(List<string> usernames)
        {
            return await _context.Users
                .Where(u => usernames.Contains(u.Username))
                .Select(u => new UserDto
                {
                    Username = u.Username,
                    Wins = u.Wins,
                    Loses = u.Loses,
                    IsOnline = u.IsOnline
                })
                .ToListAsync();
        }

        public async Task DeleteGameInvitation(User sender, User receiver)
        {
            var gi = await _context.GameInvites.FirstOrDefaultAsync(gi => gi.SenderId == sender.Id && gi.ReceiverId == receiver.Id);

            if (gi != null)
            {
                _context.GameInvites.Remove(gi);
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateLastReadMessageAt(User user, User friend, DateTime messageTimeSent)
        {
            var chatRoomUser = await _context.ChatRoomUsers
                .FirstOrDefaultAsync(cru =>
                    cru.UserId == user.Id &&
                    cru.ChatRoom.ChatRoomUsers.Any(cu => cu.UserId == friend.Id));

            if (chatRoomUser != null && messageTimeSent > chatRoomUser.LastReadMessageAt)
            {
                chatRoomUser.LastReadMessageAt = messageTimeSent;
                await _context.SaveChangesAsync();
            }
        }
    }

}



