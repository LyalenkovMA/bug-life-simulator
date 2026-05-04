using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace TalesFromTheUnderbrush.src.Core.Entities.Controllers
{
    public class PlayerController : ICharacterController
    {
        private Character _character;
        private int _facingDirection = 4; // 0=N, 4=S
        private static readonly Point[] _offsets = new Point[]
        { new(0,-1), new(1,-1), new(1,0), new(1,1), new(0,1), new(-1,1), new(-1,0), new(-1,-1) };

        public void Attach(Character character) => _character = character;

        public void Update(GameTime gameTime)
        {
            if (_character == null || _character.CurrentState != CharacterState.Idle) return;

            UpdateFacingDirection();
            HandleRelativeInput();
        }

        public void RequestCancel() => _character?.Interrupt();

        private void UpdateFacingDirection()
        {
            // Логика поворота за мышью (как в предыдущем обсуждении)
            // Упрощено для примера. В проде используйте Camera.ScreenToWorld
        }

        private void HandleRelativeInput()
        {
            KeyboardState kb = Keyboard.GetState();
            int dx = 0, dy = 0;
            bool pressed = false;

            Point fwd = _offsets[_facingDirection];
            Point left = new(-fwd.Y, fwd.X);
            Point right = new(fwd.Y, -fwd.X);
            Point back = new(-fwd.X, -fwd.Y);

            if (kb.IsKeyDown(Keys.W)) { dx += fwd.X; dy += fwd.Y; pressed = true; }
            if (kb.IsKeyDown(Keys.S)) { dx += back.X; dy += back.Y; pressed = true; }
            if (kb.IsKeyDown(Keys.A)) { dx += left.X; dy += left.Y; pressed = true; }
            if (kb.IsKeyDown(Keys.D)) { dx += right.X; dy += right.Y; pressed = true; }

            if (pressed && (dx != 0 || dy != 0))
            {
                Point target = new Point(_character.GridPosition.X + dx, _character.GridPosition.Y + dy);
                // Передаем команду Администратору. Он сам проверит валидность и запустит цикл.
                _character.RequestAction(ActionType.Move, target, 0.25f, 0.4f);
            }
        }
    }
}