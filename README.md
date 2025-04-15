# Clipboard Inspector

A C# console utility for analyzing the full contents of the Windows clipboard.  
It enumerates all available formats using both .NET and Win32 APIs, and displays format metadata and content previews where possible.

---

## Purpose

Windows stores clipboard data in a structured format:  
A list of **key-value pairs**, where:

- **Key** = Data format (e.g., `CF_UNICODETEXT`, `"HTML Format"`, `"PNG"`)
- **Value** = Data in that format (text, file list, image, binary stream, etc.)

Applications typically place multiple formats simultaneously into the clipboard so that other programs can choose the most appropriate one for pasting or processing.

---

## Key Concepts

### Clipboard as a format map

- The clipboard is a container of **multiple parallel formats**
- Each format is independently stored and accessible
- Applications iterate through the available formats and process the one(s) they support

### .NET vs. Win32 clipboard access

| API / Layer                       | Format visibility                                              | Use case                 |
| --------------------------------- | -------------------------------------------------------------- | ------------------------ |
| Win32 (`EnumClipboardFormats`)    | Full list of formats, including OLE, COM, proprietary formats  | Forensic inspection      |
| .NET (`IDataObject.GetFormats()`) | Limited to common formats (Text, FileDrop, HTML, Bitmap, etc.) | Application-level access |

Some formats (e.g., `Ole Private Data`, `DataObject`, `EnterpriseDataProtectionId`) are only visible through the Win32 API and are not exposed via .NET.

### Format resolution

- Common format names are defined in `.NET` under `System.Windows.Forms.DataFormats`
- Win32 formats are internally referenced by numeric IDs (standard or registered)
- Custom formats have IDs >= `0xC000` and can be named using `GetClipboardFormatName`
- Some formats are unnamed and only accessible by ID

---

## Supported content types

The tool detects and optionally previews:

- Text (`CF_TEXT`, `CF_UNICODETEXT`)
- File lists (`CF_HDROP`)
- Bitmaps and encoded image data (`CF_BITMAP`, `"PNG"`, `"JFIF"`)
- Rich Text (`RTF`)
- HTML fragments (`"HTML Format"`)
- Arbitrary binary data (e.g., MemoryStream or byte[])

---

## Design

- Uses `.NET Clipboard` API for structured content retrieval
- Uses `user32.dll` (Win32) for format ID enumeration and format name resolution
- Output is grouped by format, with keys and values printed clearly
- Binary formats are previewed as hexadecimal dumps
- Format mappings include both Win32 and .NET perspectives where available

---
