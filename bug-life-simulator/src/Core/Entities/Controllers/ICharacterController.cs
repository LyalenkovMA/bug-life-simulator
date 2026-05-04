using Microsoft.Xna.Framework;

namespace TalesFromTheUnderbrush.src.Core.Entities.Controllers
{
    public interface ICharacterController
    {
        void Attach(Character character);
        void Update(GameTime gameTime);
        void RequestCancel(); // Экстренная отмена (для прерываний)
    }
}