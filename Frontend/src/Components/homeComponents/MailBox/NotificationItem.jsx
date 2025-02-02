import React, { memo, useEffect } from 'react';
import './notificationItem.css';
import { useNotifications } from '../../../Tools/NotificationContext';

const NotificationItem = memo(({ notification, onAccept, onDecline }) => {

  const { removeNotification } = useNotifications();

  useEffect(() => {
    if (notification.type === 'gameInvite') {
      const expiryTime = new Date(notification.expiryTime);
      const now = new Date();
      const timeUntilExpiry = expiryTime - now;

      if (timeUntilExpiry > 0) {
        const timer = setTimeout(() => {
          removeNotification(notification);
        }, timeUntilExpiry);

        return () => clearTimeout(timer);

      } else {
        removeNotification(notification);
      }
    }
  }, [notification, removeNotification]);


  const formatDateToLocalTime = (utcTime) => {
    const localTime = new Date(utcTime);
    return localTime.toLocaleString(undefined, {
      year: '2-digit',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      hour12: false, // Display in 24-hour format
    }).replace(',', ''); // Remove the comma between date and time
  };

  const formattedTime = formatDateToLocalTime(notification.requestSentAt || notification.inviteSentAt);



  return (
    <div className='notifications'>
      {notification.type === 'friendRequest' && (
        <div className='notification'>
          <p className='message-notification'>
            <strong>{notification.senderUsername}</strong> sent you a friend request at {formattedTime}
          </p>
          <button onClick={onAccept}>Accept</button>
          <button onClick={onDecline}>Decline</button>
        </div>
      )}
      {notification.type === 'gameInvite' && (
        <div className='notification'>
          <p className='message-notification'><strong>{notification.senderUsername}</strong> sent you an invitation to a play at {formattedTime}</p>
          <button onClick={onAccept}>Accept</button>
          <button onClick={onDecline}>Decline</button>
        </div>
      )}
    </div>
  );
}, (prevProps, nextProps) => prevProps.notification === nextProps.notification);

export default NotificationItem;