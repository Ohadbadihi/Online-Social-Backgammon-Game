import { useContext, useEffect, useRef, useState } from 'react';
import { HubConnectionBuilder } from '@microsoft/signalr';
import { useLocation } from 'react-router-dom';
import { useNavigate } from 'react-router-dom';
import { PlayersContext } from '../../Tools/GamePlayersContext';
import { UserContext } from '../../Tools/UserContext';
import BoardComponent from './gameComponents/BoardComponent';
import DiceComponent from './gameComponents/DiceComponent';
import GameControls from './gameComponents/GameControls';
import PlayerInfo from './gameComponents/PlayerInfoComponent';
import GameNotifications from './gameComponents/GameNotifications';
import './game.css';

const Game = () => {
  const location = useLocation();
  const queryParams = new URLSearchParams(location.search);
  const gameId = queryParams.get('gameId');
  const navigate = useNavigate();

  const { players } = useContext(PlayersContext);
  const { user } = useContext(UserContext);
  

  const connectionRef = useRef(null);
  const [gameState, setGameState] = useState(null);
  const [possibleMoves, setPossibleMoves] = useState([]);
  const [errorMessage, setErrorMessage] = useState('');
  const [message, setMessage] = useState('');
  const [diceValues, setDiceValues] = useState([]);
  const [currentPlayerColor, setCurrentPlayerColor] = useState('');
  const [opponent, setOpponent] = useState({ username: players.opponent, color: "" });
  const [playersReady, setPlayersReady] = useState(false);


  useEffect(() => {

    if (!opponent) {
      console.error('Opponent name is not available');
      return;
    };

    if (!connectionRef.current) {
      const hubConnection = new HubConnectionBuilder().withUrl('https://localhost:7094/gamehub', {
        withCredentials: true
      }).withAutomaticReconnect().build();


      hubConnection.start().then(() => { 
        console.log('Connected to GameHub');
        connectionRef.current = hubConnection;

        
        hubConnection.invoke('JoinGame', gameId);


        hubConnection.on('GameStarted', (game) => {
          console.log('GameStarted event received', game);
          setPlayersReady(true);
          setGameState(game);

          const currentUser = user.username;
          const color = game.player1 === currentUser ? game.player1Color : game.player2Color;
          setCurrentPlayerColor(color);

          setOpponent((prevOpponent) => ({
            ...prevOpponent,
            color: game.player1 === currentUser ? game.player2Color : game.player1Color,
          }));
        });

        hubConnection.on('UpdateBoard', (updatedGameState) => {
          setGameState(updatedGameState);
        });

        hubConnection.on('PossibleMoves', (moves) => {
          setPossibleMoves(moves);
        });

        hubConnection.on('GameOver', (data) => {
          setGameState((prevState) => ({ ...prevState, isGameOver: true }));
          setMessage(`Game Over! Winner: ${data.Winner}`);
          setTimeout(() => {
            navigate("/home");
          }, 4000);
        });



        hubConnection.on('GameError', (message) => {
          setErrorMessage(message);
          // Clear the error message after some time
          setTimeout(() => setErrorMessage(''), 5000);
        });


        hubConnection.on('TurnChanged', (nextPlayerName) => {
          setGameState((prevState) => ({
            ...prevState,
            currentTurn: nextPlayerName, /*prevState.currentTurn === 'White' ? 'Black' : 'White' */
            diceRoll: [],
          }));
        });

        hubConnection.on('DiceRolled', (diceValues) => {
          setDiceValues(diceValues);
        });


      }).catch(err => { 
        console.error('Connection error:', err)
        setErrorMessage('Failed to connect to GameHub');
      });
    }

    return () => {
      if (connectionRef.current) {
        connectionRef.current.off('GameStarted');
        connectionRef.current.off('UpdateBoard');
        connectionRef.current.off('GameError');
        connectionRef.current.off('TurnChanged');
        connectionRef.current.off('GameReconnected');
        connectionRef.current.off('DiceRolled');
        connectionRef.current.off('PossibleMoves');
        connectionRef.current.stop();
      }
    };
  }, [gameId, navigate, opponent.username, user.username]);



  const isPlayerTurn = () => {
    if (!gameState || !user) return false;
    const currentUser = user.username;
    const playerColor = gameState.player1 === currentUser ? gameState.player1Color : gameState.player2Color;
    return gameState.currentTurn === playerColor;
  };

  const makeMove = async (fromIndex, toIndex) => {
    if (connectionRef.current && gameState) {
      try {
        if (!possibleMoves.some(move => move.From === fromIndex && move.To === toIndex)) {
          setErrorMessage('Invalid Move');
          return;
        }
        await connectionRef.current.invoke('MakeMove', gameState.gameId, fromIndex, toIndex);
      } catch (error) {
        console.error('Error making move:', error);
        setErrorMessage('Error making move:');
      }
    }
  };

  //player can end turn if it's their turn and they have no possible moves left
  const canEndTurn = () => isPlayerTurn();


  const endTurn = async () => {
    if (connectionRef.current && gameState) {
      try {
        await connectionRef.current.invoke('EndTurn', gameState.gameId);
      } catch (error) {
        console.error('Error ending turn:', error);
      }
    }
  };

  const rollDice = async () => {
    if (isPlayerTurn() && connectionRef.current) {
      try {
        await connectionRef.current.invoke('RollDice', gameState.gameId);
      } catch (error) {
        console.error('Error rolling dice:', error);
      }
    }
  };


  useEffect(() => {
    if (isPlayerTurn() && gameState && (!gameState.diceRoll || gameState.diceRoll.length === 0)) {
      rollDice();
    }

  }, [gameState?.currentTurn])


  useEffect(() => {
    if (connectionRef.current && gameState) {

      const fetchPossibleMoves = async () => {
        try {
          await connectionRef.current.invoke('GetPossibleMoves', gameState.gameId);
        } catch (error) {
          console.error('Error fetching possible moves:', error);
        }
      };
      fetchPossibleMoves();
    };
  }, [gameState?.currentTurn, gameState?.gameId]);



  useEffect(() => {
    let interval;
    if (gameState && connectionRef.current) {
      interval = setInterval(() => {
        setGameState((prevState) => {
          const newWhiteTime = prevState.currentTurn === 'White' ? Math.max(prevState.whiteTimeRemaining - 1, 0) : prevState.whiteTimeRemaining;
          const newBlackTime = prevState.currentTurn === 'Black' ? Math.max(prevState.blackTimeRemaining - 1, 0) : prevState.blackTimeRemaining;

          // Check if time has expired
          if (newWhiteTime === 0 || newBlackTime === 0) {
            connectionRef.current.invoke('TimerEnded', prevState.gameId);
            clearInterval(interval);
          }

          return {
            ...prevState,
            whiteTimeRemaining: newWhiteTime,
            blackTimeRemaining: newBlackTime,
          };
        });
      }, 1000);
    }

    return () => clearInterval(interval);
  }, [gameState, connectionRef.current]);


  return (
    <div className="game-page">
      <div className="game-container">
        {!playersReady ? (
          <div>Waiting for opponent to join...</div>
        ) : (
          <>
            <PlayerInfo gameState={gameState} connection={connectionRef.current} userColor={currentPlayerColor} opponent={opponent} />
            {(errorMessage || message) && (<GameNotifications errorMessage={errorMessage} message={message} />)}
            <BoardComponent gameState={gameState} onMove={makeMove} possibleMoves={possibleMoves} currentPlayer={currentPlayerColor} isPlayerTurn={isPlayerTurn}/>
            <DiceComponent diceValues={diceValues} />
            <GameControls onEndTurn={endTurn} canEndTurn={canEndTurn()} />
          </>
        )}
      </div>

    </div>
  );
};

export default Game;