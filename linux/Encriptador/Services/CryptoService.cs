using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Encriptador.Services;

/// <summary>
/// Cifra/descifra archivos y carpetas con AES-256-GCM (autenticado, por
/// bloques de 64 KB) derivando la clave de la contraseña mediante PBKDF2
/// (600.000 iteraciones). Un archivo individual, o varios archivos sueltos
/// elegidos a la vez, o una carpeta completa (recursiva) se empaquetan en
/// un único ".enc". Las carpetas guardan un manifiesto (nombres, tamaños y
/// estructura) cifrado aparte de los datos: eso permite leer el índice y
/// abrir/extraer un archivo puntual sin descifrar el resto. Cada bloque de
/// 64 KB lleva su propio nonce y tag de autenticación, así que una
/// contraseña incorrecta o un archivo manipulado se detectan de inmediato
/// (no dependen, como en CBC, de que falle el relleno al final).
/// Por defecto se borra el original (archivo o carpeta entera) una vez
/// encriptado con éxito, salvo que se pida conservarlo; el borrado puede
/// ser "seguro" (sobrescribiendo el contenido antes de borrar).
/// </summary>
public static class CryptoService
{
    private const int SaltSize = 16;
    private const int KeySize = 32; // AES-256
    private const int Pbkdf2Iterations = 600_000;
    private const byte TipoArchivo = 1;
    private const byte TipoCarpeta = 2;
    private const int MaxEntradas = 5_000_000;
    private const int MaxLongitudRuta = 32_000;
    private const int MaxLongitudManifiesto = 200_000_000;
    private const int TamanoChunk = 64 * 1024;
    private const int NonceSize = 12;
    private const int TagSize = 16;

    public const string Extension = ".enc";

    public static bool EsArchivoEncriptado(string ruta) =>
        ruta.EndsWith(Extension, StringComparison.OrdinalIgnoreCase);

    /// <summary>Indica si un .enc contiene una carpeta (sin necesitar la contraseña: el tipo va sin cifrar).</summary>
    public static bool EsContenedorDeCarpeta(string rutaEnc)
    {
        using var fs = File.OpenRead(rutaEnc);
        if (fs.Length < SaltSize + 1)
            return false;
        fs.Seek(SaltSize, SeekOrigin.Begin);
        return fs.ReadByte() == TipoCarpeta;
    }

    // ===================== Encriptar =====================

    /// <summary>Encripta un archivo o una carpeta completa en un único ".enc".</summary>
    public static (int ArchivosIncluidos, string RutaDestino) Encriptar(
        string ruta, string password, bool conservarOriginal = false, bool borradoSeguro = false,
        IProgress<(int Actual, int Total)>? progreso = null)
    {
        if (Directory.Exists(ruta))
        {
            var raiz = ruta.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var destino = raiz + Extension;
            var fuente = Directory.EnumerateFiles(raiz, "*", SearchOption.AllDirectories)
                .Select(a => (RutaCompleta: a, RutaRelativa: Path.GetRelativePath(raiz, a).Replace('\\', '/')))
                .ToList();

            var cantidad = EncriptarConjunto(fuente, destino, password, progreso);

            if (!conservarOriginal)
                BorrarCarpeta(raiz, borradoSeguro);

            return (cantidad, destino);
        }

        if (File.Exists(ruta))
        {
            var destino = ruta + Extension;
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var clave = DerivarClave(password, salt);

            using (var salida = File.Create(destino))
            {
                salida.Write(salt);
                salida.WriteByte(TipoArchivo);
                var longitud = new FileInfo(ruta).Length;
                EscribirInt64(salida, longitud);

                using var entrada = File.OpenRead(ruta);
                CifrarStream(entrada, salida, clave, longitud);
            }

            if (!conservarOriginal)
                BorrarArchivo(ruta, borradoSeguro);

            return (1, destino);
        }

        throw new FileNotFoundException(Loc.T("crypto.rutaNoExiste"), ruta);
    }

    /// <summary>
    /// Encripta varios archivos sueltos elegidos a la vez (selección múltiple) en un único ".enc"
    /// con la misma estructura que una carpeta, pero sin carpeta contenedora real.
    /// </summary>
    public static int EncriptarVarios(
        IReadOnlyList<string> rutas, string destino, string password, bool conservarOriginal = false,
        bool borradoSeguro = false, IProgress<(int Actual, int Total)>? progreso = null)
    {
        var fuente = rutas.Select(r => (RutaCompleta: r, RutaRelativa: Path.GetFileName(r))).ToList();
        var cantidad = EncriptarConjunto(fuente, destino, password, progreso);

        if (!conservarOriginal)
            foreach (var r in rutas)
                BorrarArchivo(r, borradoSeguro);

        return cantidad;
    }

