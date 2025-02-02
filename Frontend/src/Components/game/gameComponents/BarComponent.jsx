import React from 'react';
import CheckerComponent from './CheckerComponent';
import './gameComponentsCss/bar.css';

const BarComponent = ({ gameState }) => {
  const whiteBarCheckers = Array.from({ length: gameState.board.whiteOut }, () => ({ color: 'White' }));
  const blackBarCheckers = Array.from({ length: gameState.board.blackOut }, () => ({ color: 'Black' }));

  return (
    <div className="bar">
      <div className="bar-section white-bar">
        {whiteBarCheckers.map((checker, idx) => (
          <CheckerComponent key={`white-${idx}`} color={checker.color} offset={idx} />
        ))}
      </div>
      <div className="bar-section black-bar">
        {blackBarCheckers.map((checker, idx) => (
          <CheckerComponent key={`black-${idx}`} color={checker.color} offset={idx} />
        ))}
      </div>
    </div>
  );
};

export default BarComponent;