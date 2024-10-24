using LeagueToolkit.Core.Wad;
using LeagueToolkit.Hashing;
using System;
using System.IO;

namespace Obsidian.Utils
{
    public static class WadUtils
    {
        public static void SaveChunk(WadFile wad, WadChunk chunk, string chunkPath, string saveDirectory)
        {
            try
            {
                // Crear la ruta completa para el archivo
                string filePath = CreateChunkFilePath(saveDirectory, chunkPath);
                
                // Verificar si la ruta es válida y accesible
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    Console.WriteLine("Error: La ruta del archivo es inválida.");
                    return;
                }

                // Verificar si el directorio existe y crearlo si es necesario
                string directoryPath = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                // Guardar el archivo en la ruta especificada
                using FileStream chunkFileStream = File.Create(filePath);
                using Stream chunkStream = wad.OpenChunk(chunk);

                // Copiar el contenido del chunk al archivo
                chunkStream.CopyTo(chunkFileStream);

                Console.WriteLine($"Archivo guardado exitosamente en {filePath}");
            }
            catch (UnauthorizedAccessException ex)
            {
                // Manejar el error de acceso denegado
                Console.WriteLine($"Error: No se tiene acceso a la ruta {saveDirectory}. Detalles: {ex.Message}");
            }
            catch (IOException ex)
            {
                // Manejar errores de entrada/salida
                Console.WriteLine($"Error de entrada/salida al guardar el archivo: {ex.Message}");
            }
            catch (Exception ex)
            {
                // Manejar cualquier otro tipo de excepción
                Console.WriteLine($"Error inesperado al guardar el archivo: {ex.Message}");
            }
        }

        public static string CreateChunkFilePath(string saveDirectory, string chunkPath)
        {
            // Crear la ruta usando el directorio de guardado y la ruta del chunk
            string naivePath = Path.Join(saveDirectory, chunkPath);

            // Verificar si la ruta es demasiado larga y ajustar si es necesario
            if (naivePath.Length <= 260)
                return naivePath;

            // Si la ruta es demasiado larga, usar un hash en el nombre del archivo
            return Path.Join(
                saveDirectory,
                string.Format(
                    "{0:x16}{1}",
                    XxHash64Ext.Hash(chunkPath.ToLowerInvariant()),
                    Path.GetExtension(chunkPath)
                )
            );
        }
    }
}