    private static int EncriptarConjunto(
        List<(string RutaCompleta, string RutaRelativa)> fuente, string destino, string password,
        IProgress<(int, int)>? progreso)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var clave = DerivarClave(password, salt);

        var entradas = new List<(string RutaCompleta, string Relativa, long Longitud, long Offset)>();
        long offsetAcumulado = 0;
        foreach (var (rutaCompleta, relativa) in fuente)
        {
            var longitud = new FileInfo(rutaCompleta).Length;
            entradas.Add((rutaCompleta, relativa, longitud, offsetAcumulado));
            offsetAcumulado += LongitudCifrada(longitud);
        }

        using var salida = File.Create(destino);
        salida.Write(salt);
        salida.WriteByte(TipoCarpeta);

        var manifiestoPlano = SerializarManifiesto(entradas);

        using (var msManifiesto = new MemoryStream(manifiestoPlano))
        {
            EscribirInt64(salida, manifiestoPlano.Length);
            CifrarStream(msManifiesto, salida, clave, manifiestoPlano.Length);
        }

        var total = entradas.Count;
        var procesados = 0;
        foreach (var e in entradas)
        {
            using (var entradaArchivo = File.OpenRead(e.RutaCompleta))
                CifrarStream(entradaArchivo, salida, clave, e.Longitud);

            procesados++;
            progreso?.Report((procesados, total));
        }

