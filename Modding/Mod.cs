using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;

using JetBrains.Annotations;

using Godot.Modding.Patching;
using Godot.Serialization;

namespace Godot.Modding
{
    /// <summary>
    /// Represents a modular component loaded at runtime, with its own assemblies, resource packs, and data.
    /// </summary>
    [PublicAPI]
    public sealed record Mod
    {
        /// <summary>
        /// Initializes a new <see cref="Mod"/> using <paramref name="metadata"/>.
        /// </summary>
        /// <param name="metadata">The <see cref="Metadata"/> to use. Assemblies, resource packs, and data are all loaded according to the directory specified in the metadata.</param>
        public Mod(Metadata metadata)
        {
            this.Meta = metadata;
            this.LoadResources();
            this.Data = this.LoadData();
            this.Patches = this.LoadPatches();
            this.Assemblies = this.LoadAssemblies();
        }
        
        /// <summary>
        /// The metadata of the <see cref="Mod"/>, such as its ID, name, load order, etc.
        /// </summary>
        public Metadata Meta
        {
            get;
        }
        
        /// <summary>
        /// The assemblies of the <see cref="Mod"/>.
        /// </summary>
        public IEnumerable<Assembly> Assemblies
        {
            get;
        }
        
        /// <summary>
        /// The XML data of the <see cref="Mod"/> if any, combined into a single <see cref="XmlDocument"/>.
        /// </summary>
        public XmlDocument? Data
        {
            get;
        }
        
        /// <summary>
        /// The patches applied by the <see cref="Mod"/>.
        /// </summary>
        public IEnumerable<IPatch> Patches
        {
            get;
        }
        
        private IEnumerable<Assembly> LoadAssemblies()
        {
            string assembliesPath = $"{this.Meta.Directory}{System.IO.Path.DirectorySeparatorChar}Assemblies";
            
            return System.IO.Directory.Exists(assembliesPath)
                ? System.IO.Directory.GetFiles(assembliesPath, "*dll", SearchOption.AllDirectories).Select(Assembly.LoadFile)
                : Enumerable.Empty<Assembly>();
        }
        
        private XmlDocument? LoadData()
        {
            IEnumerable<XmlDocument> documents = this.LoadDocuments().ToArray();
            if (!documents.Any())
            {
                return null;
            }
            
            XmlDocument data = new();
            
            XmlElement root = data.CreateElement("Data");
            data.AppendChild(root);
            data.InsertBefore(data.CreateXmlDeclaration("1.0", "UTF-8", null), root);
            
            documents
                .SelectMany(document => document.Cast<XmlNode>())
                .Where(node => node is not XmlDeclaration)
                .ForEach(node => root.AppendChild(data.ImportNode(node, true)));
            
            return data;
        }
        
        private IEnumerable<XmlDocument> LoadDocuments()
        {
            string dataPath = $"{this.Meta.Directory}{System.IO.Path.DirectorySeparatorChar}Data";
            
            if (!System.IO.Directory.Exists(dataPath))
            {
                yield break;
            }
            
            foreach (string xmlPath in System.IO.Directory.GetFiles(dataPath, "*.xml", SearchOption.AllDirectories))
            {
                XmlDocument document = new();
                document.Load(xmlPath);
                yield return document;
            }
        }
        
        private void LoadResources()
        {
            string resourcesPath = $"{this.Meta.Directory}{System.IO.Path.DirectorySeparatorChar}Resources";
            
            if (!System.IO.Directory.Exists(resourcesPath))
            {
                return;
            }
            
            string? invalidResourcePath = System.IO.Directory.GetFiles(resourcesPath, "*.pck", SearchOption.AllDirectories).FirstOrDefault(resourcePath => !ProjectSettings.LoadResourcePack(resourcePath));
            if (invalidResourcePath is not null)
            {
                throw new ModLoadException(this.Meta.Directory, $"Error loading resource pack at {invalidResourcePath}");
            }
        }
        
        private IEnumerable<IPatch> LoadPatches()
        {
            string patchesPath = $"{this.Meta.Directory}{System.IO.Path.DirectorySeparatorChar}Patches";
            
            if (!System.IO.Directory.Exists(patchesPath))
            {
                yield break;
            }
            
            Serializer serializer = new();
            XmlDocument document = new();
            foreach (string patchPath in System.IO.Directory.GetFiles(patchesPath, "*.xml", SearchOption.AllDirectories))
            {
                document.Load(patchPath);
                if (document.DocumentElement is not null)
                {
                    yield return serializer.Deserialize(document.DocumentElement) as IPatch ?? throw new ModLoadException(this.Meta.Directory, $"Invalid patch at {patchPath}");
                }
            }
        }
        
        /// <summary>
        /// Represents the metadata of a <see cref="Mod"/>, such as its unique ID, name, author, load order, etc.
        /// </summary>
        [PublicAPI]
        public sealed record Metadata
        {
            [UsedImplicitly]
            private Metadata()
            {
            }
            
            /// <summary>
            /// The directory where the <see cref="Metadata"/> was loaded from.
            /// </summary>
            [Serialize]
            public string Directory
            {
                get;
                [UsedImplicitly]
                private set;
            } = null!;
            
