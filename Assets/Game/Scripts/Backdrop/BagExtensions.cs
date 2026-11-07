using System;
using System.Collections.Generic;

namespace HoldIt.Backdrop
{
    public static class BagExtensions
    {
        /// <summary>
        /// Draws from a shuffle bag of the numbers 0 to <paramref name="count"/> - 1: every one comes up once before any comes
        /// up again, in a random order. The empty bag is refilled and shuffled first, and its first draw is never
        /// <paramref name="avoid"/> (the last one drawn), so no number comes twice in a row across the turn of a bag. The
        /// list is the bag's whole state; pass the same one each time.
        /// </summary>
        public static int Draw(this List<int> bag, int count, int avoid, Random random)
        {
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count), count, "There must be something to draw.");

            if (bag.Count == 0)
            {
                for (var i = 0; i < count; i++)
                    bag.Add(i);

                for (var i = count - 1; i > 0; i--)
                {
                    var j = random.Next(i + 1);
                    (bag[i], bag[j]) = (bag[j], bag[i]);
                }

                // The draw takes from the end; keep the number just drawn away from there.
                if (count > 1 && bag[count - 1] == avoid)
                    (bag[count - 1], bag[0]) = (bag[0], bag[count - 1]);
            }

            var last = bag.Count - 1;
            var drawn = bag[last];
            bag.RemoveAt(last);
            return drawn;
        }
    }
}
