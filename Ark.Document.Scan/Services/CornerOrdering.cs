using Ark.Document.Scan.Models;

namespace Ark.Document.Scan.Services;

// Standard "sum and diff" order-points method: sorts 4 arbitrary points into
// TL, TR, BR, BL so they line up with a fixed destination rectangle for a perspective warp.
internal static class CornerOrdering
{
    public static CornerPoint[] Order(IReadOnlyList<CornerPoint> points)
    {
        var topLeft = points.OrderBy(p => p.X + p.Y).First();
        var bottomRight = points.OrderByDescending(p => p.X + p.Y).First();
        var topRight = points.OrderByDescending(p => p.X - p.Y).First();
        var bottomLeft = points.OrderBy(p => p.X - p.Y).First();

        return new[] { topLeft, topRight, bottomRight, bottomLeft };
    }
}
