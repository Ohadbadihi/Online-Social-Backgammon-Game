import { useEffect, useState, useContext } from 'react';
import { UserContext } from '../../../Tools/UserContext';
import './gameComponentsCss/playerInfo.css';

const PlayerInfo = ({ gameState, connection, userColor, opponent}) => {
  const { user } = useContext(UserContext);
  const currentUserName = user.username;
  const [whiteTime, setWhiteTime] = useState(gameState.whiteTimeRemaining);
  const [blackTime, setBlackTime] = useState(gameState.blackTimeRemaining);

  const playerColor = userColor;
  const opponentColor = opponent.color;
  const opponentName = opponent.username;

  useEffect(() => {
    const interval = setInterval(() => {
      if (gameState.currentTurn === 'White') {
        setWhiteTime((prev) => (prev > 0 ? prev - 1 : 0));
      } else {
        setBlackTime((prev) => (prev > 0 ? prev - 1 : 0));
      }

      // Notify server when time runs out
      if (whiteTime <= 0 && gameState.currentTurn === 'White') {
        connection.invoke('TimerEnded', gameState.gameId);
      }
      if (blackTime <= 0 && gameState.currentTurn === 'Black') {
        connection.invoke('TimerEnded', gameState.gameId);
      }
    }, 1000);

    return () => clearInterval(interval);
  }, [gameState.currentTurn, whiteTime, blackTime, connection, gameState.gameId]);


  const formatTime = (timeInSeconds) => {
    const minutes = Math.floor(timeInSeconds / 60).toString().padStart(2, '0');
    const seconds = (timeInSeconds % 60).toString().padStart(2, '0');
    return `${minutes}:${seconds}`;
  };

  return (
    <div className="player-info">
      <div
        className={`player ${
          gameState.currentTurn === playerColor ? 'active' : ''
        }`}
      >
        {currentUserName} ({playerColor}) - Time Left:{' '}
        {formatTime(playerColor === 'White' ? whiteTime : blackTime)}
      </div>
      <div
        className={`player ${
          gameState.currentTurn === opponentColor ? 'active' : ''
        }`}
      >
        {opponentName} ({opponentColor}) - Time Left:{' '}
        {formatTime(opponentColor === 'White' ? whiteTime : blackTime)}
      </div>
    </div>
  );
};

export default PlayerInfo;