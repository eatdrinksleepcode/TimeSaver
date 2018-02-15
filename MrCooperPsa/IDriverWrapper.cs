using System;
using System.Drawing;

namespace MrCooperPsa
{
    public interface IDriverWrapper : IDisposable {
        void SetScreenSize(Point position, Size size);
    }
}