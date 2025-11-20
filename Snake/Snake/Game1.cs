using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Snake
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        // Grid & rendering
        private int _cols;
        private int _rows;
        private int _cellSize;
        private Texture2D _pixel;
        private Color _snakeColor = Color.LimeGreen;
        private Color _background = Color.CornflowerBlue;

        // Snake state
        private readonly List<Point> _snake = new();
        private Direction _direction = Direction.Right;
        private Direction _nextDirection = Direction.Right;

        // Movement timing
        private float _moveTimer;
        private float _moveInterval = 0.10f; // seconds per step

        private enum Direction { Up, Down, Left, Right }

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // Window size already set in the file; keep current values (1000x600)
            _graphics.PreferredBackBufferWidth = _graphics.PreferredBackBufferWidth;
            _graphics.PreferredBackBufferHeight = _graphics.PreferredBackBufferHeight;
            _graphics.ApplyChanges();

            // Choose a cell size that divides the window nicely
            _cellSize = 20;
            _cols = _graphics.PreferredBackBufferWidth / _cellSize;
            _rows = _graphics.PreferredBackBufferHeight / _cellSize;

            // Initialize snake centered in grid
            _snake.Clear();
            int startX = _cols / 2;
            int startY = _rows / 2;
            for (int i = 0; i < 5; i++)
                _snake.Add(new Point(startX - i, startY));

            _direction = Direction.Right;
            _nextDirection = Direction.Right;
            _moveTimer = 0f;

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // 1x1 white pixel used to draw rectangles
            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });
        }

        protected override void Update(GameTime gameTime)
        {
            // Exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                Exit();
                return;
            }

            HandleInput();

            // Movement timer
            _moveTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_moveTimer >= _moveInterval)
            {
                _moveTimer -= _moveInterval;
                _direction = _nextDirection;
                StepSnake();
            }

            base.Update(gameTime);
        }

        private void HandleInput()
        {
            var ks = Keyboard.GetState();

            if (ks.IsKeyDown(Keys.Up) || ks.IsKeyDown(Keys.W))
            {
                if (_direction != Direction.Down) _nextDirection = Direction.Up;
            }
            else if (ks.IsKeyDown(Keys.Down) || ks.IsKeyDown(Keys.S))
            {
                if (_direction != Direction.Up) _nextDirection = Direction.Down;
            }
            else if (ks.IsKeyDown(Keys.Left) || ks.IsKeyDown(Keys.A))
            {
                if (_direction != Direction.Right) _nextDirection = Direction.Left;
            }
            else if (ks.IsKeyDown(Keys.Right) || ks.IsKeyDown(Keys.D))
            {
                if (_direction != Direction.Left) _nextDirection = Direction.Right;
            }
        }

        private void StepSnake()
        {
            var head = _snake[0];
            Point newHead = head;
            switch (_direction)
            {
                case Direction.Up: newHead = new Point(head.X, head.Y - 1); break;
                case Direction.Down: newHead = new Point(head.X, head.Y + 1); break;
                case Direction.Left: newHead = new Point(head.X - 1, head.Y); break;
                case Direction.Right: newHead = new Point(head.X + 1, head.Y); break;
            }

            // Wrap-around at edges so the snake continues moving on the other side
            newHead.X = (newHead.X % _cols + _cols) % _cols;
            newHead.Y = (newHead.Y % _rows + _rows) % _rows;

            _snake.Insert(0, newHead);
            _snake.RemoveAt(_snake.Count - 1);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(_background);

            _spriteBatch.Begin();

            // Draw snake segments
            foreach (var segment in _snake)
            {
                var rect = new Rectangle(segment.X * _cellSize, segment.Y * _cellSize, _cellSize, _cellSize);
                _spriteBatch.Draw(_pixel, rect, _snakeColor);
            }

            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
