namespace Augustus;

using System.IO;
using System.Threading.Tasks;

public partial class APISimulator
{
    internal class FileManager
    {
        private readonly string cacheFolderPath;

        public FileManager(string cacheFolderPath)
        {
            this.cacheFolderPath = cacheFolderPath;
        }

        public async Task WriteToFileAsync(string filename, string content)
        {
            string fullPath = Path.Combine(cacheFolderPath, filename);
            await File.WriteAllTextAsync(fullPath, content);
        }

        // Add more file operations like read, delete, etc. if necessary
    }

}
