using Microsoft.Xna.Framework;

namespace TalesFromTheUnderbrush
{
    public class TestState : IGameState
    {
        public void LoadContent()
        {
            // Пока пусто
        }

        public void Update(GameTime gameTime)
        {
            // Простая логика
        }

        public void Draw(GameTime gameTime)
        {
            // Простая отрисовка для теста
        }

        public void OnEnter()
        {
            System.Diagnostics.Debug.WriteLine("TestState entered");
        }

        public void OnExit()
        {
            System.Diagnostics.Debug.WriteLine("TestState exited");
        }

        public GameStateType? GetNextState()
        {
            // Всегда остаёмся в этом состоянии для теста
            return null;
        }
    }
}
