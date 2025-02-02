import { useState, useEffect } from 'react';
import './gameComponentsCss/gameNotification.css';

const GameNotifications = ({ message, errorMessage }) => {
    const [gameMessage, setMessage] = useState('');
    const [gameErrorMessage, setErrorMessage] = useState('');

    useEffect(() => {

        displayMessage();
        displayErrorMessage();
    }, [message, errorMessage]);

    const displayMessage = () => {
        if(message.length > 0){
            setMessage(message);
        }
        setTimeout(() => {
            setMessage('');
        }, 3000);
    }
    const displayErrorMessage = () => {
        if(errorMessage.length > 0) {
            setErrorMessage(errorMessage);
        }
        setTimeout(() => {
            setErrorMessage('');
        }, 3000);
    }
      
    return <div>
        {gameMessage !== '' && (<div className="game-notification">{gameMessage}</div>)}
        {gameErrorMessage !== '' && (<div className="game-notification error">{gameErrorMessage}</div>)}
    </div>
};

export default GameNotifications;