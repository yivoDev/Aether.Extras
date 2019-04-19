using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using tainicom.Aether.Atlas;
using tainicom.Aether.Shaders;

namespace Samples.Atlas
{
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        SpriteFont font;

        KeyboardState previousKeyboardState;

        int mipLevel = 4;
        bool showAtlas = false;
        bool useGenerateBitmap = true;
        bool useMipmapPerSprite = true;
        Rectangle atlasSize = new Rectangle(0, 0, 1024, 512);
        RenderTarget2D rt;


        TextureAtlas atlasMipmapPerSprite;
        TextureAtlas atlasMipmap;
        TextureAtlas atlasNoMipmap;
        Texture2D tiledMap;
        TiledMapEffect tiledMapEffect;
        
        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            graphics.PreferredBackBufferWidth = atlasSize.Width;
            graphics.PreferredBackBufferHeight = atlasSize.Height;
        }

        protected override void LoadContent()
        {            
            spriteBatch = new SpriteBatch(GraphicsDevice);
            font = Content.Load<SpriteFont>("font");

            rt = new RenderTarget2D(GraphicsDevice, atlasSize.Width, atlasSize.Height);

            // Load atlas
            atlasMipmapPerSprite = Content.Load<TextureAtlas>("atlasMipmapPerSprite");
            atlasMipmap = Content.Load<TextureAtlas>("atlasMipmap");
            atlasNoMipmap = Content.Load<TextureAtlas>("atlasNoMipmap");

            // Create tiledMap
            tiledMap = new Texture2D(GraphicsDevice, 4, 2, false, SurfaceFormat.Color);
            tiledMap.SetData(new Color[] {
                new Color(5,1,0,255),
                new Color(5,2,0,255),
                new Color(6,2,0,255),
                new Color(7,0,0,0),

                new Color(0,1,0,255),
                new Color(1,1,0,255),
                new Color(1,1,0,255),
                new Color(6,3,0,255),
            });

            // Create tiledMapEffect
            tiledMapEffect = new TiledMapEffect(GraphicsDevice);
            tiledMapEffect.Texture = tiledMap;
            tiledMapEffect.MapSize = new Vector2(tiledMap.Width, tiledMap.Height);
            tiledMapEffect.AtlasSize = new Vector2(8, 4);
            tiledMapEffect.Alpha = 1f;
            tiledMapEffect.VertexColorEnabled = true;
            tiledMapEffect.DiffuseColor = new Vector3(1f, 1f, 1f);
            tiledMapEffect.World = Matrix.Identity;
        }
        
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            KeyboardState keyState = Keyboard.GetState();
            GamePadState gamePadState = GamePad.GetState(PlayerIndex.One);

            if (keyState.IsKeyDown(Keys.Escape) || gamePadState.Buttons.Back == ButtonState.Pressed)
                Exit();

            if (keyState.IsKeyDown(Keys.F1) && !previousKeyboardState.IsKeyDown(Keys.F1))
                useMipmapPerSprite = !useMipmapPerSprite;
            if (keyState.IsKeyDown(Keys.F2) && !previousKeyboardState.IsKeyDown(Keys.F2))
                useGenerateBitmap = !useGenerateBitmap;
            if (keyState.IsKeyDown(Keys.F3) && !previousKeyboardState.IsKeyDown(Keys.F3))
                showAtlas = !showAtlas;
            if (keyState.IsKeyDown(Keys.OemPlus) && !previousKeyboardState.IsKeyDown(Keys.OemPlus) && mipLevel < 10)
                mipLevel++;
            if (keyState.IsKeyDown(Keys.Add) && !previousKeyboardState.IsKeyDown(Keys.Add) && mipLevel < 10)
                mipLevel++;
            if (keyState.IsKeyDown(Keys.OemMinus) && !previousKeyboardState.IsKeyDown(Keys.OemMinus) && mipLevel > 0)
                mipLevel--;
            if (keyState.IsKeyDown(Keys.Subtract) && !previousKeyboardState.IsKeyDown(Keys.Subtract) && mipLevel > 0)
                mipLevel--;
            
            previousKeyboardState = keyState;
            base.Update(gameTime);
        }
        
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.BlendState = BlendState.Opaque;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;

            int mipLevel2 = (int)Math.Pow(2, mipLevel);
            var mipSize = atlasSize;
            mipSize.Width /= mipLevel2;
            mipSize.Height /= mipLevel2;
            
            GraphicsDevice.SetRenderTarget(rt);
            GraphicsDevice.Clear(Color.Black);

            var currentAtlas = (useGenerateBitmap) ? (useMipmapPerSprite ? atlasMipmapPerSprite : atlasMipmap) : atlasNoMipmap;

            if (showAtlas)
            {
                spriteBatch.Begin();
                spriteBatch.Draw(currentAtlas.Texture, mipSize, Color.White);
                spriteBatch.End();
            }
            else
            {
                DrawTiledMap(gameTime, tiledMap, currentAtlas, mipSize);
            }


            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.CornflowerBlue);
            spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointWrap, null, null);
            spriteBatch.Draw(rt, atlasSize, mipSize, Color.White);
            spriteBatch.End();

            spriteBatch.Begin();
            spriteBatch.DrawString(font, String.Format("[F1] MipmapPerSprite - ({0})", useMipmapPerSprite ? "ON" : "OFF"), new Vector2(20, 20), Color.White);
            spriteBatch.DrawString(font, String.Format("[F2] GenerateMipmap - ({0})", useGenerateBitmap ? "ON" : "OFF"), new Vector2(20, 40), Color.White);
            spriteBatch.DrawString(font, String.Format("[F3] {0}", showAtlas? "Show TiledMap" : "Show Atlas"), new Vector2(20, 60), Color.White);
            spriteBatch.DrawString(font, String.Format("[+/-] MipLevel - ({0})", mipLevel), new Vector2(20, 80), Color.White);
            spriteBatch.End();

            base.Draw(gameTime);
        }

        private void DrawTiledMap(GameTime gameTime, Texture2D tiledMap, TextureAtlas atlas, Rectangle mipSize)
        {
            // setup tiledMapEffect
            var viewport = GraphicsDevice.Viewport;
            Matrix projection = Matrix.CreateOrthographicOffCenter(0, viewport.Width, viewport.Height, 0, 0, 1);
            Matrix halfPixelOffset = Matrix.Identity;
#if XNA
                halfPixelOffset = Matrix.CreateTranslation(-0.5f, -0.5f, 0);
#endif

            tiledMapEffect.World = Matrix.Identity;
            tiledMapEffect.View = Matrix.Identity;
            tiledMapEffect.Projection = halfPixelOffset * projection;

            tiledMapEffect.Texture2 = atlas.Texture;
            GraphicsDevice.SamplerStates[0] = SamplerState.PointWrap;

            // Draw tiledMap
            spriteBatch.Begin(0, BlendState.AlphaBlend, SamplerState.PointWrap, null, null, tiledMapEffect);
            spriteBatch.Draw(tiledMap, mipSize, Color.White);
            spriteBatch.End();
        }
    }
}
