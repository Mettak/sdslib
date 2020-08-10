# THIS LIBRARY IS IN REFACTORING STATE, SO IT'S USELESS RIGHT NOW. SWITCH TO THE BRANCH <a href="https://github.com/Mettak/sdslib/tree/before_refactoring">'before_refactoring'</a> WHERE IS LAST FUNCTIONAL VERSION.

# sdslib
<b>sdslib</b> is small DLL library written in C# with whose help you can manage SDS files. This library gives you ability to create .NET applications for modding <a href="https://en.wikipedia.org/wiki/Mafia_II" target=_blank><b>Mafia II</b></a>. Development of this library requires reverse engineering.

## Features
* <s>Extracting files</s>
* <s>Replacing files</s>

## SDS file
SDS data format is used by video-games (Mafia II and Mafia III); contains compressed data, such as textures, sounds, scripts, 3D models, that is extracted and loaded at runtime in <b>The Illusion Engine</b>. Following notes describes <b>version 19</b> of this file format, I don't know how it works in Mafia III which uses version 20 but I think it will be similar.

* Data are compressed with <a href="https://www.zlib.net/" target=_blank><i>zlib</i></a> into 16 KB blocks.
* Uses <a href="https://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function" target=_blank><i>FNV hash</i></a> function for checksums
* Tables and DLC's content are encrypted with <a href="https://en.wikipedia.org/wiki/Tiny_Encryption_Algorithm" target=_blank><i>TEA</i></a> (not implemented in this library)

## <s>Usage</s>
```c#
/* Init of SDS file */
SdsFile sds = new SdsFile(@"...Mafia II\pc\sds\mapa\mapa_city.sds");

/* Extracts all files from SDS into selected directory */
sds.ExtractAllFiles(@"C:\Users\Mettak\Desktop\mapa_city");

/* Extracts single file to the selected path */
sds.ExtractFileByName("map.dds", @"C:\Users\Mettak\Desktop\map.dds");

/* Extracts all textures from current SDS (if contains any) */
sds.ExtractFilesByTypeName(typeof(Texture), @"C:\Users\Mettak\Desktop\tex");

/* Replace file */
sds.ReplaceFileByName("map.dds", @"C:\Users\Mettak\Desktop\modified_map.dds");

/* Saves modified SDS file */
sds.Save();

/* Saves modified SDS file to the selected path */
sds.Save(@"C:\Users\Mettak\Desktop\modified.sds");
```

## Notice
* sdslib and all contents within this repository and/or organisation, are not affiliated with 2K Czech, 2K Games or Take-Two Interactive Software Inc.
* Mafia is registered trademark of Take-Two Interactive Software Inc.
