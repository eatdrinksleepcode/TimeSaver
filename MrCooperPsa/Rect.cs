using System;
using System.Drawing;

namespace MrCooperPsa {
    internal struct Rect {
        public Point Position { get; set; }
        public Size Size { get; set; }

        public Tuple<Rect, Rect> SliceBottom(int height) {
            var top = new Rect {
                Position = Position,
                Size = new Size(Size.Width, Size.Height - height)

            }; 
            return Tuple.Create(
                top,
                new Rect {
                    Position = new Point(Position.X, Position.Y + top.Size.Height),
                    Size = new Size(Size.Width, height)
                }
            );
        }

        public Tuple<Rect, Rect> SliceRight(int width) {
            var left = new Rect {
                Position = Position,
                Size = new Size(Size.Width - width, Size.Height)

            }; 
            return Tuple.Create(
                left,
                new Rect {
                    Position = new Point(Position.X + left.Size.Width, Position.Y),
                    Size = new Size(width, Size.Height)
                }
            );
        }

        public Tuple<Rect, Rect> SplitVertical() {
            return SliceRight(Size.Width / 2);
        }
    }
}