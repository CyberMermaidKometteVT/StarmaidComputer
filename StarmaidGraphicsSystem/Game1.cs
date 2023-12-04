using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace StarmaidGraphicsSystem
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;


        private Model thalassaDroneModel;
        private Effect thalassaDroneShell;
        private Effect thalassaDroneLights;


        private Matrix world;
        private Matrix view;
        private Matrix proj;

        private Vector3 camera = new();

        private Vector3 position = new(0, 0, 0);

        private Vector3 cameraPosition = new();
        private Vector3 cameraLookAtVector = new();
        private Vector3 cameraUpVector = new(0, 1, 0);

        private Viewport viewport = new(0, 0, 640, 480);

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here


            // Load fbx of Thalassa
            thalassaDroneModel = Content.Load<Model>("ThalassaDrone");

            // Load shader effects
            thalassaDroneShell = Content.Load<Effect>("ThalassaDroneShell");
            thalassaDroneLights = Content.Load<Effect>("ThalassaDroneLights");


            // World calculations
            world = Matrix.CreateTranslation(position);
            view = Matrix.CreateLookAt(cameraPosition, cameraLookAtVector, cameraUpVector);
            proj = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, viewport.AspectRatio, 1, 200);

        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();


            // TODO: Add your update logic here



            base.Update(gameTime);

        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);



            foreach (var mesh in thalassaDroneModel.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.EnableDefaultLighting();
                    effect.PreferPerPixelLighting = true;
                    effect.TextureEnabled = true;
                    effect.Alpha = 1;
                    effect.View = Matrix.CreateLookAt(new Vector3(-60, 30, 0), new Vector3(0, 30, 0), Vector3.Up);
                    effect.Projection = proj;
                    effect.AmbientLightColor = new Vector3(0.2f, 0.1f, 0.3f);
                    effect.DiffuseColor = new Vector3(0.96f, 0.98f, 0.89f);
                }

                mesh.Draw();
            }



            // TODO: Add your drawing code here

            base.Draw(gameTime);
        }
    }
}