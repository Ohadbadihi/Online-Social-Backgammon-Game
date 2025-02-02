import { useEffect, useCallback, useRef } from 'react';
import NotificationItem from './NotificationItem';
import { useNotifications } from '../../../Tools/NotificationContext';
import { useError } from '../../../Tools/ErrorContext';
import './mailbox.css';


function Mailbox({ username, homeConnection, onAction }) {

  const { notifications, addNotification, removeNotification } = useNotifications();
  const notificationsRef = useRef(notifications);
  const { handleError } = useError();


  // Keep notificationsRef up-to-date
  useEffect(() => {
    notificationsRef.current = notifications;
  }, [notifications]);



  useEffect(() => {
    const fetchNotifications = async () => {
      try {
        const response = await fetch(`https://localhost:7094/api/home/getNotifications/${username}`, {
          credentials: 'include',
        });
        if (!response.ok) {
          if (response.status === 500 || response.status === 404) {
            handleError('An error occurred. Please try again later.');
          }
        }

        const data = await response.json();
        const friendRequests = data.friendRequests || [];
        const gameInvites = data.gameInvites || [];

        gameInvites.forEach(gi => {
          const senderUsername = gi.senderUsername;
          const inviteSentAt = gi.inviteSentAt;
          const expiryTime = gi.expiryTime;
          if (!notificationsRef.current.some(n => n.senderUsername === senderUsername && n.type === 'gameInvite')) {
            addNotification({ type: 'gameInvite', senderUsername: senderUsername, inviteSentAt: inviteSentAt, expiryTime: expiryTime });
          }
        });

        // Prevent duplicate friend requests and game invites
        friendRequests.forEach(fr => {
          const senderUsername = fr.senderUsername;
          const requestSentAt = fr.requestSentAt;
          if (!notificationsRef.current.some(n => n.senderUsername === senderUsername && n.type === 'friendRequest')) {
            addNotification({ type: 'friendRequest', senderUsername: senderUsername, requestSentAt: requestSentAt });
          }
        });



      } catch (error) {
        handleError('An error occurred. Please try again later.');
      }
    };

    fetchNotifications();
  }, [username, addNotification, removeNotification]);




  useEffect(() => {
    if (homeConnection) {

      homeConnection.on('ReceiveFriendRequest', (friendRequestDto) => {
        const { senderUsername, requestSentAt } = friendRequestDto;
        if (!notificationsRef.current.some(n => n.senderUsername === senderUsername && n.type === 'friendRequest')) {
          addNotification({ type: 'friendRequest', senderUsername: senderUsername, requestSentAt: requestSentAt });
        }
      });

      homeConnection.on('ReceiveGameInvite', (gameInviteDto) => {
        const { senderUsername, inviteSentAt, expiryTime } = gameInviteDto;
        if (!notificationsRef.current.some(n => n.senderUsername === senderUsername && n.type === 'gameInvite')) {
          addNotification({ type: 'gameInvite', senderUsername: senderUsername, inviteSentAt: inviteSentAt, expiryTime: expiryTime });
          const timeUntilExpiry = new Date(expiryTime) - new Date();
          setTimeout(() => {
            removeNotification({ type: 'gameInvite', senderUsername: senderUsername });
          }, timeUntilExpiry);
        }
      });

      return () => {
        if (homeConnection) {
          homeConnection.off('ReceiveFriendRequests');
          homeConnection.off('ReceiveGameInvites');
        }

      };
    }
  }, [homeConnection, username, addNotification])




  useEffect(() => {
    const interval = setInterval(() => {
      const now = new Date();
      notificationsRef.current.forEach(notification => {
        if (notification.type === 'gameInvite' && new Date(notification.expiryTime) <= now) {
          removeNotification({ type: 'gameInvite', senderUsername: notification.senderUsername });
        }
      });
    }, 1000); // Check every second

    return () => clearInterval(interval);
  }, [removeNotification]);




  // useCallback to memoize these functions and prevent re-renders
  const handleAccept = useCallback(async (notification) => {

    const { senderUsername, type } = notification;

    if (type === 'gameInvite') {
      try {
        if (homeConnection && homeConnection.state === "Connected") {
          await homeConnection.invoke('AcceptGameInvite', senderUsername);
          removeNotification(notification);
        }
      } catch (error) {
        handleError('An error occurred. Please try again later.');
      }
    }

    else if (type === 'friendRequest') {
      try {
        const apiUrl = `https://localhost:7094/api/home/friendRequest/accept`;
        const requestBody = { UserSendReq: { Username: senderUsername }, UserReceiveReq: { username } };

        const response = await fetch(apiUrl, {
          method: 'POST',
          credentials: 'include',
          headers: {
            'Content-Type': 'application/json',
          },
          body: JSON.stringify(requestBody)
        });

        if (response.ok) {
          removeNotification(notification);
          onAction("Friend request accepted.");

        } else {
          if (response.status === 500 || response.status === 404) {
            navigate('/error', { state: { message: 'Server error occurred. Please try again later.' } });
          }
          console.error('Failed to accept the request');
        }
      }
      catch (error) {
        console.error('Error accepting friend request.');
      }

    }
  }, [homeConnection, removeNotification]);


  const handleDecline = useCallback(async (notification) => {

    const { senderUsername, type } = notification;
    try {
      const apiUrl = type === 'friendRequest'
        ? `https://localhost:7094/api/home/friendRequest/decline`
        : `https://localhost:7094/api/game/gameInvite/decline`;

      const requestBody = { UserSendReq: { Username: senderUsername }, UserReceiveReq: { Username: username } };

      const response = await fetch(apiUrl, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        credentials: 'include',
        body: JSON.stringify(requestBody)
      });

      if (response.ok) {
        removeNotification(notification);
      } else {
        if (response.status === 500 || response.status === 404) {
          handleError('An error occurred. Please try again later.');
        }
        console.error('Failed to decline the request');
      }
    } catch (error) {
      handleError('An error occurred. Please try again later.');
    }
  }, [username, removeNotification]);

  return (
    <div className="mailbox">
      {notifications.length === 0 ? (
        <p className='noNotifications-text'>No notifications</p>
      ) : (
        notifications.map((notification) => (
          <NotificationItem
            key={`${notification.type}-${notification.senderUsername}`}
            notification={notification}
            onAccept={() => handleAccept(notification)}
            onDecline={() => handleDecline(notification)}
          />
        ))
      )}
    </div>
  );
}

export default Mailbox;

