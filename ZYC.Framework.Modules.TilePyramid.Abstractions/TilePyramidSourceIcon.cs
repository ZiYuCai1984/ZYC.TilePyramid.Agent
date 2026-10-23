namespace ZYC.Framework.Modules.TilePyramid.Abstractions;

// ReSharper disable StringLiteralTypo
[AttributeUsage(AttributeTargets.Field)]
public class TilePyramidSourceIcon : Attribute
{
    public const string Tiff =
        "data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCAyNCAyNCI+PHRleHQgeD0iMTIiIHk9IjE2IiBmb250LWZhbWlseT0ic3lzdGVtLXVpLHNhbnMtc2VyaWYiIGZvbnQtc2l6ZT0iMTEiIGZvbnQtd2VpZ2h0PSJib2xkIiB0ZXh0LWFuY2hvcj0ibWlkZGxlIiBmaWxsPSJjdXJyZW50Q29sb3IiPlRJRjwvdGV4dD48L3N2Zz4=";


    public const string Svs =
        "data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCAyNCAyNCI+PHRleHQgeD0iMTIiIHk9IjE2IiBmb250LWZhbWlseT0ic3lzdGVtLXVpLHNhbnMtc2VyaWYiIGZvbnQtc2l6ZT0iMTEiIGZvbnQtd2VpZ2h0PSJib2xkIiB0ZXh0LWFuY2hvcj0ibWlkZGxlIiBmaWxsPSJjdXJyZW50Q29sb3IiPlNWUzwvdGV4dD48L3N2Zz4=";

    public const string Mandelbrot = "Turtle";

    public const string Function = "ChartLine";


    public const string Hamamatsu = "Microscope";

    public const string Mirax = "Microscope";


    public const string DeepZoom =
        "data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHZpZXdCb3g9IjAgMCAyNCAyNCI+PHRleHQgeD0iMTIiIHk9IjE2IiBmb250LWZhbWlseT0ic3lzdGVtLXVpLHNhbnMtc2VyaWYiIGZvbnQtc2l6ZT0iMTEiIGZvbnQtd2VpZ2h0PSJib2xkIiB0ZXh0LWFuY2hvcj0ibWlkZGxlIiBmaWxsPSJjdXJyZW50Q29sb3IiPkRaSTwvdGV4dD48L3N2Zz4=";


    public TilePyramidSourceIcon(string icon)
    {
        Icon = icon;
    }

    public string Icon { get; }
}
