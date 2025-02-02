namespace FinalProjApi.Game.GameModels
{
    public class Dice
    {
        public int[] Rolls { get; private set; }
        private static readonly Random random = new Random();
        public void RollDice()
        {
            int die1 = random.Next(1, 7);
            int die2 = random.Next(1, 7);

            Rolls = die1 == die2 ? new int[] { die1, die1, die1, die1 } : new int[] { die1, die2 };
        }

        public void RemoveRoll(int roll)
        {
            var rollsList = Rolls.ToList();
            rollsList.Remove(roll);
            Rolls = rollsList.ToArray();
        }
    }
}