            /// <summary>
            /// The unique ID of the <see cref="Mod"/>.
            /// </summary>
            [Serialize]
            public string Id
            {
                get;
                [UsedImplicitly]
                private set;
            } = null!;
            
            /// <summary>
            /// The name of the <see cref="Mod"/>.
            /// </summary>
            [Serialize]
            public string Name
            {
                get;
                [UsedImplicitly]
                private set;
            } = null!;
            
            /// <summary>
            /// The individual or group that created the <see cref="Mod"/>.
            /// </summary>
            [Serialize]
            public string Author
            {
                get;
                [UsedImplicitly]
                private set;
            } = null!;
            
            /// <summary>
            /// The unique IDs of all other <see cref="Mod"/>s that the <see cref="Mod"/> depends on.
            /// </summary>
            public IEnumerable<string> Dependencies
            {
                get;
                [UsedImplicitly]
                private set;
            } = Enumerable.Empty<string>();
            
            /// <summary>
            /// The unique IDs of all other <see cref="Mod"/>s that should be loaded before the <see cref="Mod"/>.
            /// </summary>
            public IEnumerable<string> Before
            {
                get;
                [UsedImplicitly]
                private set;
            } = Enumerable.Empty<string>();
            
            /// <summary>
            /// The unique IDs of all other <see cref="Mod"/>s that should be loaded after the <see cref="Mod"/>.
            /// </summary>
            public IEnumerable<string> After
            {
                get;
                [UsedImplicitly]
                private set;
            } = Enumerable.Empty<string>();
            
            /// <summary>
            /// The unique IDs of all other <see cref="Mod"/>s that are incompatible with the <see cref="Mod"/>.
            /// </summary>
            public IEnumerable<string> Incompatible
            {
                get;
                [UsedImplicitly]
                private set;
            } = Enumerable.Empty<string>();
            
            /// <summary>
            /// Loads a <see cref="Metadata"/> from <paramref name="directoryPath"/>.
            /// </summary>
            /// <param name="directoryPath">The directory path. It must contain a "Mod.xml" file inside it with valid metadata.</param>
            /// <returns>A <see cref="Metadata"/> loaded from <paramref name="directoryPath"/>.</returns>
            /// <exception cref="ModLoadException">Thrown if the metadata file does not exist, or the metadata is invalid, or if there is another unexpected issue while trying to load the metadata.</exception>
            public static Metadata Load(string directoryPath)
            {
                string metadataFilePath = $"{directoryPath}{System.IO.Path.DirectorySeparatorChar}Mod.xml";
                
                if (!System.IO.File.Exists(metadataFilePath))
                {
                    throw new ModLoadException(directoryPath, new FileNotFoundException($"Mod metadata file {metadataFilePath} does not exist"));
                }
                
                try
                {
                    XmlDocument document = new();
                    document.Load(metadataFilePath);
                    if (document.DocumentElement?.Name is not "Mod")
                    {
                        throw new ModLoadException(directoryPath, "Root XML node \"Mod\" for serializing mod metadata does not exist");
                    }
                    
                    XmlNode directoryNode = document.CreateNode(XmlNodeType.Element, "Directory", null);
                    directoryNode.InnerText = directoryPath;
                    document.DocumentElement.AppendChild(directoryNode);
                    
                    return new Serializer().Deserialize<Metadata>(document.DocumentElement)!;
                }
                catch (Exception exception) when (exception is not ModLoadException)
                {
                    throw new ModLoadException(directoryPath, exception);
                }
            }
            
            [AfterDeserialization]
            private void IsValid()
            {
                // Check that the incompatible, load before, and load after lists don't have anything in common or contain the mod's own ID
                bool invalidLoadOrder = this.Incompatible
                    .Prepend(this.Id)
                    .Concat(this.Before)
                    .Concat(this.After)
                    .Indistinct()
                    .Any();
                // Check that the dependency and incompatible lists don't have anything in common
                bool invalidDependencies = this.Dependencies
                    .Intersect(this.Incompatible)
                    .Any();
                if (invalidLoadOrder || invalidDependencies)
                {
                    throw new ModLoadException(this.Directory, "Invalid metadata");
                }
            }
        }
    }
    public static partial class EnumerableExtensions
    {
        /// <summary>
        /// Finds elements that are contained more than once in <paramref name="source"/>.
        /// </summary>
        /// <param name="source">The <see cref="IEnumerable{T}"/> to search in.</param>
        /// <typeparam name="T">The <see cref="Type"/> of elements in <paramref name="source"/>.</typeparam>
        /// <returns>An <see cref="IEnumerable{T}"/> of all elements that appear more than once in <paramref name="source"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="source"/> is <see langword="null"/>.</exception>
        public static IEnumerable<T> Indistinct<T>(this IEnumerable<T> source)
        {
            return source is null
                ? throw new ArgumentNullException(nameof(source))
                : EnumerableExtensions.IndistinctIterator(source).Distinct();
        }
        
        private static IEnumerable<T> IndistinctIterator<T>(IEnumerable<T> source)
        {
            HashSet<T> visited = new();
            foreach (T element in source)
            {
                if (!visited.Add(element))
                {
                    yield return element;
                }
            }
        }
    }
}