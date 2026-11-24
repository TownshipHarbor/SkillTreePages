using System;

namespace SkillTreePagesQuest
{
    internal interface IBits
    {
        uint Index { get; }
        uint Total { get; }
        uint Read(int count);
    }

    internal enum Shape
    {
        Stock,
        Pages,
        PagesLoose
    }

    internal struct Header
    {
        internal int Limit;
        internal int Page;
        internal int Extent;
        internal int Copies;
    }

    internal static class Wire
    {
        internal const int Capacity = 64;
        internal const int PageSize = 8;
        internal const int HeaderBits = 96;
        internal const int MaxCopies = 3;
        internal const int PointsBits = 6;
        internal const int IdBits = 17;
        internal const int BarBits = 17;

        internal static int PageCount(int extent)
        {
            return Math.Max(1, (extent + PageSize - 1) / PageSize);
        }

        internal static bool Visible(int index, int page, int extent)
        {
            return index < extent && index / PageSize == page;
        }

        internal static bool CanBuy(int owned, int limit)
        {
            return owned < limit;
        }

        internal static bool ValidHeader(uint limit, uint page, uint extent)
        {
            return limit >= 1 && limit <= Capacity && extent >= limit && extent <= Capacity && page < (uint)PageCount((int)extent);
        }

        internal static Shape Classify(Func<IBits> open, int rest, int knownCopies, out Header header)
        {
            header = new Header();
            for (int copies = 1; copies <= MaxCopies; copies++)
            {
                if (knownCopies > 0 && copies != knownCopies) continue;
                Header h;
                Shape s = Try(open(), copies, rest, out h);
                if (s == Shape.Pages || (s == Shape.PagesLoose && knownCopies > 0))
                {
                    header = h;
                    return s;
                }
            }
            return Shape.Stock;
        }

        static Shape Try(IBits s, int copies, int rest, out Header header)
        {
            header = new Header();
            uint start = s.Index, total = s.Total;
            if (start > total || total - start < (uint)(HeaderBits * copies + PointsBits + Capacity)) return Shape.Stock;
            uint limit = s.Read(32), page = s.Read(32), extent = s.Read(32);
            if (!ValidHeader(limit, page, extent)) return Shape.Stock;
            for (int c = 1; c < copies; c++)
            {
                if (s.Read(32) != limit || s.Read(32) != page || s.Read(32) != extent) return Shape.Stock;
            }
            s.Read(PointsBits);
            for (int i = 0; i < Capacity; i++)
            {
                if (s.Index + 1 > total) return Shape.Stock;
                if (s.Read(1) == 0) continue;
                if (i >= extent || s.Index + IdBits > total) return Shape.Stock;
                s.Read(IdBits);
            }
            header.Limit = (int)limit;
            header.Page = (int)page;
            header.Extent = (int)extent;
            header.Copies = copies;
            if (rest < 0) return Shape.PagesLoose;
            long pad = (long)total - s.Index - rest;
            return pad >= 0 && pad < 8 ? Shape.Pages : Shape.Stock;
        }
    }
}
