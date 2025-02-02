import { useState } from 'react';
import './homeAside.css';

const FriendsList = ({ friends, onSelectedFriend }) => {

  const [selectedFriend, setSelectedFriend] = useState(null);

  const handleFriendClick = (friendUsername) => {
    if (selectedFriend === friendUsername) {
      setSelectedFriend(null);
    }
    else {
      setSelectedFriend(friendUsername);
    }
  };


  return (
    <div className="friendsList-Container">

      {friends.length > 0 ? (
        friends.map((friend) => (
          <div key={friend.username}
              className={`friend-item ${friend.hasUnreadMessages ? 'unread' : ''}`}
              onClick={() => handleFriendClick(friend.username)}>

            {friend.username} {friend.isOnline ? '(online)' : '(offline)'}
            {friend.hasUnreadMessages && <span className="unread-indicator">🟢</span>}
            {selectedFriend === friend.username && (
              <div className='friend-options'>
                <button onClick={() => onSelectedFriend(selectedFriend, "message")}>Send Message</button>
                <button onClick={() => onSelectedFriend(selectedFriend, "invite")}>Invite to Play</button>
              </div>
            )}

          </div>
        ))
      ) : (
        <p>No friends found</p>
      )}

    </div>
  );
};

export default FriendsList;