        return entradas.Count;
    }

    // ===================== Desencriptar (archivo suelto) =====================

    /// <summary>Desencripta un ".enc" de archivo individual (no carpeta) directamente a su ubicación final.</summary>
    public static (int ArchivosRestaurados, string RutaDestino) Desencriptar(string ruta, string password, bool conservarOriginal = false)
    {
        ValidarRutaEnc(ruta);

        var nombreBase = EsArchivoEncriptado(ruta) ? ruta[..^Extension.Length] : ruta + ".dec";

        using (var entrada = File.OpenRead(ruta))
        {
            var salt = new byte[SaltSize];
            LeerCompleto(entrada, salt);
            var tipo = entrada.ReadByte();
            if (tipo != TipoArchivo)
                throw new InvalidOperationException(Loc.T("crypto.tipoEsCarpeta"));

            var clave = DerivarClave(password, salt);
            var longitud = LeerInt64(entrada);
            if (longitud < 0)
                throw new CryptographicException(Loc.T("crypto.archivoDanado"));

            using var salida = File.Create(nombreBase);
            DescifrarStream(entrada, salida, clave, longitud);
        }

        if (!conservarOriginal)
            File.Delete(ruta);

        return (1, nombreBase);
    }

    // ===================== Explorar / extraer carpetas =====================

    /// <summary>
    /// Abre un .enc de carpeta (o de varios archivos): valida la contraseña y
    /// descifra únicamente el manifiesto, sin tocar el contenido de ningún
    /// archivo. Con la sesión resultante se puede listar, abrir o extraer
    /// archivos puntuales bajo demanda.
    /// </summary>
    public static SesionCarpeta AbrirCarpeta(string rutaEnc, string password)
    {
        ValidarRutaEnc(rutaEnc);

        using var entrada = File.OpenRead(rutaEnc);
        var salt = new byte[SaltSize];
        LeerCompleto(entrada, salt);
        var tipo = entrada.ReadByte();
        if (tipo != TipoCarpeta)
            throw new InvalidOperationException(Loc.T("crypto.tipoEsArchivo"));

        var clave = DerivarClave(password, salt);

        var manifiestoLen = LeerInt64(entrada);
        if (manifiestoLen < 0 || manifiestoLen > MaxLongitudManifiesto)
            throw new CryptographicException(Loc.T("crypto.archivoDanado"));

        byte[] manifiestoPlano;
        using (var msSalida = new MemoryStream())
        {
            DescifrarStream(entrada, msSalida, clave, manifiestoLen);
            manifiestoPlano = msSalida.ToArray();
        }

        var inicioDatos = entrada.Position;
        var entradas = DeserializarManifiesto(manifiestoPlano);

        return new SesionCarpeta(rutaEnc, clave, inicioDatos, entradas);
    }

    /// <summary>
    /// Sesión sobre un .enc de carpeta ya validado: permite listar, abrir o
    /// extraer archivos puntuales sin volver a pedir la contraseña.
    /// </summary>
    public sealed class SesionCarpeta
    {
        private readonly string _rutaEnc;
        private readonly byte[] _clave;
        private readonly long _inicioDatos;
        private readonly List<EntradaManifiesto> _entradas;

        internal SesionCarpeta(string rutaEnc, byte[] clave, long inicioDatos, List<EntradaManifiesto> entradas)
        {
            _rutaEnc = rutaEnc;
            _clave = clave;
            _inicioDatos = inicioDatos;
            _entradas = entradas;
        }

        /// <summary>Rutas relativas (con '/') y tamaño original de cada archivo incluido.</summary>
        public IReadOnlyList<(string RutaRelativa, long Longitud)> Archivos =>
            _entradas.Select(e => (e.Relativa, e.Longitud)).ToList();

        /// <summary>Descifra un único archivo (por su ruta relativa) al destino indicado.</summary>
        public void ExtraerArchivo(string rutaRelativa, string destino)
        {
            var entrada = _entradas.FirstOrDefault(e => e.Relativa == rutaRelativa)
                ?? throw new FileNotFoundException(Loc.T("crypto.archivoNoManifiesto"), rutaRelativa);

            var carpetaContenedora = Path.GetDirectoryName(destino);
            if (!string.IsNullOrEmpty(carpetaContenedora))
                Directory.CreateDirectory(carpetaContenedora);

            using var origen = File.OpenRead(_rutaEnc);
            origen.Seek(_inicioDatos + entrada.Offset, SeekOrigin.Begin);

            using var salida = File.Create(destino);
            DescifrarStream(origen, salida, _clave, entrada.Longitud);
        }

        /// <summary>Descifra varios archivos (rutas relativas) dentro de <paramref name="carpetaDestino"/>, preservando su estructura.</summary>
        public void ExtraerVarios(IEnumerable<string> rutasRelativas, string carpetaDestino, IProgress<(int Actual, int Total)>? progreso = null)
        {
            var lista = rutasRelativas.ToList();
            var procesados = 0;
            foreach (var relativa in lista)
            {
                var destino = Path.Combine(carpetaDestino, relativa.Replace('/', Path.DirectorySeparatorChar));
                ExtraerArchivo(relativa, destino);
                procesados++;
                progreso?.Report((procesados, lista.Count));
            }
        }
    }

    internal sealed record EntradaManifiesto(string Relativa, long Longitud, long Offset);

    // ===================== Cifrado/descifrado por bloques (AES-256-GCM) =====================

    private static void CifrarStream(Stream entrada, Stream salida, byte[] clave, long longitudTotal)
    {
        using var aesGcm = new AesGcm(clave, TagSize);
        var buffer = new byte[TamanoChunk];
        var nonce = new byte[NonceSize];
        var tag = new byte[TagSize];
        var restante = longitudTotal;

        var chunks = NumeroDeChunks(longitudTotal);
        for (var i = 0; i < chunks; i++)
        {
            var tam = (int)Math.Min(TamanoChunk, restante);
            LeerCompleto(entrada, buffer.AsSpan(0, tam));

            RandomNumberGenerator.Fill(nonce);
            var cifrado = new byte[tam];
            aesGcm.Encrypt(nonce, buffer.AsSpan(0, tam), cifrado, tag);

            salida.Write(nonce);
            salida.Write(tag);
            salida.Write(cifrado);

            restante -= tam;
        }
    }

    private static void DescifrarStream(Stream entrada, Stream salida, byte[] clave, long longitudTotal)
    {
        using var aesGcm = new AesGcm(clave, TagSize);
        var nonce = new byte[NonceSize];
        var tag = new byte[TagSize];
        var restante = longitudTotal;

        var chunks = NumeroDeChunks(longitudTotal);
        for (var i = 0; i < chunks; i++)
        {
            var tam = (int)Math.Min(TamanoChunk, restante);
            LeerCompleto(entrada, nonce);
            LeerCompleto(entrada, tag);
            var cifrado = new byte[tam];
            LeerCompleto(entrada, cifrado);

            var plano = new byte[tam];
            try
            {
                aesGcm.Decrypt(nonce, cifrado, tag, plano);
            }
            catch (CryptographicException)
            {
                throw new CryptographicException(Loc.T("crypto.archivoDanado"));
            }

            salida.Write(plano);
            restante -= tam;
        }
    }

    private static int NumeroDeChunks(long longitud) =>
        longitud == 0 ? 1 : (int)((longitud + TamanoChunk - 1) / TamanoChunk);

    private static long LongitudCifrada(long longitudOriginal) =>
        longitudOriginal + (long)NumeroDeChunks(longitudOriginal) * (NonceSize + TagSize);

    // ===================== Borrado (normal y seguro) =====================

    /// <summary>Borra una carpeta temporal (p. ej. de vista previa) sobrescribiendo su contenido antes. Falla en silencio si algún archivo sigue abierto en otra app.</summary>
    public static void LimpiarCarpetaTemporalSegura(string carpeta)
    {
        try
        {
            if (Directory.Exists(carpeta))
                BorrarCarpeta(carpeta, seguro: true);
        }
        catch
        {
            // Best-effort: puede seguir abierto en otra app.
        }
    }

    private static void BorrarArchivo(string ruta, bool seguro)
    {
        if (seguro)
            SobrescribirConAleatorio(ruta);
        File.Delete(ruta);
    }

    private static void BorrarCarpeta(string carpeta, bool seguro)
    {
        if (seguro)
            foreach (var archivo in Directory.EnumerateFiles(carpeta, "*", SearchOption.AllDirectories))
                SobrescribirConAleatorio(archivo);

        Directory.Delete(carpeta, recursive: true);
    }

    private static void SobrescribirConAleatorio(string ruta)
    {
        var longitud = new FileInfo(ruta).Length;
        using var fs = new FileStream(ruta, FileMode.Open, FileAccess.Write, FileShare.None);
        var buffer = new byte[81920];
        var restante = longitud;
        while (restante > 0)
        {
            var n = (int)Math.Min(buffer.Length, restante);
            RandomNumberGenerator.Fill(buffer.AsSpan(0, n));
            fs.Write(buffer, 0, n);
            restante -= n;
        }
        fs.Flush(flushToDisk: true);
    }

    // ===================== Helpers internos =====================

    private static void ValidarRutaEnc(string ruta)
    {
        if (Directory.Exists(ruta))
            throw new InvalidOperationException(Loc.T("crypto.seleccionarArchivoEnc"));

        if (!File.Exists(ruta))
            throw new FileNotFoundException(Loc.T("crypto.rutaNoExiste"), ruta);
    }

    private static byte[] SerializarManifiesto(List<(string RutaCompleta, string Relativa, long Longitud, long Offset)> entradas)
    {
        using var ms = new MemoryStream();
        EscribirInt32(ms, entradas.Count);
        foreach (var e in entradas)
        {
            var pathBytes = Encoding.UTF8.GetBytes(e.Relativa);
            EscribirInt32(ms, pathBytes.Length);
            ms.Write(pathBytes);
            EscribirInt64(ms, e.Longitud);
            EscribirInt64(ms, e.Offset);
        }
        return ms.ToArray();
    }

    private static List<EntradaManifiesto> DeserializarManifiesto(byte[] datos)
    {
        using var ms = new MemoryStream(datos);
        var cantidad = LeerInt32(ms);
        if (cantidad < 0 || cantidad > MaxEntradas)
            throw new CryptographicException(Loc.T("crypto.archivoDanado"));

        var lista = new List<EntradaManifiesto>(cantidad);
        for (var i = 0; i < cantidad; i++)
        {
            var pathLen = LeerInt32(ms);
            if (pathLen <= 0 || pathLen > MaxLongitudRuta)
                throw new CryptographicException(Loc.T("crypto.archivoDanado"));

            var pathBytes = new byte[pathLen];
            LeerCompleto(ms, pathBytes);
            var relativa = Encoding.UTF8.GetString(pathBytes);

            var longitud = LeerInt64(ms);
            var offset = LeerInt64(ms);
            if (longitud < 0 || offset < 0)
                throw new CryptographicException(Loc.T("crypto.archivoDanado"));

            lista.Add(new EntradaManifiesto(relativa, longitud, offset));
        }
        return lista;
    }

    private static void EscribirInt32(Stream stream, int valor) => stream.Write(BitConverter.GetBytes(valor));

    private static void EscribirInt64(Stream stream, long valor) => stream.Write(BitConverter.GetBytes(valor));

    private static int LeerInt32(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[4];
        LeerCompleto(stream, buffer);
        return BitConverter.ToInt32(buffer);
    }

    private static long LeerInt64(Stream stream)
    {
        Span<byte> buffer = stackalloc byte[8];
        LeerCompleto(stream, buffer);
        return BitConverter.ToInt64(buffer);
    }

    private static void LeerCompleto(Stream stream, Span<byte> buffer)
    {
        var leido = 0;
        while (leido < buffer.Length)
        {
            var n = stream.Read(buffer[leido..]);
            if (n == 0)
                throw new CryptographicException(Loc.T("crypto.archivoDanado"));
            leido += n;
        }
    }

    private static byte[] DerivarClave(string password, byte[] salt)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Pbkdf2Iterations, HashAlgorithmName.SHA256);
        return pbkdf2.GetBytes(KeySize);
    }
}
