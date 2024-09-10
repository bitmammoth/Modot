using System.Linq;
using System.Xml;

using JetBrains.Annotations;

using Godot.Serialization;
using System;
using System.Collections.Generic;

namespace Godot.Modding.Patching
{
    /// <summary>
    /// An <see cref="IPatch"/> that selects descendants of an <see cref="XmlNode"/> according to an XPath string and applies a separate patch on them.
    /// </summary>
    [PublicAPI]
    public class TargetedPatch : IPatch
    {
        /// <summary>
        /// Initialises a new <see cref="TargetedPatch"/> with the specified parameters.
        /// </summary>
        /// <param name="targets">An XPath string that specifies descendant <see cref="XmlNode"/>s to apply <paramref name="patch"/> on.</param>
        /// <param name="patch">The patch to apply on all <see cref="XmlNode"/>s selected by <paramref name="targets"/>.</param>
        public TargetedPatch(string targets, IPatch patch)
        {
            this.Targets = targets;
            this.Patch = patch;
        }
        
        [UsedImplicitly]
        private TargetedPatch()
        {
        }
        
        /// <summary>
        /// The targets to apply the <see cref="TargetedPatch"/> on, in the form of an XPath.
        /// </summary>
        [Serialize]
        public string Targets
        {
            get;
            [UsedImplicitly]
            private set;
        } = null!;
        
        /// <summary>
        /// The patch to apply on <see cref="XmlNode"/>s that match <see cref="Targets"/>.
        /// </summary>
        [Serialize]
        public IPatch Patch
        {
            get;
            [UsedImplicitly]
            private set;
        } = null!;
        
        /// <summary>
        /// Applies <see cref="Patch"/> to all <see cref="XmlNode"/>s under <paramref name="data"/> that match <see cref="Targets"/>.
        /// </summary>
        /// <param name="data">The <see cref="XmlNode"/> to apply the patch on.</param>
        public void Apply(XmlNode data)
        {
            data.SelectNodes(this.Targets)?
                .Cast<XmlNode>()
                .ForEach(this.Patch.Apply);
        }
        
    }
    public static partial class EnumerableExtensions
    {
        /// <summary>
        /// Filters all elements from <paramref name="source"/> that are not <see langword="null"/>.
        /// </summary>
        /// <param name="source">The <see cref="IEnumerable{T}"/> to search in.</param>
        /// <typeparam name="T">The <see cref="Type"/> of element in <paramref name="source"/>.</typeparam>
        /// <returns>An <see cref="IEnumerable{T}"/> of all non-<see langword="null"/> elements from <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is <see langword="null"/>.</exception>
        [Pure]
        public static IEnumerable<T> NotNull<T>(this IEnumerable<T?> source)
        {
            return source.Where(element => element is not null)!;
        }
    }
    public static partial class EnumerableExtensions
    {
        /// <summary>
        /// Executes <paramref name="function"/> over every item in <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The <see cref="IEnumerable{T}"/> of items to execute <paramref name="function"/> on.</param>
        /// <param name="function">The <see cref="Action{T}"/> to invoke for each item in <paramref name="source"/>.</param>
        /// <typeparam name="T">The <see cref="Type"/> of item in <paramref name="source"/>.</typeparam>
        /// <exception cref="ArgumentNullException">Thrown if either <paramref name="source"/> or <paramref name="function"/> is <see langword="null"/>.</exception>
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> function)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (function is null)
            {
                throw new ArgumentNullException(nameof(function));
            }
            
            foreach (T item in source)
            {
                function.Invoke(item);
            }
        }
        
        /// <summary>
        /// Executes <paramref name="function"/> over every item in <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The <see cref="IEnumerable{T}"/> of items to execute <paramref name="function"/> on.</param>
        /// <param name="function">The function to invoke for each item and its index in <paramref name="source"/>.</param>
        /// <typeparam name="T">The <see cref="Type"/> of item in <paramref name="source"/>.</typeparam>
        /// <exception cref="ArgumentNullException">Thrown if either <paramref name="source"/> or <paramref name="function"/> is <see langword="null"/>.</exception>
        public static void ForEach<T>(this IEnumerable<T> source, Action<T, int> function)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (function is null)
            {
                throw new ArgumentNullException(nameof(function));
            }
            
            int index = 0;
            foreach (T item in source)
            {
                function.Invoke(item, index);
                index += 1;
            }
        }
    }
}