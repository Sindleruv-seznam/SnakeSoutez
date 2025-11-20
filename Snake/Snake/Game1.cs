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
        private Color _snake2Color = Color.Red;
        private Color _background = Color.CornflowerBlue;

        // Snake state (player 1)
        private readonly List<Point> _snake = new();
        private Direction _direction = Direction.Right;
        private Direction _nextDirection = Direction.Right;

        // Snake state (player 2 - WASD)
        private readonly List<Point> _snake2 = new();
        private Direction _direction2 = Direction.Left;
        private Direction _nextDirection2 = Direction.Left;

        private bool _isGameOver;

        // Movement timing
        private float _moveTimer;
        private float _moveInterval = 0.15f; // seconds per step

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

            // Initialize snake centered in grid (player 1, length: 2 segments)
            _snake.Clear();
            int startX = _cols / 2;
            int startY = _rows / 2;
            for (int i = 0; i < 2; i++)
                _snake.Add(new Point(startX - i, startY));
            _direction = Direction.Right;
            _nextDirection = Direction.Right;

            // Initialize snake2 (player 2) near upper-left quarter (length: 2 segments)
            _snake2.Clear();
            int startX2 = Math.Max(1, _cols / 4);
            int startY2 = Math.Max(1, _rows / 4);
            for (int i = 0; i < 2; i++)
                _snake2.Add(new Point(startX2 + i, startY2)); // oriented left
            _direction2 = Direction.Left;
            _nextDirection2 = Direction.Left;

            _moveTimer = 0f;
            _isGameOver = false;

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

            // Allow restart with Space when game over
            if (_isGameOver)
            {
                if (Keyboard.GetState().IsKeyDown(Keys.Space))
                    Initialize();
                return;
            }

            HandleInput();

            // Movement timer
            _moveTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_moveTimer >= _moveInterval)
            {
                _moveTimer -= _moveInterval;
                _direction = _nextDirection;
                _direction2 = _nextDirection2;
                StepSnakes();
            }

            base.Update(gameTime);
        }

        private void HandleInput()
        {
            var ks = Keyboard.GetState();

            // Player 1 - Arrow keys only
            if (ks.IsKeyDown(Keys.Up))
            {
                if (_direction != Direction.Down) _nextDirection = Direction.Up;
            }
            else if (ks.IsKeyDown(Keys.Down))
            {
                if (_direction != Direction.Up) _nextDirection = Direction.Down;
            }
            else if (ks.IsKeyDown(Keys.Left))
            {
                if (_direction != Direction.Right) _nextDirection = Direction.Left;
            }
            else if (ks.IsKeyDown(Keys.Right))
            {
                if (_direction != Direction.Left) _nextDirection = Direction.Right;
            }

            // Player 2 - WASD
            if (ks.IsKeyDown(Keys.W))
            {
                if (_direction2 != Direction.Down) _nextDirection2 = Direction.Up;
            }
            else if (ks.IsKeyDown(Keys.S))
            {
                if (_direction2 != Direction.Up) _nextDirection2 = Direction.Down;
            }
            else if (ks.IsKeyDown(Keys.A))
            {
                if (_direction2 != Direction.Right) _nextDirection2 = Direction.Left;
            }
            else if (ks.IsKeyDown(Keys.D))
            {
                if (_direction2 != Direction.Left) _nextDirection2 = Direction.Right;
            }
        }

        private void StepSnakes()
        {
            // Move snake1
            var head = _snake[0];
            Point newHead = head;
            switch (_direction)
            {
                case Direction.Up: newHead = new Point(head.X, head.Y - 1); break;
                case Direction.Down: newHead = new Point(head.X, head.Y + 1); break;
                case Direction.Left: newHead = new Point(head.X - 1, head.Y); break;
                case Direction.Right: newHead = new Point(head.X + 1, head.Y); break;
            }

            // If new head is outside grid, end the game
            if (newHead.X < 0 || newHead.X >= _cols || newHead.Y < 0 || newHead.Y >= _rows)
            {
                _isGameOver = true;
                return;
            }

            _snake.Insert(0, newHead);
            _snake.RemoveAt(_snake.Count - 1);

            // Move snake2
            var head2 = _snake2[0];
            Point newHead2 = head2;
            switch (_direction2)
            {
                case Direction.Up: newHead2 = new Point(head2.X, head2.Y - 1); break;
                case Direction.Down: newHead2 = new Point(head2.X, head2.Y + 1); break;
                case Direction.Left: newHead2 = new Point(head2.X - 1, head2.Y); break;
                case Direction.Right: newHead2 = new Point(head2.X + 1, head2.Y); break;
            }

            if (newHead2.X < 0 || newHead2.X >= _cols || newHead2.Y < 0 || newHead2.Y >= _rows)
            {
                _isGameOver = true;
                return;
            }

            _snake2.Insert(0, newHead2);
            _snake2.RemoveAt(_snake2.Count - 1);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(_background);

            _spriteBatch.Begin();

            // Draw snake segments (player 1)
            foreach (var segment in _snake)
            {
                var rect = new Rectangle(segment.X * _cellSize, segment.Y * _cellSize, _cellSize, _cellSize);
                _spriteBatch.Draw(_pixel, rect, _snakeColor);
            }

            // Draw snake2 segments (player 2)
            foreach (var segment in _snake2)
            {
                var rect = new Rectangle(segment.X * _cellSize, segment.Y * _cellSize, _cellSize, _cellSize);
                _spriteBatch.Draw(_pixel, rect, _snake2Color);
            }

            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
