using System.Text;
using System.Runtime.InteropServices;

class ClipboardInspector
{
    // Interop: Win32 API
    [DllImport("user32.dll")]
    static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll")]
    static extern bool CloseClipboard();

    [DllImport("user32.dll")]
    static extern uint EnumClipboardFormats(uint format);

    [DllImport("user32.dll", SetLastError = true)]
    static extern int GetClipboardFormatName(uint format, StringBuilder lpszFormatName, int cchMaxCount);

    [STAThread]
    static void Main()
    {
        try
        {
            Console.WriteLine("=== Clipboard Format Table (Win32 API) ===\n");
            DumpClipboardFormats();

            IDataObject? dataObject = Clipboard.GetDataObject();
            if (dataObject == null)
            {
                Console.WriteLine("Clipboard is empty.");
                return;
            }

            string[] formats = dataObject.GetFormats();
            Console.WriteLine("\n=== Clipboard Content (structured formats) ===\n");

            int count = 1;
            foreach (var format in formats)
            {
                uint? id = GetClipboardFormatId(format);
                string win32Id = id?.ToString() ?? "n/a";
                string win32Name = id.HasValue ? GetFormatName(id.Value) : "unknown";
                string typeDescription = GetFormatDotNetTypeName(format);

                Console.WriteLine($"[{count++}] Format Key:     {format}");
                Console.WriteLine($"    Win32 ID:       {win32Id}");
                Console.WriteLine($"    Win32 Name:     {win32Name}");
                Console.WriteLine($"    Type Description: {typeDescription}");

                object? data = dataObject.GetData(format);
                if (data is string str)
                {
                    Console.WriteLine("    Content (Text):");
                    Console.WriteLine($"      \"{PreviewText(str)}\"");
                }
                else if (data is string[] fileList)
                {
                    Console.WriteLine("    Content (File List):");
                    foreach (var file in fileList)
                        Console.WriteLine($"      - {file}");
                }
                else if (data is MemoryStream memStream)
                {
                    Console.WriteLine("    Content (Binary Data, MemoryStream):");
                    PreviewBinary(memStream.ToArray(), indent: "      ");
                }
                else if (data is System.Drawing.Bitmap)
                {
                    Console.WriteLine("    Content: <Bitmap detected>");
                }
                else if (data != null)
                {
                    Console.WriteLine($"    Content (Type: {data.GetType()}):");
                    byte[]? bytes = ObjectToByteArray(data);
                    if (bytes != null)
                        PreviewBinary(bytes, indent: "      ");
                    else
                        Console.WriteLine("      Not displayable.");
                }
                else
                {
                    Console.WriteLine("    Content: null or unreadable.");
                }

                Console.WriteLine(); // Empty line
            }
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey(true);

        }
        catch (Exception ex)
        {
            Console.WriteLine("Error accessing the clipboard: " + ex.Message);
        }
    }

    static void DumpClipboardFormats()
    {
        if (!OpenClipboard(IntPtr.Zero))
        {
            Console.WriteLine("Clipboard could not be opened.");
            return;
        }

        uint format = 0;
        while ((format = EnumClipboardFormats(format)) != 0)
        {
            string name = GetFormatName(format);
            Console.WriteLine($"Format ID: {format,-5} Name: {name}");
        }

        CloseClipboard();
    }

    static string GetFormatName(uint format)
    {
        // Name standard formats directly
        switch (format)
        {
            case 1: return "CF_TEXT";
            case 2: return "CF_BITMAP";
            case 3: return "CF_METAFILEPICT";
            case 4: return "CF_SYLK";
            case 5: return "CF_DIF";
            case 6: return "CF_TIFF";
            case 7: return "CF_OEMTEXT";
            case 8: return "CF_DIB";
            case 9: return "CF_PALETTE";
            case 10: return "CF_PENDATA";
            case 11: return "CF_RIFF";
            case 12: return "CF_WAVE";
            case 13: return "CF_UNICODETEXT";
            case 14: return "CF_ENHMETAFILE";
            case 15: return "CF_HDROP";
            case 16: return "CF_LOCALE";
            case 17: return "CF_DIBV5";
        }

        // Custom formats
        StringBuilder nameBuilder = new StringBuilder(256);
        int len = GetClipboardFormatName(format, nameBuilder, nameBuilder.Capacity);
        if (len > 0)
            return nameBuilder.ToString();
        else
            return "(Unnamed/Unknown format)";
    }

    static string PreviewText(string input, int maxLength = 200)
    {
        input = input.Replace("\r", "").Replace("\n", "\\n");
        return input.Length > maxLength ? input.Substring(0, maxLength) + "..." : input;
    }

    static void PreviewBinary(byte[] bytes, int maxLength = 64, string indent = "  ")
    {
        int len = Math.Min(bytes.Length, maxLength);
        string hex = BitConverter.ToString(bytes.Take(len).ToArray()).Replace("-", " ");
        Console.WriteLine($"{indent}Length: {bytes.Length} bytes");
        Console.WriteLine($"{indent}Hexdump: {hex}" + (bytes.Length > len ? " ..." : ""));
    }

    static byte[]? ObjectToByteArray(object obj)
    {
        if (obj is MemoryStream ms)
            return ms.ToArray();
        if (obj is byte[] b)
            return b;
        return null;
    }

    static string GetFormatDotNetTypeName(string format)
    {
        if (format == DataFormats.Text)
            return "Text (ANSI)";
        else if (format == DataFormats.UnicodeText)
            return "Text (Unicode)";
        else if (format == DataFormats.Html)
            return "HTML (System)";
        else if (format == DataFormats.Rtf)
            return "Rich Text Format (RTF)";
        else if (format == DataFormats.Bitmap)
            return "Bitmap (System.Drawing.Bitmap)";
        else if (format == DataFormats.FileDrop)
            return "File List (Explorer)";
        else if (format == "HTML Format")
            return "HTML Fragment (complex)";
        else if (format == "PNG")
            return "PNG Image (binary)";
        else if (format == "JFIF" || format == "JPG" || format == "JPEG")
            return "JPEG Image (binary)";
        else
            return "Unknown / custom";
    }

    static uint? GetClipboardFormatId(string formatName)
    {
        if (!OpenClipboard(IntPtr.Zero))
            return null;

        uint current = 0;
        while ((current = EnumClipboardFormats(current)) != 0)
        {
            StringBuilder sb = new StringBuilder(256);
            int len = GetClipboardFormatName(current, sb, sb.Capacity);
            if (len > 0 && sb.ToString().Equals(formatName, StringComparison.OrdinalIgnoreCase))
            {
                CloseClipboard();
                return current;
            }
            // Standard formats like "UnicodeText" have no name → compare by standard name
            if (formatName == DataFormats.Text && current == 1) { CloseClipboard(); return 1; }
            if (formatName == DataFormats.Bitmap && current == 2) { CloseClipboard(); return 2; }
            if (formatName == DataFormats.UnicodeText && current == 13) { CloseClipboard(); return 13; }
            if (formatName == DataFormats.FileDrop && current == 15) { CloseClipboard(); return 15; }
        }

        CloseClipboard();
        return null;
    }
}
