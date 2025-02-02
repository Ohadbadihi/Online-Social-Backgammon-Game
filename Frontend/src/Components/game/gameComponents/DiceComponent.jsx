import { useState, useEffect } from 'react';
import './gameComponentsCss/dice.css';

const DiceComponent = ({ diceValues }) => {
  const [rolling, setRolling] = useState(false);

  useEffect(() => {
    if (diceValues.length > 0) {
      setRolling(true);
      setTimeout(() => setRolling(false), 1000);
    }
  }, [diceValues]);

  return (
    <div className="dice-container">
      {diceValues.map((roll, index) => (
        <div key={index} className={`die die-${roll} ${rolling ? 'rolling' : ''}`}>
          {roll}
        </div>
      ))}
    </div>
  );
};
export default DiceComponent;