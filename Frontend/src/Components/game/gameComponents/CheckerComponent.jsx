import './gameComponentsCss/checker.css';

const CheckerComponent = ({ color, offset, index, onDragStart, isUpperRow, isSelected, onClick  }) => {

  const checkerColor = color.toLowerCase();
  const style = isUpperRow
  ? { bottom: `${125 - (30 * offset) + 15}px` }
  
  : { top: `${125 - (30 * offset) + 15}px` };

  return (
    <div
      className={`checker ${checkerColor} ${isSelected ? 'selected' : ''}`}
      style={style}
      draggable="true"
      onDragStart={(e) => onDragStart(e, index)}
      onClick={() => onClick(index)}
    ></div>
  )
};
export default CheckerComponent;

