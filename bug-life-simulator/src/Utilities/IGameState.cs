using Microsoft.Xna.Framework;
using TalesFromTheUnderbrush.src.GameLogic;

namespace TalesFromTheUnderbrush
{
    public interface IGameState
    {
        void LoadContent();
        void Update(GameTime gameTime);
        void Draw(GameTime gameTime);
        void OnEnter();
        void OnExit();
        GameState? GetNextState(); // Возвращает следующее состояние или null
    }
}
