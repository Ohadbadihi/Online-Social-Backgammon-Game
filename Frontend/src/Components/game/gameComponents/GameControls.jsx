import React from 'react';
import './gameComponentsCss/gameControl.css'
const GameControls = ({ onEndTurn, canEndTurn }) => {
    
  return (
    <div className="game-controls">
       {canEndTurn && <button onClick={onEndTurn}>End Turn</button>}
    </div>
  );
};
export default GameControls;