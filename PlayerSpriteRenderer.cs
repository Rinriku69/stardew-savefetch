using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace SaveFetch
{
    /// <summary>
    /// Renders the player's standing/idle sprite to a PNG, off-screen. Must be called on the
    /// game thread (SMAPI event handlers run there) — graphics calls aren't thread-safe, and
    /// this touches the shared GraphicsDevice, so it has to happen before ModEntry hands the
    /// upload off to a background Task.
    /// </summary>
    public static class PlayerSpriteRenderer
    {
        // one animation cel is 16x32px; render upscaled for a crisper web-side image, with a
        // margin around the frame since FarmerRenderer.draw's anchor (hat/hair overhang, etc.)
        // isn't precisely documented outside the game's own use of it — confirm the framing
        // looks right in the manual verification pass and tighten these if there's dead space.
        private const int FrameWidth = 16;
        private const int FrameHeight = 32;
        private const int Scale = 4;
        private const int CanvasWidth = FrameWidth * Scale * 2;
        private const int CanvasHeight = (int)(FrameHeight * Scale * 1.5);

        public static byte[] RenderIdlePortrait(Farmer player)
        {
            GraphicsDevice device = Game1.graphics.GraphicsDevice;

            using var renderTarget = new RenderTarget2D(device, CanvasWidth, CanvasHeight);
            using var spriteBatch = new SpriteBatch(device);

            device.SetRenderTarget(renderTarget);
            device.Clear(Color.Transparent);

            var position = new Vector2(CanvasWidth / 2f - (FrameWidth * Scale / 2f), CanvasHeight * 0.15f);

            spriteBatch.Begin(transformMatrix: Matrix.CreateScale(Scale));
            player.FarmerRenderer.draw(spriteBatch, player, FarmerSprite.walkDown, position / Scale);
            spriteBatch.End();

            device.SetRenderTarget(null);

            using var stream = new MemoryStream();
            renderTarget.SaveAsPng(stream, CanvasWidth, CanvasHeight);
            return stream.ToArray();
        }
    }
}
