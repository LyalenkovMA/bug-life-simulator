using Microsoft.Xna.Framework;

namespace TalesFromTheUnderbrush
{
    public interface IGameState
    {
        void LoadContent();
        void Update(GameTime gameTime);
        void Draw(GameTime gameTime);
        void OnEnter();
        void OnExit();
        GameStateType? GetNextState(); // Возвращает следующее состояние или null
    }
}
