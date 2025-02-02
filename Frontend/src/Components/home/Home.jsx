import { useEffect, useState, useContext, useRef } from "react";
import { UserContext } from "../../Tools/UserContext";
import { useNavigate } from "react-router-dom";
import { HubConnectionBuilder } from '@microsoft/signalr';
import Searchbar from '../homeComponents/HomeHeader/Searchbar';
import Mailbox from "../homeComponents/MailBox/Mailbox";
import ChatBox from "../homeComponents/Chatbox/ChatBox";
import FriendsList from '../homeComponents/HomeAside/HomeAside';
import HomeBody from "../homeComponents/HomeBody/HomeBody";
import OnlineUsers from "../homeComponents/OnlineUsersComponent/OnlineUsers";
import { PlayersContext } from "../../Tools/GamePlayersContext";
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faEnvelopeOpenText } from '@fortawesome/free-solid-svg-icons';
import { useNotifications } from '../../Tools/NotificationContext';
import dice from '../../assets/dice.png';
import './home.css';
import { useError } from "../../Tools/ErrorContext";

function Home() {
  const { setPlayers } = useContext(PlayersContext);
  const { hasNewNotifications, addNotification, markNotificationsAsSeen } = useNotifications();
  const { user, setUser } = useContext(UserContext);
  const [chatFriend, setChatFriend] = useState(null);
  const [friendsList, setFriendsList] = useState([]);
  const [isMailboxOpen, setIsMailboxOpen] = useState(false);
  const [isConnectionReady, setIsConnectionReady] = useState(false);
  const [unreadMessages, setUnreadMessages] = useState({});
  const [message, setMessage] = useState('');
  const homeConnectionRef = useRef(null);
  const { handleError } = useError();

  const navigate = useNavigate();



  useEffect(() => {
    const storedUsername = localStorage.getItem('Username');
    if (storedUsername) {
      setUser(prev => ({ ...prev, username: storedUsername }))
    }
    else {
      navigate("/login");
    }
  }, [setUser, navigate])



  useEffect(() => {
    const initializeConnection = async () => {
      if (!homeConnectionRef.current) {
        // Initialize SignalR connection
        const homeHubConnection = new HubConnectionBuilder()
          .withUrl('https://localhost:7094/homehub', { withCredentials: true })
          .withAutomaticReconnect()
          .build();

        await homeHubConnection.start();
        homeConnectionRef.current = homeHubConnection;

        console.log('HomeHub connection established for:', user.username);

        setIsConnectionReady(true);
        await homeHubConnection.invoke('GetOnlineUsers');


        homeHubConnection.on('ReceiveUnreadMessages', (unreadMessages) => {
          const updatedUnreadMessages = { ...unreadMessages }; // This should be an object with friend usernames as keys
          setUnreadMessages((prevState) => ({
            ...prevState,
            ...updatedUnreadMessages
          }));
        });

        homeHubConnection.on('ReceiveFriendRequest', (friendRequest) => {
          addNotification({
            type: 'friendRequest',
            senderUsername: friendRequest.senderUsername,
            requestSentAt: friendRequest.requestSentAt
          });
        });

        homeHubConnection.on('ReceiveGameInvite', (gameInvite) => {
          addNotification({
            type: 'gameInvite',
            senderUsername: gameInvite.senderUsername,
            inviteSentAt: gameInvite.inviteSentAt,
            expiryTime: gameInvite.expiryTime
          });
        });

        homeHubConnection.on('UpdateFriendList', (updatedFriends) => {
          setFriendsList(updatedFriends);
        });

        homeHubConnection.on('FriendRequestDeclined', (receiverUsername) => {
          displayMessage(`${receiverUsername} has declined your friend request.`);
        });

        homeHubConnection.on('FriendRequestAccepted', (receiverUsername) => {
          displayMessage(`${receiverUsername} has accepted your friend request.`);
        })

        homeHubConnection.on('GameInviteDeclined', (receiverUsername) => {
          displayMessage(`${receiverUsername} has declined your game invitation.`);
        });

        homeHubConnection.on('GameReady', (gameId, opponentUsername) => {
          console.log('GameReady event received on client:', user.username);
          console.log('gameId:', gameId, 'opponentUsername:', opponentUsername);
          setPlayers({ user: user.username, opponent: opponentUsername });
          displayMessage('Game Invite accepted by ' + opponentUsername);
          navigate(`/game?gameId=${gameId}`);
        });


        homeHubConnection.on('ErrorMessage', (message) => {
          const messageApi = message.message;
          console.log(messageApi)
          displayMessage(message.message);
        });
      }
    };

    initializeConnection();

    return () => {
      if (homeConnectionRef.current) {
        homeConnectionRef.current.off('ReceiveUnreadMessages');
        homeConnectionRef.current.off('GameInviteDeclined');
        homeConnectionRef.current.off('ReceiveFriendRequest');
        homeConnectionRef.current.off('ReceiveGameInvite');
        homeConnectionRef.current.off('UpdateFriendList');
        homeConnectionRef.current.off('FriendRequestDeclined');
        homeConnectionRef.current.off('GameReady');
        homeConnectionRef.current.off('ErrorMessage');
        homeConnectionRef.current.stop().catch(console.error);
        homeConnectionRef.current = null;
      }
    };
  }, []);



  useEffect(() => {
    const fetchFriendsList = async () => {
      try {
        const friendsResponse = await fetch(
          `https://localhost:7094/api/home/friends/${user.username}`,
          { credentials: "include" }
        );

        if (!friendsResponse.ok) {
          if (response.status === 500 || response.status === 404) {
            navigate('/error', { state: { message: 'Server error occurred. Please try again later.' } });
          }
        }
        const friendsData = await friendsResponse.json();
        setFriendsList(friendsData);
      } catch (error) {
        handleError('An error occurred. Please try again later.');
      }
    };
    if (user.username) {
      fetchFriendsList();
    }
  }, [user.username]);


  const toggleMailbox = () => {
    setIsMailboxOpen(prev => !prev);
    if (isMailboxOpen === true) {
      markNotificationsAsSeen();
    }
  };

  const handleFriendAction = (friend, action) => {
    if (action === 'message') {
      setChatFriend(friend);
    }
    else if (action === 'invite') {
      sendGameInviteToAFriend(friend);
      displayMessage("Game Invite sent to " + friend);
    }
  };


  const sendGameInviteToAFriend = async (friendUsername) => {
    try {
      await homeConnectionRef.current.invoke('SendGameInvite', friendUsername);
    } catch (error) {
      console.error('Error sending game invite:', error);
      handleError('An error occurred. Please try again later.');
    };
  };


  const handleLogout = async () => {
    try {
      console.log(user.username);
      const response = await fetch("https://localhost:7094/api/auth/logout", {
        method: 'POST',
        credentials: 'include',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username: user.username, sessionId: user.sessionId })
      });

      if (response.ok) {
        console.log("Logged out successfully");
        setUser({ username: '', sessionId: '' });
        localStorage.removeItem('Username');
        navigate('/login');
      }
      else {
        if (response.status === 500 || response.status === 404) {
          handleError('An error occurred. Please try again later.');
        }
      }

    } catch (error) {
      handleError('An error occurred. Please try again later.');
    }
  };

  const closeChat = () => {
    setChatFriend(null); // Close the chatbox
  };

  const displayMessage = (message) => {
    setMessage(message);
    console.log(message);
    setTimeout(() => {
      setMessage('');
    }, 3000)
  };


  return (
    <div className="home-page">

      <div className="home-Header">
        <div className="title-home"> <h1> Home Page </h1> <img className="diceImg" src={dice} alt="dicesImg" /> </div>
        {user.username && <div> Welcome, {user.username} </div>}
        <Searchbar connection={homeConnectionRef.current} onAction={displayMessage} />

        <div className="mailbox-container">
          <FontAwesomeIcon
            icon={faEnvelopeOpenText}
            className={`mailBoxicon ${hasNewNotifications ? 'has-new-notifications' : ''}`}
            onClick={toggleMailbox}
          />
          {hasNewNotifications && <span className="notification-badge">!</span>}
        </div>
        {isMailboxOpen && (
          <div className="mailbox-popup">
            <button onClick={toggleMailbox} className="close-mailbox">X</button>
            <Mailbox username={user.username} homeConnection={homeConnectionRef.current} onAction={displayMessage} />
          </div>
        )}

        <button onClick={handleLogout} className="home-logoutBtn">Logout</button>
      </div>

      {message !== '' && (<div className="messages-response">{message}</div>)}

      <div className="home-context">
        <div className="home-chat">
          {isConnectionReady && (
            <OnlineUsers connection={homeConnectionRef.current} />
          )}

          {chatFriend && <ChatBox userName={user.username} friend={chatFriend} closeChat={closeChat} homeConnection={homeConnectionRef.current} />}
        </div>

        <div className="home-body">
          <HomeBody />
        </div>

        <div className="home-FriendsList">
          <h3>Your Friends</h3>
          <FriendsList
            friends={friendsList.map(friend => ({
              ...friend,
              hasUnreadMessages: unreadMessages[friend.username] !== undefined // Check if the friend has unread messages
            }))}
            onSelectedFriend={handleFriendAction}
          />
        </div>
      </div>
    </div>
  )
}

export default Home;