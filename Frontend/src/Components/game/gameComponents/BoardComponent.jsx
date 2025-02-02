import { useState } from 'react';
import CheckerComponent from './CheckerComponent';
import BarComponent from './BarComponent';
import './gameComponentsCss/board.css';

const BoardComponent = ({ gameState, onMove, possibleMoves, currentPlayer, isPlayerTurn }) => {

  const [selectedChecker, setSelectedChecker] = useState(null);
  const [highlightedPoints, setHighlightedPoints] = useState([]);


  const handleCheckerClick = (fromIndex) => {
    // Fetch possible moves from the gameState or calculate them
    if (!isPlayerTurn()) return;
    const movesFromHere = possibleMoves
      .filter(move => move.From === fromIndex)
      .map(move => move.To);
    setSelectedChecker(fromIndex);
    setHighlightedPoints(movesFromHere);
  };

  const handlePointClick = (toIndex) => {
    if (isPlayerTurn() && highlightedPoints.includes(toIndex)) {
      onMove(selectedChecker, toIndex);
      setSelectedChecker(null);
      setHighlightedPoints([]);
    } else {
      // Deselect if clicking elsewhere
      setSelectedChecker(null);
      setHighlightedPoints([]);
    }
  };

  const handleDragStart = (e, fromIndex) => {
    if (!isPlayerTurn()) return;
    e.dataTransfer.setData('fromIndex', fromIndex);
  };

  const handleDrop = (e, toIndex) => {
    e.preventDefault();
    if (!isPlayerTurn()) return;
    const fromIndex = parseInt(e.dataTransfer.getData('fromIndex'), 10);
    onMove(fromIndex, toIndex);
    setSelectedChecker(null);
    setHighlightedPoints([]);
  };

  const handleDragOver = (e) => {
    e.preventDefault();
  };

  const renderPoint = (point, index, isUpperRow) => {
    const isEven = index % 2 === 0;
    const triangleClass = isUpperRow ? 'triangle-up' : 'triangle-down';
    const triangleColorClass = isEven ? 'point-light' : 'point-dark';
    const isSelected = selectedChecker === index;

    return (
      <div
        key={index}
        className={`point ${triangleClass} ${triangleColorClass} ${highlightedPoints.includes(index) ? 'highlighted' : ''}`}
        onClick={() => handlePointClick(index)}
        onDragOver={handleDragOver}
        onDrop={(e) => handleDrop(e, index)}
      >
        {point.map((checker, idx) => (
          <CheckerComponent
            key={idx}
            color={checker.color}
            offset={idx}
            index={index}
            isUpperRow={isUpperRow}
            onDragStart={handleDragStart}
            onClick={handleCheckerClick}
            isSelected={isSelected}
          />
        ))}
      </div>
    );
  };
                      
  const pointsUpper = currentPlayer === 'White' ? gameState.board.positions.slice(12, 24): gameState.board.positions.slice(0, 12).reverse();
  const pointsLower = currentPlayer === 'White' ? gameState.board.positions.slice(0, 12).reverse() : gameState.board.positions.slice(12, 24);

  return (
    <div className="board">

      <div className="board-row upper">
        {pointsUpper.map((point, index) => renderPoint(point, index, true))}
      </div>

      <BarComponent gameState={gameState} />
      
      <div className="board-row lower">
        {pointsLower.map((point, index) => renderPoint(point, 11 - index, false))}
      </div>
    </div>
  );
};

export default BoardComponent;