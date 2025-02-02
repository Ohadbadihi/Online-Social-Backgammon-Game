import { useState, useEffect, useRef, useContext } from "react";
import { UserContext } from "../../../Tools/UserContext";
import './searchbar.css';
import { useError } from "../../../Tools/ErrorContext";

const Searchbar = ({ connection, onAction }) => {

    const [usernameInSearchBar, setUsernameInSearchBar] = useState('');
    const [resultSearch, setResultSearch] = useState([]);
    const [showOptions, setShowOptions] = useState(false);
    const [selectedUser, setSelectedUser] = useState(null);
    const { user } = useContext(UserContext);
    const { handleError } = useError()

    const [windowPosition, setWindowPosition] = useState({ top: 100, left: 100 }); // initial position for the options window
    const windowRef = useRef(null);  // reference to the options window
    const offset = useRef({ x: 0, y: 0 });


    useEffect(() => {
        if (usernameInSearchBar.length > 1) {
            const timer = setTimeout(() => searchUsers(usernameInSearchBar), 300);
            return () => clearTimeout(timer);
        } else {
            setResultSearch([]);
        }
    }, [usernameInSearchBar]);

    const searchUsers = async (text) => {

        try {
            const response = await fetch(`https://localhost:7094/api/home/users?text=${text}`, {
                method: 'GET',
                credentials: 'include',
                headers: {
                    'Content-Type': 'application/json'
                }
            });
            if (response.ok) {
                const data = await response.json();
                setResultSearch(data);
            } else {
                if (response.status === 500) {
                    navigate('/error', { state: { message: 'Server error occurred. Please try again later.' } });
                }
                setResultSearch([]);
                onAction("No Users found.");
            }
        } catch (error) {

            handleError('Error searching username', error);
        }
    }

    const handleUserClick = (username) => {
        setSelectedUser(username);
        setShowOptions(true);
    };

    const closeOptions = () => {
        setShowOptions(false);
        setSelectedUser(null);
    };

    const sendFriendReq = async () => {

        const userSendReq = user.username;
        if (!userSendReq || !selectedUser) {
            handleError("Usernames are missing.");
            return;
        }
        try {
            const selectedUserFromBar = selectedUser;
            onAction("Friend request sent to " + selectedUserFromBar);
            await connection.invoke('SendFriendRequest', selectedUserFromBar);

        }
        catch (error) {
            handleError(error);
        }

        console.log("Sending friend request to:", selectedUser);
    }


    const sendGameInvite = async () => {
        const userSendReq = user.username;
        if (!selectedUser || !userSendReq) {
            handleError("Username are missing.");
            return;
        }

        try {           
            onAction("Game invite sent to " + selectedUser);
            await connection.invoke('SendGameInvite', selectedUser);

        } catch (error) {
            handleError('Error sending game invite:');
        };

    }


    const startDragging = (e) => {
        offset.current = {
            x: e.clientX - windowRef.current.getBoundingClientRect().left,
            y: e.clientY - windowRef.current.getBoundingClientRect().top
        };
        document.addEventListener('mousemove', onDrag);
        document.addEventListener('mouseup', stopDragging);
    }

    const onDrag = (e) => {
        setWindowPosition({
            top: e.clientY - offset.current.y,
            left: e.clientX - offset.current.x
        });
    }

    const stopDragging = () => {
        document.removeEventListener('mousemove', onDrag);
        document.removeEventListener('mouseup', stopDragging);
    }


    return (
        <div className="searchbar-container">

            <input
                className="searchbar-input"
                name="username"
                type="text"
                value={usernameInSearchBar}
                onChange={(e) => setUsernameInSearchBar(e.target.value)}
                placeholder="Search for users.."
            />

            {
                resultSearch.length > 0 &&
                (<div className="searchbar-dropdown">
                    {resultSearch.map((username, index) => (
                        <div key={index} className="searchbar-result"
                            onClick={() => handleUserClick(username)}> {username} </div>
                    ))}
                </div>)
            }

            {
                showOptions && selectedUser && (
                    <div className="user-options-window"
                        ref={windowRef}
                        style={{ top: `${windowPosition.top}px`, left: `${windowPosition.left}px` }}

                    >

                        <h3 className="user-options-header"
                            onMouseDown={startDragging}>

                            {selectedUser}
                            <button className="close-button" onClick={closeOptions}>X</button>

                        </h3>

                        <button onClick={sendGameInvite} className="btn-options-window"> Invite to Game </button>
                        <button onClick={sendFriendReq} className="btn-options-window"> Send Friend Request </button>

                    </div>
                )}


        </div>
    )
}

export default Searchbar;