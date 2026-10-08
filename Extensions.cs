using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DogPlus
{
    public static class Extensions
    {
        public static readonly Random rng = new Random(DateTime.Now.Millisecond);

        /// <summary>
        /// Returns a random element from the given source.
        /// </summary>
        /// <typeparam name="T">The type of collection.</typeparam>
        /// <param name="source">The source to pick an element from.</param>
        /// <returns>Returns a random element from the source.</returns>
        public static T RandomElement<T>(this IEnumerable<T> source)
        {
            if (source == null)
                throw new InvalidOperationException("Cannot select a random element from a null source.");

            var list = source as IList<T> ?? source.ToList();

            if (list.Count == 0)
                throw new InvalidOperationException("Cannot select a random element from an empty collection.");

            return list[rng.Next(list.Count)];
        }

        /// <summary>
        /// Returns a number between 1 and the number entered.
        /// </summary>
        /// <param name="number">The number of sides on the dice.</param>
        /// <returns>Returns a number between 1-number.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static int RandomPositiveNumber(this int number)
        {
            if(number <= 0)
            {
                return 1;
            }

            return rng.Next(1, number + 1);
        }
    }
}
