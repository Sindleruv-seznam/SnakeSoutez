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
        private Color _barrierColor = Color.DarkGray;
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

        // Barriers
        private HashSet<Point> _barrierCells = new();
        private int _barrierOffset = 8; // distance from each screen edge in cells (moved 2 segments toward center)

        // Shared movement timing (both snakes use this -> identical speed)
        private float _sharedMoveTimer;
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

            // Build barriers (vertical lines spanning top to bottom, offset from edges)
            BuildBarriers();

            // Initialize green snake (player 1) at right-top facing down (2 segments)
            _snake.Clear();
            int greenHeadX = Math.Max(1, _cols - 2);
            int greenHeadY = 1;
            if (greenHeadY >= _rows) greenHeadY = Math.Max(0, _rows - 2);
            _snake.Add(new Point(greenHeadX, greenHeadY));         // head
            _snake.Add(new Point(greenHeadX, greenHeadY - 1));     // tail above head
            _direction = Direction.Down;
            _nextDirection = Direction.Down;

            // Initialize red snake (player 2) at left-bottom facing up (2 segments)
            _snake2.Clear();
            int redHeadX = 1;
            int redHeadY = Math.Max(1, _rows - 2);
            if (redHeadY < 0) redHeadY = 0;
            _snake2.Add(new Point(redHeadX, redHeadY));            // head
            _snake2.Add(new Point(redHeadX, redHeadY + 1));        // tail below head
            _direction2 = Direction.Up;
            _nextDirection2 = Direction.Up;

            _sharedMoveTimer = 0f;
            _isGameOver = false;

            base.Initialize();
        }

        private void BuildBarriers()
        {
            _barrierCells.Clear();

            // ensure offsets are sane
            int leftX = Math.Max(0, Math.Min(_cols - 1, _barrierOffset));
            int rightX = Math.Max(0, Math.Min(_cols - 1, _cols - 1 - _barrierOffset));

            // if offsets collide, skip building barriers
            if (leftX >= rightX) return;

            for (int y = 0; y < _rows; y++)
            {
                _barrierCells.Add(new Point(leftX, y));
                _barrierCells.Add(new Point(rightX, y));
            }
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

            // Shared movement timer — both snakes move together at identical speed
            _sharedMoveTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_sharedMoveTimer >= _moveInterval)
            {
                _sharedMoveTimer -= _moveInterval;
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
            // Move snake1 (green)
            var head = _snake[0];
            Point newHead = head;
            switch (_direction)
            {
                case Direction.Up: newHead = new Point(head.X, head.Y - 1); break;
                case Direction.Down: newHead = new Point(head.X, head.Y + 1); break;
                case Direction.Left: newHead = new Point(head.X - 1, head.Y); break;
                case Direction.Right: newHead = new Point(head.X + 1, head.Y); break;
            }

            // If new head hits a barrier or is outside grid, end the game
            if (newHead.X < 0 || newHead.X >= _cols || newHead.Y < 0 || newHead.Y >= _rows || _barrierCells.Contains(newHead))
            {
                _isGameOver = true;
                return;
            }

            _snake.Insert(0, newHead);
            _snake.RemoveAt(_snake.Count - 1);

            // Move snake2 (red)
            var head2 = _snake2[0];
            Point newHead2 = head2;
            switch (_direction2)
            {
                case Direction.Up: newHead2 = new Point(head2.X, head2.Y - 1); break;
                case Direction.Down: newHead2 = new Point(head2.X, head2.Y + 1); break;
                case Direction.Left: newHead2 = new Point(head2.X - 1, head2.Y); break;
                case Direction.Right: newHead2 = new Point(head2.X + 1, head2.Y); break;
            }

            if (newHead2.X < 0 || newHead2.X >= _cols || newHead2.Y < 0 || newHead2.Y >= _rows || _barrierCells.Contains(newHead2))
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

            // Draw barriers
            foreach (var cell in _barrierCells)
            {
                var rect = new Rectangle(cell.X * _cellSize, cell.Y * _cellSize, _cellSize, _cellSize);
                _spriteBatch.Draw(_pixel, rect, _barrierColor);
            }

            // Draw snake segments (player 1 - green)
            foreach (var segment in _snake)
            {
                var rect = new Rectangle(segment.X * _cellSize, segment.Y * _cellSize, _cellSize, _cellSize);
                _spriteBatch.Draw(_pixel, rect, _snakeColor);
            }

            // Draw snake2 segments (player 2 - red)
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
