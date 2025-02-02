import { useState, useEffect, useRef } from "react";
import { HubConnectionBuilder } from "@microsoft/signalr";
import './chatbox.css';
import { useError } from "../../../Tools/ErrorContext";


function ChatBox({ userName, friend, closeChat }) {
    const [messages, setMessages] = useState([]);
    const [newMessage, setNewMessage] = useState('');
    const connectionRef = useRef(null);
    const messageEndRef = useRef(null);
    const { handleError } = useError();

    useEffect(() => {

        const getLastMessages = async () => {
            try {
                const response = await fetch(`https://localhost:7094/api/home/chatRoom/messages/${userName}/${friend}`, {
                    method: 'GET',
                    headers: { 'Content-Type': 'application/json' },
                    credentials: 'include'
                });
                if (response.ok) {
                    const data = await response.json();
                    setMessages(data);
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

        if (userName && friend) {
            getLastMessages();
        }

    }, [friend, userName]);



    useEffect(() => {

        const connectToHub = async () => {
            const chatConnection = new HubConnectionBuilder()
                .withUrl(`https://localhost:7094/chatHub?friendUsername=${friend}`, {
                    withCredentials: true
                }).withAutomaticReconnect()
                .build();


            try {


                await chatConnection.start();
                connectionRef.current = chatConnection;


                chatConnection.on('ReceiveMessage', (senderUsername, message, timeSent) => {
                    setMessages((prevMessages) => [...prevMessages, { senderUsername, message, timeSent }]);
                });

                chatConnection.on('ReceiveRecentMessages', (messages) => {
                    setMessages(messages);
                });
            }
            catch (error) {
                handleError('An error occurred. Please try again later.');
            }
        };

        connectToHub();

        return () => {
            if (connectionRef.current) {
                connectionRef.current.off('ReceiveMessage');
                connectionRef.current.off('ReceiveRecentMessages');
                connectionRef.current.stop().then(() => console.log('SignalR disconnected')).catch(error => console.error('Error disconnecting SignalR:', error));
            }
        };

    }, [friend]);


    const sendMessage = async () => {
        if (connectionRef.current && newMessage.trim() !== '') {
            try {
                await connectionRef.current.invoke('SendMessage', friend, newMessage.trim());
                setMessages((prev) => [...prev, { senderUsername: userName, message: newMessage.trim(), timeSent: new Date().toISOString() }]);
                setNewMessage('');
            }
            catch (error) {
                console.error('Error sending message:', error);
            }

        }
    };

    useEffect(() => {
        if (messageEndRef.current) {
            messageEndRef.current.scrollIntoView({ behavior: 'smooth' });
        }
    }, [messages]);

    const handleKeyDown = (e) => {
        if (e.key === 'Enter') {
            sendMessage();
        }
    };
    const convertTimeToLocal = (utcTime) => {
        const date = new Date(utcTime);
        return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', hour12: false });
    };


    return (
        <div className="chatBox" >
            <div className="chatbox-header">
                <h4>Chat with {friend} </h4>
                <button className="closeChat-Btn" onClick={closeChat}>Close</button>
            </div>

            <div className="chatbox-body">
                <div className="chatMessage-container">
                    <div className="messages">
                        {messages.map((msg, index) => (
                            <div key={index} className={`message ${msg.senderUsername === userName ? 'my-message' : 'their-message'}`}>
                                <div className="content-message">
                                    <div className="whoSent-container"> <p>{msg.senderUsername === userName ? 'Me' : msg.senderUsername}:</p> </div>
                                    <div className="output-content">
                                        <div className="the-message">{msg.message}</div>
                                        <div className="whenSent">{convertTimeToLocal(msg.timeSent)}</div>
                                    </div>
                                </div>
                            </div>
                        ))}
                    </div>

                </div>

                <div className="input-chat">
                    <div className="input-container">
                        <input
                            type="text"
                            value={newMessage}
                            onChange={(e) => setNewMessage(e.target.value)}
                            onKeyDown={handleKeyDown}
                            placeholder="Type a message..."
                            className="input-chatbox"
                        />

                        <button className="sendMessage" onClick={sendMessage}>Send</button>
                    </div>

                </div>
            </div>

        </div>
    );
}

export default ChatBox;

